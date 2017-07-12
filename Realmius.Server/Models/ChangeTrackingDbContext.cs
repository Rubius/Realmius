////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realmius.Contracts.Helpers;
using Realmius.Contracts.Models;
using Realmius.Server.Configurations;
using Realmius.Server.Exchange;
using Realmius.Server.Infrastructure;

namespace Realmius.Server.Models
{
    public abstract class ChangeTrackingDbContext : DbContext
    {
        public object User { get; set; }

        internal static Dictionary<Type, IRealmiusServerDbConfiguration> Configurations = new Dictionary<Type, IRealmiusServerDbConfiguration>();

        public static event EventHandler<UpdatedDataBatch> DataUpdated;

        public static ILogger Logger { get; set; } = new Logger();

        protected virtual void OnDataUpdated(UpdatedDataBatch e)
        {
            DataUpdated?.Invoke(this, e);
        }
        public bool EnableAudit { get; set; }
        public bool EnableSyncTracking { get; set; } = true;

        private readonly string _nameOrConnectionString;
        private IRealmiusServerDbConfiguration _syncConfiguration;
        private Dictionary<string, SyncTypeInfo> _syncedTypes;

        protected ChangeTrackingDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            _nameOrConnectionString = nameOrConnectionString;
            Initialize();
        }

        protected ChangeTrackingDbContext()
        {
            _nameOrConnectionString = Database.Connection.ConnectionString;
            Initialize();
        }

        private void Initialize()
        {
            IRealmiusServerDbConfiguration configuration;
            if (Configurations.TryGetValue(this.GetType(), out configuration))
            {
                Initialize(configuration);
            }
        }

        private void Initialize(IRealmiusServerDbConfiguration syncConfiguration)
        {
            if (syncConfiguration == null)
                return;

            _syncConfiguration = syncConfiguration;
            _syncedTypes = new Dictionary<string, SyncTypeInfo>();
            foreach (Type type in _syncConfiguration.TypesToSync)
            {
                var propertyDict = new Dictionary<string, bool>();
                foreach (PropertyInfo propertyInfo in type.GetProperties())
                {
                    var ignore = propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() != null;
                    propertyDict[propertyInfo.Name] = !ignore;
                }

                _syncedTypes[type.Name] = new SyncTypeInfo()
                {
                    TypeName = type.Name,
                    TrackedProperties = propertyDict,
                };
            }
        }

