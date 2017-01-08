using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RealmSync.SyncService;

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

                    //CheckAndProcess(deserialized);

                    var dbSet = ef.Set(type);
                    var dbEntity = dbSet.Find(item.PrimaryKey);
                    if (dbEntity != null)
                    {
                        //entity exists in DB, UPDATE it
                        _serializer.Populate(new StringReader(item.SerializedObject), dbEntity);
                    }
                    else
                    {
                        //entity does not exist in DB, CREATE it
                        var deserialized = JsonConvert.DeserializeObject(item.SerializedObject, type);
                        dbSet.Attach(deserialized);
                        ef.Entry(deserialized).State = EntityState.Added;
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

        protected virtual void CheckAndProcess(object deserialized)
        {
        }
    }
}