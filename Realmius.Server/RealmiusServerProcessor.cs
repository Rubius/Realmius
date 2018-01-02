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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realmius.Contracts.Models;
using Realmius.Server.Configurations;
using Realmius.Server.Expressions;
using Realmius.Server.Infrastructure;
using Realmius.Server.Models;
using Realmius.Contracts.Logger;
[assembly: InternalsVisibleTo("Realmius.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace Realmius.Server
{
    public class RealmiusServerProcessor : RealmiusServerProcessor<object>
    {
        public RealmiusServerProcessor(IRealmiusServerConfiguration<object> configuration)
            : base(configuration)
        {
        }
    }

    public class RealmiusServerProcessor<TUser>
    {
        private readonly Func<ChangeTrackingDbContext> _dbContextFactoryFunc;
        public IRealmiusServerConfiguration<TUser> Configuration { get; }
        private ILogger Logger => Configuration.Logger;
        private readonly Dictionary<string, Type> _syncedTypes;
        private readonly string _connectionString;

        public RealmiusServerProcessor(IRealmiusServerConfiguration<TUser> configuration)
        {
            if (configuration.Logger == null)
            {
                configuration.Logger = new Logger();
            }
            _dbContextFactoryFunc = configuration.ContextFactoryFunction;
            Configuration = configuration;
            _syncedTypes = Configuration.TypesToSync.ToDictionary(x => x.Name, x => x);
            var syncObjectInterface = typeof(IRealmiusObjectServer);
            foreach (var type in _syncedTypes.Values)
            {
                if (!syncObjectInterface.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                    throw new InvalidOperationException($"Type {type} does not implement IRealmiusObjectServer, unable to continue");
            }

            _connectionString = _dbContextFactoryFunc().Database.GetDbConnection().ConnectionString;
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
                string upObjectInfo = !item.IsDeleted
                    ? item.SerializedObject
                    : "{" + $"\n  \"{nameof(item.Type)}\": \"{item.Type}\",\n  \"{nameof(item.PrimaryKey)}\": \"{item.PrimaryKey}\"\n  \"{nameof(item.IsDeleted)}\": \"{item.IsDeleted}\"\n" + "}";
                Logger.Debug($"User {user}, Saving entity: {upObjectInfo}"); //{JsonConvert.SerializeObject(item)}
                IRealmiusObjectServer dbEntity = null;
                try
                {
                    var type = _syncedTypes[item.Type];
                    objectInfo = new UploadDataResponseItem(item.PrimaryKey, item.Type);
                    result.Results.Add(objectInfo);

                    object[] key;
                    try
                    {
                        key = Configuration.KeyForType(type, item.PrimaryKey);
                    }
                    catch (Exception e)
                    {
                        Logger.Exception(e, $"Error getting key, {item.PrimaryKey}");
                        throw;
                    }

                    var referencesConverter = new RealmServerCollectionConverter
                    {
                        Database = ef,
                    };
                    var settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter>
                        {
                            referencesConverter
                        },
                        ObjectCreationHandling = ObjectCreationHandling.Reuse,
                    };
                    dbEntity = (IRealmiusObjectServer)ef.Find(type, key);
                    if (dbEntity != null)
                    {
                        if (item.IsDeleted)
                        {
                            ef.Remove(dbEntity);
                        }
                        else
                        {
                            //entity exists in DB, UPDATE it
                            var untouchedEntityClone = ef.CloneWithOriginalValues(dbEntity);

                            JsonConvert.PopulateObject(item.SerializedObject, dbEntity, settings);

                            var args = new CheckAndProcessArgs<TUser>
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
                            dbEntity = (IRealmiusObjectServer)JsonConvert.DeserializeObject(item.SerializedObject,
                                type, settings);

                            var args = new CheckAndProcessArgs<TUser>
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
                                ef.Attach(dbEntity);
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
                        Logger.Debug($"Error saving the entity {objectInfo.Error}");
                    }
                    ef.SaveChanges();
                }
                catch (Exception e)
                {
                    if (objectInfo != null)
                    {
                        objectInfo.Error = e.ToString();
                    }
                    if (e is ValidationException && dbEntity != null)
                    {
                        ef.Entry(dbEntity).State = EntityState.Detached; //if one entity fails EF validation, we should still save all the others (if possible)
                    }
                    Logger.Info($"Exception saving the entity {e}");
                    //continue processing anyway
                    //throw;
                }
            }

            //OnDataUpdated(updatedResult);

            return result;
        }

        public virtual IList<string> GetTagsForUser(TUser user)
        {
            var ef = _dbContextFactoryFunc();
            return Configuration.GetTagsForUser(user, ef);
        }

        public DownloadDataResponse Download(DownloadDataRequest request, TUser user)
        {
            if (user == null)
                throw new NullReferenceException("user arg cannot be null");

            var response = new DownloadDataResponse
            {
                LastChange = GetTagsForUser(user).ToDictionary(x => x, x => DateTimeOffset.UtcNow),
            };

            request.Types = request.Types.Intersect(_syncedTypes.Keys).ToList();
            //if (types.Count > 0)
            //{
            //    throw new Exception("Some types are not configured to be synced: " + string.Join(",", types));
            //}

            var context = CreateSyncStatusDbContext();

            var changes = CreateQuery(request, user, context);

            foreach (var changedObject in changes)
            {
                var lastChangeTime = FindLastChangeTime(request, changedObject);

                var jObject = JObject.Parse(changedObject.FullObjectAsJson);
                var changedColumns =
                    changedObject.ColumnChangeDates.Where(x => x.Value > lastChangeTime)
                        .ToDictionary(x => x.Key);

                foreach (var property in jObject.Properties().ToList())
                {
                    if (!changedColumns.ContainsKey(property.Name))
                    {
                        jObject.Remove(property.Name);
                    }
                }
                var downloadResponseItem = new DownloadResponseItem
                {
                    Type = changedObject.Type,
                    MobilePrimaryKey = changedObject.MobilePrimaryKey,
                    IsDeleted = changedObject.IsDeleted,
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
            foreach (var tag in tags.Where(x => !string.IsNullOrEmpty(x)))
            {
                if (request.LastChangeTime.TryGetValue(tag, out DateTimeOffset time))
                {
                    if (time > lastChangeTime)
                        lastChangeTime = time;
                }
            }

            return lastChangeTime;
        }

        internal IQueryable<SyncStatusServerObject> CreateQuery(DownloadDataRequest request, TUser user, SyncStatusDbContext context)
        {
            Expression<Func<SyncStatusServerObject, bool>> whereExpression = null;
            foreach (var userTag in GetTagsForUser(user))
            {
                Expression<Func<SyncStatusServerObject, bool>> condition = null;
                if (request.LastChangeTime.TryGetValue(userTag, out DateTimeOffset lastChange))
                {
                    condition = x =>
                        (x.Tag0 == userTag || x.Tag1 == userTag || x.Tag2 == userTag || x.Tag3 == userTag) &&
                        x.LastChange > lastChange;
                }
                else if (!request.OnlyDownloadSpecifiedTags)
                {
                    condition = x =>
                        !x.IsDeleted &&
                        (x.Tag0 == userTag || x.Tag1 == userTag || x.Tag2 == userTag || x.Tag3 == userTag);
                }

                if (condition != null)
                {
                    whereExpression = whereExpression == null ? condition : whereExpression.Or(condition);
                }
            }

            if (whereExpression == null)
                whereExpression = x => false;

            var changes = context.SyncStatusServerObjects.AsNoTracking()
                .Where(x => request.Types.Contains(x.Type)).Where(whereExpression);

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