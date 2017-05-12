using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realmius.Contracts.Models;
using Realmius.Server.Expressions;
using Realmius.Server.Infrastructure;
using Realmius.Server.Models;
using Realmius.Server.QuickStart;
using Realmius.Server.ServerConfiguration;

namespace Realmius.Server
{
    public class RealmSyncServerProcessor : RealmSyncServerProcessor<ISyncUser>
    {
        public RealmSyncServerProcessor(Func<ChangeTrackingDbContext> dbContextFactoryFunc,
            IRealmSyncServerConfiguration<ISyncUser> configuration)
            : base(dbContextFactoryFunc, configuration)
        {
        }

        /// <summary>
        /// this will all the types between all users!
        /// </summary>
        public RealmSyncServerProcessor(Func<ChangeTrackingDbContext> dbContextFactoryFunc, Type typeToSync, params Type[] typesToSync)
            : base(dbContextFactoryFunc, new ShareEverythingRealmSyncServerConfiguration(typeToSync, typesToSync))
        {
        }
    }

    public class RealmSyncServerProcessor<TUser>
        where TUser : ISyncUser
    {
        private readonly Func<ChangeTrackingDbContext> _dbContextFactoryFunc;
        public IRealmSyncServerConfiguration<TUser> Configuration { get; }
        private readonly Dictionary<string, Type> _syncedTypes;
        private readonly string _connectionString;

        public RealmSyncServerProcessor(Func<ChangeTrackingDbContext> dbContextFactoryFunc, IRealmSyncServerConfiguration<TUser> configuration)
        {
            _dbContextFactoryFunc = dbContextFactoryFunc;
            Configuration = configuration;
            _syncedTypes = Configuration.TypesToSync.ToDictionary(x => x.Name, x => x);
            var syncObjectInterface = typeof(IRealmSyncObjectServer);
            foreach (var type in _syncedTypes.Values)
            {
                if (!syncObjectInterface.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                    throw new InvalidOperationException($"Type {type} does not implement IRealmSyncObjectServer, unable to continue");
            }

            _connectionString = dbContextFactoryFunc().Database.Connection.ConnectionString;

        }

        public UploadDataResponse Upload(UploadDataRequest request, TUser user)
        {
            var result = new UploadDataResponse();

            //var updatedResult = new UpdatedDataBatch();

            var ef = _dbContextFactoryFunc();
            ef.User = user;
            foreach (var item in request.ChangeNotifications)
            {
                UploadDataResponseItem objectInfo = null;
                Logger.Log.Debug($"User {user}, Saving entity: {JsonConvert.SerializeObject(item)}");
                IRealmSyncObjectServer dbEntity = null;
                try
                {
                    var type = _syncedTypes[item.Type];
                    objectInfo = new UploadDataResponseItem(item.PrimaryKey, item.Type);
                    result.Results.Add(objectInfo);

                    var dbSet = ef.Set(type);

                    object[] key;
                    try
                    {
                        key = Configuration.KeyForType(type, item.PrimaryKey);
                    }
                    catch (Exception e)
                    {
                        Logger.Log.Exception(e, $"Error getting key, {item.PrimaryKey}");
                        throw;
                    }

                    var referencesConverter = new RealmServerCollectionConverter()
                    {
                        Database = ef,
                    };
                    var settings = new JsonSerializerSettings()
                    {
                        Converters = new List<JsonConverter>()
                        {
                            referencesConverter
                        },
                        ObjectCreationHandling = ObjectCreationHandling.Reuse,
                    };
                    dbEntity = (IRealmSyncObjectServer)dbSet.Find(key);
                    if (dbEntity != null)
                    {
                        if (item.IsDeleted)
                        {
                            dbSet.Remove(dbEntity);
                        }
                        else
                        {
                            //entity exists in DB, UPDATE it
                            var untouchedEntityClone = ef.CloneWithOriginalValues(dbEntity);

                            JsonConvert.PopulateObject(item.SerializedObject, dbEntity, settings);

                            var args = new CheckAndProcessArgs<TUser>()
                            {
                                Database = ef,
                                OriginalDbEntity = untouchedEntityClone,
                                Entity = dbEntity,
                                User = user
                            };
                            if (!CheckAndProcess(args))
                            {
                                //revert all changes (no need to revert, entity was detached)
                                ef.Entry(dbEntity).Reload();
                                objectInfo.Error = "Object failed security checks";
                            }
                            else
                            {

                                //try
                                //{
                                //    entry.State = EntityState.Modified;
                                //}
                                //catch (InvalidOperationException e)
                                //{
                                //    var anotherEntry = dbSet.Find(key);
                                //    ef.Entry(anotherEntry).State = EntityState.Detached;
                                //    entry.State = EntityState.Modified;
                                //}
                            }
                        }
                    }
                    else
                    {
                        if (!item.IsDeleted)
                        {
                            //entity does not exist in DB, CREATE it
                            dbEntity = (IRealmSyncObjectServer)JsonConvert.DeserializeObject(item.SerializedObject,
                                type, settings);

                            var args = new CheckAndProcessArgs<TUser>()
                            {
                                Database = ef,
                                OriginalDbEntity = null,
                                Entity = dbEntity,
                                User = user
                            };
                            if (CheckAndProcess(args))
                            {
                                //dbEntity.LastChangeServer = DateTime.UtcNow;
                                //add to the database
                                dbSet.Attach(dbEntity);
                                ef.Entry(dbEntity).State = EntityState.Added;
                            }
                            else
                            {
                                objectInfo.Error = "Object failed security checks";
                            }
                        }
                    }

                    //updatedResult.Items.Add(new UpdatedDataItem()
                    //{
                    //    DeserializedObject = dbEntity,
                    //    Change = new DownloadResponseItem()
                    //    {
                    //        MobilePrimaryKey = item.PrimaryKey,
                    //        Type = item.Type,
                    //        SerializedObject = item.SerializedObject,
                    //    },
                    //});
                    if (!string.IsNullOrEmpty(objectInfo.Error))
                    {
                        Logger.Log.Debug($"Error saving the entity {objectInfo.Error}");
                    }
                    ef.SaveChanges();
                }
                catch (Exception e)
                {
                    if (objectInfo != null)
                    {
                        objectInfo.Error = e.ToString();
                    }
                    if (e is DbEntityValidationException && dbEntity != null)
                    {
                        ef.Entry(dbEntity).State = EntityState.Detached; //if one entity fails EF validation, we should still save all the others (if possible)
                    }
                    Logger.Log.Info($"Exception saving the entity {e}");
                    //continue processing anyway
                    //throw;
                }
            }

            //OnDataUpdated(updatedResult);

            return result;
        }


        public DownloadDataResponse Download(DownloadDataRequest request, TUser user)
        {
            //var ef = _dbContextFactoryFunc();
            if (user == null)
                throw new NullReferenceException("user arg cannot be null");

            var response = new DownloadDataResponse()
            {
                LastChange = user.Tags.ToDictionary(x => x, x => DateTimeOffset.UtcNow),
            };


            request.Types = request.Types.Intersect(_syncedTypes.Keys).ToList();
            //if (types.Count > 0)
            //{
            //    throw new Exception("Some types are not configured to be synced: " + string.Join(",", types));
            //}

            var context = CreateSyncStatusDbContext();

            var changes = CreateQuery(request, user, context);

            foreach (var syncStatusServerObject in changes)
            {
                var lastChangeTime = FindLastChangeTime(request, syncStatusServerObject);

                var jObject = JObject.Parse(syncStatusServerObject.FullObjectAsJson);
                var changedColumns =
                    syncStatusServerObject.ColumnChangeDates.Where(x => x.Value > lastChangeTime)
                        .ToDictionary(x => x.Key);

                foreach (var property in jObject.Properties().ToList())
                {
                    if (!changedColumns.ContainsKey(property.Name))
                    {
                        jObject.Remove(property.Name);
                    }
                }
                ;
                var downloadResponseItem = new DownloadResponseItem()
                {
                    Type = syncStatusServerObject.Type,
                    MobilePrimaryKey = syncStatusServerObject.MobilePrimaryKey,
                    IsDeleted = syncStatusServerObject.IsDeleted,
                    SerializedObject = jObject.ToString(),
                };
                response.ChangedObjects.Add(downloadResponseItem);
            }

            return response;
        }

        private DateTimeOffset FindLastChangeTime(DownloadDataRequest request, SyncStatusServerObject syncStatusServerObject)
        {
            var tags = new[]
            {
                syncStatusServerObject.Tag0,
                syncStatusServerObject.Tag1,
                syncStatusServerObject.Tag2,
                syncStatusServerObject.Tag3,
            };
            var lastChangeTime = DateTimeOffset.MinValue;
            foreach (string tag in tags.Where(x => !string.IsNullOrEmpty(x)))
            {
                DateTimeOffset time;
                if (request.LastChangeTime.TryGetValue(tag, out time))
                {
                    if (time > lastChangeTime)
                        lastChangeTime = time;
                }
            }

            return lastChangeTime;
        }

        internal IQueryable<SyncStatusServerObject> CreateQuery(DownloadDataRequest request, TUser user, SyncStatusDbContext context)
        {
            Expression<Func<SyncStatusServerObject, bool>> where = null;
            foreach (string userTag in user.Tags)
            {
                DateTimeOffset lastChange;

                Expression<Func<SyncStatusServerObject, bool>> condition = null;
                if (request.LastChangeTime.TryGetValue(userTag, out lastChange))
                {
                    condition =
                        x =>
                            (x.Tag0 == userTag || x.Tag1 == userTag || x.Tag2 == userTag || x.Tag3 == userTag) &&
                            x.LastChange > lastChange;
                }
                else if (!request.OnlyDownloadSpecifiedTags)
                {
                    condition = x => !x.IsDeleted && (x.Tag0 == userTag || x.Tag1 == userTag || x.Tag2 == userTag || x.Tag3 == userTag);
                }

                if (condition != null)
                {
                    if (where == null)
                        where = condition;
                    else
                        where = where.Or(condition);
                }
            }

            if (where == null)
                where = x => false;

            var changes = context.SyncStatusServerObjects.AsNoTracking()
                .Where(x => request.Types.Contains(x.Type)).Where(where);

            //var changes = context.SyncStatusServerObjects.AsNoTracking()
            //    .Where(x => x.LastChange > request.LastChangeTime &&
            //    request.Types.Contains(x.Type)
            //    && (user.Tags.Contains(x.Tag0) || user.Tags.Contains(x.Tag1) || user.Tags.Contains(x.Tag2) || user.Tags.Contains(x.Tag3)))
            //    .OrderBy(x => x.LastChange);


            return changes;
        }

        /// <summary>
        /// returns True if it's ok to save the object, False oterwise
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckAndProcess(CheckAndProcessArgs<TUser> args)
        {
            return Configuration.CheckAndProcess(args);
        }

        protected SyncStatusDbContext CreateSyncStatusDbContext()
        {
            var context = new SyncStatusDbContext(_connectionString);
            return context;
        }
    }
}