        protected virtual IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj)
        {
            return _syncConfiguration.GetTagsForObject(db, obj);
        }
        public override int SaveChanges()
        {
            return ProcessChanges();
            //return base.SaveChanges();
        }

        public void UpdateChangeTime()
        {

        }

        private const string ChangesForDeletedObject = "";
        protected virtual int ProcessChanges()
        {
            if (!EnableSyncTracking)
            {
                return base.SaveChanges();
            }
            if (_syncConfiguration == null)
                throw new InvalidOperationException($"{nameof(ChangeTrackingDbContext)} was not initialized (_syncConfiguration is null)!");

            int result;
            try
            {
                var syncStatusContext = CreateSyncStatusContext();

                var updatedResult = new UpdatedDataBatch();
                var dateTimeNow = GetDate();

                var entries = ChangeTracker.Entries()
                    .Where(x => !(x.Entity is LogEntryBase))
                    .Where(e => e.State == EntityState.Modified
                        || e.State == EntityState.Added
                        || e.State == EntityState.Deleted
                    ).Select(x => new EfEntityInfo
                    {
                        ModifiedProperties = x.State == EntityState.Deleted ? new Dictionary<string, bool>() : x.CurrentValues.PropertyNames.ToDictionary(z => z, z => x.Property(z).IsModified),
                        Entity = x.Entity,
                        CurrentValues = x.State == EntityState.Deleted ? null : x.CurrentValues?.Clone(),
                        OriginalValues = x.State == EntityState.Added ? null : x.OriginalValues?.Clone(),
                        State = x.State
                    }).ToList();

                result = base.SaveChanges();

                if (EnableAudit)
                {
                    SaveLogs(entries);
                    base.SaveChanges();
                }

                foreach (var entity in entries)
                {
                    var typeName = GetEfTypeName(entity.Entity);
                    if (!_syncedTypes.ContainsKey(typeName))
                        continue;
                    var syncTypeInfo = _syncedTypes[typeName];

                    var obj = (IRealmiusObjectServer)entity.Entity;
                    var syncStatusObject = AddOrCreateNewSyncObject(syncStatusContext, typeName, obj.MobilePrimaryKey);
                    string changes;
                    if (entity.State == EntityState.Modified)
                    {
                        var serializedCurrent = SerializeObject(obj);

                        if (syncStatusObject != null && syncStatusObject.FullObjectAsJson == serializedCurrent)
                        {
                            changes = null;
                        }
                        else
                        {
                            var jsonDiff = JsonHelper.GetJsonDiffAsJObject(
                                syncStatusObject?.FullObjectAsJson ?? "{}",
                                serializedCurrent);

                            if (syncStatusObject != null)
                            {
                                foreach (var property in jsonDiff.Properties())
                                {
                                    var propName = property.Name;
                                    syncStatusObject.ColumnChangeDates[propName] = dateTimeNow;
                                }
                            }

                            changes = jsonDiff.ToString();
                        }

                    }
                    else if (entity.State == EntityState.Added)
                    {
                        changes = SerializeObject(obj);
                        var jObject = JObject.Parse(changes);

                        foreach (var propName in jObject.Properties().Select(x => x.Name))
                        {
                            syncStatusObject.ColumnChangeDates[propName] = dateTimeNow;
                        }
                        changes = jObject.ToString();
                    }
                    else if (entity.State == EntityState.Deleted)
                    {
                        syncStatusObject.IsDeleted = true;
                        changes = ChangesForDeletedObject;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException($"Entity state '{entity.State}' is not supported");
                    }

                    if (changes != null)
                        ProcessAndAddChanges(syncStatusObject, obj, updatedResult, changes);
                }

                if (updatedResult.Items.Count > 0)
                {
                    syncStatusContext.SaveChanges();

                    OnDataUpdated(updatedResult);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return result;
        }

        public static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new RealmServerObjectResolver(),
        };

        internal virtual string SerializeObject(IRealmiusObjectServer obj)
        {
            return JsonConvert.SerializeObject(obj, JsonSerializerSettings);
        }

        private void ProcessAndAddChanges(SyncStatusServerObject syncStatusObject, IRealmiusObjectServer obj, UpdatedDataBatch updatedResult, string changes)
        {
            Process(syncStatusObject, obj);
            var updatedResultItem = CreateDownloadResponseItemInfo(syncStatusObject, changes);

            updatedResult.Items.Add(updatedResultItem);
        }

        private DownloadResponseItemInfo CreateDownloadResponseItemInfo(SyncStatusServerObject syncObj, string changes)
        {
            var downloadResponseItem = new DownloadResponseItem()
            {
                Type = syncObj.Type,
                IsDeleted = syncObj.IsDeleted,
                MobilePrimaryKey = syncObj.MobilePrimaryKey,
                SerializedObject = changes,
            };

            return new DownloadResponseItemInfo()
            {
                DownloadResponseItem = downloadResponseItem,
                Tag0 = syncObj.Tag0,
                Tag1 = syncObj.Tag1,
                Tag2 = syncObj.Tag2,
                Tag3 = syncObj.Tag3,
            };
        }

        internal virtual DateTimeOffset GetDate()
        {
            return DateTimeOffset.Now;
        }

        private SyncStatusServerObject AddOrCreateNewSyncObject(SyncStatusDbContext syncStatusContext, string type, string mobilePrimaryKey)
        {
            var syncObj = syncStatusContext.SyncStatusServerObjects.Find(type, mobilePrimaryKey);
            if (syncObj == null)
            {
                syncObj = new SyncStatusServerObject(type, mobilePrimaryKey);
                syncStatusContext.SyncStatusServerObjects.Add(syncObj);
            }
            return syncObj;
        }

        internal SyncStatusDbContext CreateSyncStatusContext()
        {
            // var connectionString = Database.Connection.ConnectionString;
            var syncStatusContext = new SyncStatusDbContext(_nameOrConnectionString);
            return syncStatusContext;
        }

        protected virtual void Process(SyncStatusServerObject syncStatusObject, IRealmiusObjectServer obj)
        {
            var typeName = GetEfTypeName(obj);
            var tags = GetTagsForObject(this, obj);
            if (tags.Count > 0)
                syncStatusObject.Tag0 = tags[0];
            if (tags.Count > 1)
                syncStatusObject.Tag1 = tags[1];
            if (tags.Count > 2)
                syncStatusObject.Tag2 = tags[2];
            if (tags.Count > 3)
                syncStatusObject.Tag3 = tags[3];

            syncStatusObject.FullObjectAsJson = SerializeObject(obj);
            syncStatusObject.LastChange = DateTime.UtcNow;
            syncStatusObject.Type = typeName;
            syncStatusObject.UpdateColumnChangeDatesSerialized();
        }

        public override Task<int> SaveChangesAsync()
        {
            ProcessChanges();
            return base.SaveChangesAsync();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            ProcessChanges();
            return base.SaveChangesAsync(cancellationToken);
        }

        #region Audit
        private void SaveLogs(List<EfEntityInfo> entries)
        {
            foreach (var entity in entries)
            {
                LogEntryBase log = null;
                if (entity.State == EntityState.Modified)
                {
                    var beforeJson = Serialize(entity.OriginalValues);
                    var afterJson = Serialize(entity.CurrentValues);
                    var diffJson = JsonHelper.GetJsonDiff(beforeJson, afterJson);
                    log = CreateLogEntry(beforeJson, afterJson, diffJson, entity);

                }
                else if (entity.State == EntityState.Added)
                {
                    var beforeJson = "{}";
                    var afterJson = Serialize(entity.CurrentValues);
                    var diffJson = JsonHelper.GetJsonDiff(beforeJson, afterJson);
                    log = CreateLogEntry(beforeJson, afterJson, diffJson, entity);
                }
                else if (entity.State == EntityState.Deleted)
                {
                    var beforeJson = Serialize(entity.OriginalValues);
                    var afterJson = "{deleted: true}";

                    log = CreateLogEntry(beforeJson, afterJson, "{deleted: true}", entity);
                }

                if (log != null)
                {
                    AddLogsToDatabase(log);
                }
            }
        }

        protected virtual void AddLogsToDatabase(LogEntryBase log)
        {
        }

        protected virtual LogEntryBase CreateLogEntry(string beforeJson, string afterJson, string diffJson, EfEntityInfo entity)
        {
            var entityType = GetEfTypeName(entity.Entity);

            var logEntry = CreateLogEntry();
            logEntry.Time = DateTimeOffset.Now;
            logEntry.EntityType = entityType;
            logEntry.BeforeJson = beforeJson;
            logEntry.AfterJson = afterJson;
            logEntry.ChangesJson = diffJson;
            logEntry.RecordIdInt = GetId<int>(entity.Entity);
            logEntry.RecordIdString = GetId<string>(entity.Entity);
            return logEntry;
        }

        protected virtual LogEntryBase CreateLogEntry()
        {
            return new LogEntryBase();
        }


        protected virtual T GetId<T>(object entity)
        {
            var value = entity.GetType().GetProperty("Id")?.GetValue(entity);
            if (value is T)
            {
                return (T)value;
            }
            return default(T);
        }

        private string GetEfTypeName(object entity)
        {
            var name = entity.GetType().Name;
            var lastUnderscore = name.LastIndexOf("_", StringComparison.Ordinal);
            return lastUnderscore > 0 ? name.Substring(0, lastUnderscore) : name;
        }
        #endregion

        private string Serialize(DbPropertyValues values)
        {
            var dict = new Dictionary<string, object>();
            foreach (var valuesPropertyName in values.PropertyNames)
            {
                dict[valuesPropertyName] = values[valuesPropertyName];
            }
            return JsonHelper.SerializeObject(dict, 1);
        }

        public void AttachObjects(IEnumerable<IRealmiusObjectServer> objects)
        {
            var syncStatusContext = CreateSyncStatusContext();

            var updatedResult = new UpdatedDataBatch();

            foreach (var obj in objects)
            {
                try
                {
                    AttachObject(obj, syncStatusContext, updatedResult);
                }
                catch (Exception e)
                {
                    Logger.Exception(e, $"Error attaching object {obj.MobilePrimaryKey}");
                }
            }

            OnDataUpdated(updatedResult);
            syncStatusContext.SaveChanges();
        }

        public void AttachDeletedObject(string typeName, string id)
        {
            var syncStatusContext = CreateSyncStatusContext();
            var syncObj = AddOrCreateNewSyncObject(syncStatusContext, typeName, id);
            syncObj.IsDeleted = true;
            syncObj.LastChange = DateTimeOffset.UtcNow;

            var updatedResult = new UpdatedDataBatch();
            var updatedResultItem = CreateDownloadResponseItemInfo(syncObj, ChangesForDeletedObject);
            updatedResult.Items.Add(updatedResultItem);
            OnDataUpdated(updatedResult);

            syncStatusContext.SaveChanges();
        }

        public void AttachObject(string type, string id)
        {
            var obj = (IRealmiusObjectServer)GetObjectByKey(type, id);
            AttachObject(obj);
        }

        public void AttachObject(IRealmiusObjectServer obj)
        {
            AttachObjects(new[] { obj });
        }

        private void AttachObject(IRealmiusObjectServer obj, SyncStatusDbContext syncStatusContext, UpdatedDataBatch updatedResult)
        {
            var typeName = GetEfTypeName(obj);
            var syncObj = AddOrCreateNewSyncObject(syncStatusContext, typeName, obj.MobilePrimaryKey);

            var current = JObject.FromObject(obj);
            var diff = JsonHelper.GetJsonDiff(JObject.Parse(syncObj.FullObjectAsJson ?? "{}"), current);

            var dateTimeNow = GetDate();

            foreach (JProperty jProperty in diff.Properties())
            {
                syncObj.ColumnChangeDates[jProperty.Name] = dateTimeNow;
            }

            ProcessAndAddChanges(syncObj, obj, updatedResult, diff.ToString());
        }

        public object GetObjectByKey(string type, string keyString)
        {
            var keyType = GetKeyType(type);
            var key = keyType == typeof(Guid) ? Guid.Parse(keyString) : Convert.ChangeType(keyString, keyType);

            var entitySet = GetEntitySet(type);

            var dbSet = (dynamic)GetType().GetProperty(entitySet.Name).GetValue(this, null);
            return (object)dbSet.Find(key);
        }

        private EntitySet GetEntitySet(string typeName)
        {
            var adapter = (IObjectContextAdapter)this;
            var context = adapter.ObjectContext;
            var container = context.MetadataWorkspace.GetEntityContainer(context.DefaultContainerName, DataSpace.CSpace);
            var entitySet = container.EntitySets.FirstOrDefault(x => x.ElementType.Name == typeName);
            if (entitySet == null)
                throw new InvalidOperationException($"Type '{typeName}' not found in the DbContext");

            return entitySet;
        }
        internal DbSet GetDbSet(string typeName)
        {
            var entitySet = GetEntitySet(typeName);

            return (DbSet)GetType().GetProperty(entitySet.Name).GetValue(this, null);
        }

        internal Type GetKeyType(string typeName)
        {
            var entitySet = GetEntitySet(typeName);

            var keys = entitySet.ElementType.KeyProperties.FirstOrDefault().PrimitiveType.ClrEquivalentType;

            return keys;
        }

        public T CloneWithOriginalValues<T>(T dbEntity)
            where T : class, IRealmiusObjectServer
        {
            var entry = Entry(dbEntity);
            if (entry.State == EntityState.Detached)
                return null; //new entity

            var untouchedEntityClone = (T)entry.GetDatabaseValues().ToObject();

            return untouchedEntityClone;
        }


    }
}