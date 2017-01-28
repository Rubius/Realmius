using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RealmSync.SyncService;
using System.Reflection;
using RealmSync.Server.Models;

namespace RealmSync.Server
{
    public class RealmSyncServerProcessor : RealmSyncServerProcessor<string>
    {
        public RealmSyncServerProcessor(Func<DbContext> dbContextFactoryFunc, params Type[] syncedTypes) : base(dbContextFactoryFunc, syncedTypes)
        {
        }
    }

    public class RealmSyncServerProcessor<TUser>
    {
        private readonly Func<DbContext> _dbContextFactoryFunc;
        private readonly Dictionary<string, Type> _syncedTypes;
        private readonly JsonSerializer _serializer;

        public event EventHandler<UpdatedDataBatch> DataUpdated;
        protected virtual void OnDataUpdated(UpdatedDataBatch e)
        {
            DataUpdated?.Invoke(this, e);
        }

        public RealmSyncServerProcessor(Func<DbContext> dbContextFactoryFunc, params Type[] syncedTypes)
        {
            _dbContextFactoryFunc = dbContextFactoryFunc;
            _syncedTypes = syncedTypes.ToDictionary(x => x.Name, x => x);
            var syncObjectInterface = typeof(IRealmSyncObjectServer);
            foreach (var type in _syncedTypes.Values)
            {
                if (!syncObjectInterface.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                    throw new InvalidOperationException($"Type {type} does not implement IRealmSyncObjectServer, unable to continue");
            }
            _serializer = new JsonSerializer();
        }

        public UploadDataResponse Upload(UploadDataRequest request, TUser user)
        {
            var result = new UploadDataResponse();

            var updatedResult = new UpdatedDataBatch();

            var ef = _dbContextFactoryFunc();
            foreach (var item in request.ChangeNotifications)
            {
                UploadDataResponseItem objectInfo = null;

                try
                {
                    var type = _syncedTypes[item.Type];
                    objectInfo = new UploadDataResponseItem(item.PrimaryKey, item.Type)
                    {
                    };
                    result.Results.Add(objectInfo);

                    var dbSet = ef.Set(type);
                    var dbEntity = (IRealmSyncObjectServer)dbSet.Find(item.PrimaryKey);
                    if (dbEntity != null)
                    {
                        //entity exists in DB, UPDATE it
                        _serializer.Populate(new StringReader(item.SerializedObject), dbEntity);

                        if (!CheckAndProcess(dbEntity, user))
                        {
                            //revert all changes
                            ef.Entry(dbEntity).Reload();
                            objectInfo.Error = "Object failed security checks";
                        }
                        else
                        {
                            //dbEntity.LastChangeServer = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        //entity does not exist in DB, CREATE it
                        dbEntity = (IRealmSyncObjectServer)JsonConvert.DeserializeObject(item.SerializedObject, type);
                        if (CheckAndProcess(dbEntity, user))
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

                    updatedResult.Items.Add(new UpdatedDataItem()
                    {
                        DeserializedObject = dbEntity,
                        Change = new DownloadResponseItem()
                        {
                            MobilePrimaryKey = item.PrimaryKey,
                            Type = item.Type,
                            SerializedObject = item.SerializedObject,
                        },
                    });
                }
                catch (Exception e)
                {
                    if (objectInfo != null)
                    {
                        objectInfo.Error = e.ToString();
                    }
                    //continue processing anyway
                    //throw;
                }
            }
            ef.SaveChanges();

            OnDataUpdated(updatedResult);

            return result;
        }

        public DownloadDataResponse Download(DownloadDataRequest request, TUser user)
        {
            //var ef = _dbContextFactoryFunc();

            var response = new DownloadDataResponse()
            {
                LastChange = DateTime.UtcNow,
            };

            var types = request.Types.Except(_syncedTypes.Keys).ToList();
            if (types.Count > 0)
            {
                throw new Exception("Some types are not configured to be synced: " + string.Join(",", types));
            }

            var context = new SyncStatusDbContext();
            var changes = context.SyncStatusServerObjects.AsNoTracking()
                .Where(x => x.LastChange > request.LastChangeTime && request.Types.Contains(x.Type));

            response.ChangedObjects.AddRange(changes.Select(x => new DownloadResponseItem()
            {
                Type = x.Type,
                MobilePrimaryKey = x.MobilePrimaryKey,
                SerializedObject = x.ChangesAsJson,
            }));

            //foreach (var typeName in request.Types)
            //{
            //    var type = _syncedTypes[typeName];

            //    var setMethod = ef.GetType().GetMethod("Set", new Type[0]).MakeGenericMethod(type);
            //    var dbSet = (IQueryable<IRealmSyncObjectServer>)setMethod.Invoke(ef, new object[] { });
            //    //.AsNoTracking().Select(x => x);

            //    var updatedItems = GetUpdatedItems(type, dbSet, request.LastChangeTime, user);
            //    foreach (var realmSyncObject in updatedItems)
            //    {
            //        response.ChangedObjects.Add(new DownloadResponseItem()
            //        {
            //            Type = typeName,
            //            MobilePrimaryKey = realmSyncObject.MobilePrimaryKey,
            //            ChangesAsJson = JsonConvert.SerializeObject(realmSyncObject),
            //        });
            //    }
            //}

            return response;
        }

        ///// <summary>
        ///// returns items of a given type that were updated since lastChanged
        ///// </summary>
        ///// <returns></returns>
        //protected virtual IEnumerable<IRealmSyncObjectServer> GetUpdatedItems(Type type, IQueryable<IRealmSyncObjectServer> dbSet, DateTimeOffset lastChanged, TUser user)
        //{
        //    return dbSet.Where(x => x.LastChangeServer > lastChanged)
        //        .OrderByDescending(x => x.LastChangeServer);
        //}


        /// <summary>
        /// returns True if it's ok to save the object, False oterwise
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckAndProcess(IRealmSyncObjectServer deserialized, TUser user)
        {
            return true;
        }

        /// <summary>
        /// returns True if the user has access to the passed object, otherwise false
        /// </summary>
        /// <returns></returns>
        public virtual bool UserHasAccessToObject(IRealmSyncObjectServer deserialized, TUser user)
        {
            return true;
        }

    }
}