using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RealmSync.SyncService;
using System.Reflection;

namespace RealmSync.Server
{
    public class RealmSyncServerProcessor
    {
        private readonly Func<DbContext> _dbContextFactoryFunc;
        private readonly Dictionary<string, Type> _syncedTypes;
        private JsonSerializer _serializer;

        public RealmSyncServerProcessor(Func<DbContext> dbContextFactoryFunc, params Type[] syncedTypes)
        {
            _dbContextFactoryFunc = dbContextFactoryFunc;
            _syncedTypes = syncedTypes.ToDictionary(x => x.Name, x => x);
            var syncObjectInterface = typeof(IRealmSyncObjectServer);
            foreach (var type in _syncedTypes.Values)
            {
                if (!syncObjectInterface.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                    throw new InvalidOperationException($"Type {type} does not implement IRealmSyncObject, unable to continue");
            }
            _serializer = new JsonSerializer();
        }

        public UploadDataResponse Upload(UploadDataRequest request)
        {
            var result = new UploadDataResponse();

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
                    var dbEntity = dbSet.Find(item.PrimaryKey);
                    if (dbEntity != null)
                    {
                        //entity exists in DB, UPDATE it
                        _serializer.Populate(new StringReader(item.SerializedObject), dbEntity);

                        if (!CheckAndProcess(dbEntity))
                        {
                            //revert all changes
                            ef.Entry(dbEntity).Reload();
                        }
                        else
                        {
                            objectInfo.Error = "Object failed security checks";
                        }
                    }
                    else
                    {
                        //entity does not exist in DB, CREATE it
                        var deserialized = JsonConvert.DeserializeObject(item.SerializedObject, type);
                        if (CheckAndProcess(deserialized))
                        {
                            //add to the database
                            dbSet.Attach(deserialized);
                            ef.Entry(deserialized).State = EntityState.Added;
                        }
                        else
                        {
                            objectInfo.Error = "Object failed security checks";
                        }
                    }
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

            return result;
        }

        /// <summary>
        /// returns True if it's ok to save the object, False oterwise
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckAndProcess(object deserialized)
        {
            return true;
        }

        public DownloadDataResponse Download(DownloadDataRequest request)
        {
            var ef = _dbContextFactoryFunc();

            var response = new DownloadDataResponse();

            foreach (var typeName in request.Types)
            {
                var type = _syncedTypes[typeName];
                var dbSet = ef.Set(type).Cast<IRealmSyncObjectServer>();

                var updatedItems = GetUpdatedItems(type, dbSet, request.LastChangeTime);
                foreach (var realmSyncObject in updatedItems)
                {
                    response.ChangedObjects.Add(new DownloadRequestItem()
                    {
                        Type = typeName,
                        MobilePrimaryKey = realmSyncObject.MobilePrimaryKey,
                        SerializedObject = JsonConvert.SerializeObject(realmSyncObject),
                    });
                }
            }

            return response;
        }

        protected virtual IEnumerable<IRealmSyncObjectServer> GetUpdatedItems(Type type, DbSet<IRealmSyncObjectServer> dbSet, DateTime lastChanged)
        {
            return dbSet.Where(x => x.LastChangeServer > lastChanged)
                .OrderByDescending(x => x.LastChangeServer);
        }
    }
}