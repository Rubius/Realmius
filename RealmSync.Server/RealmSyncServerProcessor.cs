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
    public class RealmSyncServerProcessor : RealmSyncServerProcessor<ISyncUser>
    {
        public RealmSyncServerProcessor(Func<DbContext> dbContextFactoryFunc,
            IRealmSyncServerConfiguration<ISyncUser> configuration)
            : base(dbContextFactoryFunc, configuration)
        {
        }

        /// <summary>
        /// this will all the types between all users!
        /// </summary>
        public RealmSyncServerProcessor(Func<DbContext> dbContextFactoryFunc, Type typeToSync, params Type[] typesToSync)
            : base(dbContextFactoryFunc, new ShareEverythingRealmSyncServerConfiguration(typeToSync, typesToSync))
        {
        }
    }

    public class RealmSyncServerProcessor<TUser>
        where TUser : ISyncUser
    {
        private readonly Func<DbContext> _dbContextFactoryFunc;
        public IRealmSyncServerConfiguration<TUser> Configuration { get; }
        private readonly Dictionary<string, Type> _syncedTypes;
        private readonly JsonSerializer _serializer;

        public event EventHandler<UpdatedDataBatch> DataUpdated;
        protected virtual void OnDataUpdated(UpdatedDataBatch e)
        {
            DataUpdated?.Invoke(this, e);
        }

        public RealmSyncServerProcessor(Func<DbContext> dbContextFactoryFunc, IRealmSyncServerConfiguration<TUser> configuration)
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
                    objectInfo = new UploadDataResponseItem(item.PrimaryKey, item.Type);
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
            if (user == null)
                throw new NullReferenceException("user arg cannot be null");

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
                .Where(x => x.LastChange > request.LastChangeTime &&
                request.Types.Contains(x.Type)
                && (user.Tags.Contains(x.Tag0) || user.Tags.Contains(x.Tag1) || user.Tags.Contains(x.Tag2) || user.Tags.Contains(x.Tag3)));

            response.ChangedObjects.AddRange(changes.Select(x => new DownloadResponseItem()
            {
                Type = x.Type,
                MobilePrimaryKey = x.MobilePrimaryKey,
                SerializedObject = x.ChangesAsJson,
            }));

            return response;
        }

        /// <summary>
        /// returns True if it's ok to save the object, False oterwise
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckAndProcess(IRealmSyncObjectServer deserialized, TUser user)
        {
            return Configuration.CheckAndProcess(deserialized, user);
        }

    }
}