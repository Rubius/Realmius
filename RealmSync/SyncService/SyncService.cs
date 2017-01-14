using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Realms;

namespace RealmSync.SyncService
{

    public class SyncStateOptions : RealmObject
    {
        [PrimaryKey]
        public int Id { get; set; }
        public DateTimeOffset LastDownloaded
        {
            get;
            set;
        } = new DateTimeOffset(new DateTime(1970, 1, 1));
    }

    public class RealmSyncService
    {
        private readonly Realm _realm;
        private readonly Dictionary<string, Type> _typesToSync;
        private SyncApiClient _syncApiClient;

        private JsonSerializerSettings _jsonSerializerSettings;
        private Realm _realmSyncData;

        public Uri ServerUri { get; set; }
        private SyncStateOptions _syncOptions;

        public RealmSyncService(Realm realm, Uri serverUploadUri, Uri serverDownloadUri, params Type[] typesToSync)
        {
            _realm = realm;
            _realmSyncData = Realm.GetInstance(new RealmConfiguration("realm.sync")
            {
                ShouldDeleteIfMigrationNeeded = true,
            });

            _typesToSync = typesToSync.ToDictionary(x => x.Name, x => x);
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new RealmObjectResolver()
            };

            _syncApiClient = new SyncApiClient(serverUploadUri, serverDownloadUri);

            _syncOptions = _realmSyncData.Find<SyncStateOptions>(1);
            if (_syncOptions == null)
            {
                _syncOptions = new SyncStateOptions()
                {
                    Id = 1,
                };
                _realmSyncData.Write(() =>
                {
                    _realmSyncData.Add(_syncOptions);
                });
            }
            Initialize();
        }

        private void Initialize()
        {
            var syncObjectInterface = typeof(IRealmSyncObjectClient);
            foreach (var type in _typesToSync.Values)
            {
                if (!syncObjectInterface.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                    throw new InvalidOperationException($"Type {type} does not implement IRealmSyncObject, unable to continue");

                var filter = (IQueryable<RealmObject>)_realm.All(type.Name);
                filter.AsRealmCollection().SubscribeForNotifications(ObjectChanged);

            }

            _realmSyncData.All<UploadRequestItemRealm>().AsRealmCollection().SubscribeForNotifications(UploadRequestItemChanged);
        }

        private void UploadRequestItemChanged(IRealmCollection<RealmObject> sender, ChangeSet changes, Exception error)
        {
            Upload();
        }

        private void ObjectChanged(IRealmCollection<RealmObject> sender, ChangeSet changes, Exception error)
        {
            if (changes == null)
                return;

            foreach (var changesInsertedIndex in changes.InsertedIndices)
            {
                var obj = (IRealmSyncObjectClient)sender[changesInsertedIndex];
                var serializedCurrent = JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
                var className = obj.GetType().Name;

                _realmSyncData.Write(() =>
                {
                    _realmSyncData.Add(new UploadRequestItemRealm()
                    {
                        Type = className,
                        PrimaryKey = obj.MobilePrimaryKey,
                        SerializedObject = serializedCurrent,
                    });
                });
                var obj2 = (IRealmSyncObjectClient)_realm.Find(className, obj.MobilePrimaryKey);
                _realm.Write(() =>
                {
                    try
                    {
                        obj2.LastSynchronizedVersion = serializedCurrent;
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.ToString());
                    }
                });
            }

            foreach (var changesModifiedIndex in changes.ModifiedIndices)
            {
                var obj = (IRealmSyncObjectClient)sender[changesModifiedIndex];
                var serializedCurrent = JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
                var serializedDiff = JsonHelper.GetJsonDiff(obj.LastSynchronizedVersion ?? "{}", serializedCurrent);
                if (serializedDiff != "{}")
                {
                    var className = obj.GetType().Name;

                    _realmSyncData.Write(() =>
                    {
                        _realmSyncData.Add(new UploadRequestItemRealm()
                        {
                            Type = className,
                            PrimaryKey = obj.MobilePrimaryKey,
                            SerializedObject = serializedDiff,
                        });
                    });
                    var obj2 = (IRealmSyncObjectClient)_realm.Find(className, obj.MobilePrimaryKey);
                    _realm.Write(() =>
                    {
                        try
                        {
                            obj2.LastSynchronizedVersion = serializedCurrent;
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine(ex.ToString());
                        }
                    });
                }
            }

        }

        private bool _uploadInProgress;
        public virtual async Task Upload()
        {
            if (_uploadInProgress)
                return;

            _uploadInProgress = true;
            var uploadSucceeeded = false;
            try
            {
                var objectsToUpload = GetObjectsToUpload().Take(10).ToDictionary(x => x.PrimaryKey, x => x);

                if (objectsToUpload.Count == 0)
                    return;

                var sendObjectsTime = DateTime.Now;
                var changes = new UploadDataRequest();
                foreach (UploadRequestItemRealm uploadRequestItemRealm in objectsToUpload.Values)
                {
                    var changeNotification = new UploadRequestItem()
                    {
                        SerializedObject = uploadRequestItemRealm.SerializedObject,
                        PrimaryKey = uploadRequestItemRealm.PrimaryKey,
                        Type = uploadRequestItemRealm.Type,
                    };
                    changes.ChangeNotifications.Add(changeNotification);
                }
                try
                {


                    var result = await _syncApiClient.UploadData(changes);

                    _realm.Write(() =>
                    {
                        foreach (var realmSyncObject in result.Results)
                        {
                            var obj = (IRealmSyncObjectClient)_realm.Find(realmSyncObject.Type, realmSyncObject.MobilePrimaryKey);
                            _realmSyncData.Remove(objectsToUpload[realmSyncObject.MobilePrimaryKey]);

                            obj.SetSyncState(SyncState.Synced);
                            //if (obj.DateTime > sendObjectsTime)
                            //{
                            //    //object has changed since we sent the result, will not change the SyncState
                            //}
                            //else
                            //{

                            //    obj.SetSyncState(SyncState.Synced);
                            //}
                        }
                    });
                    uploadSucceeeded = result.Results.Any();
                }
                catch (Exception ex)
                {

                }
            }
            finally
            {
                _uploadInProgress = false;
            }
            Task.Factory.StartNew(async () =>
            {
                if (!uploadSucceeeded)
                    await Task.Delay(2000);

                Upload();
            });
        }

        private IEnumerable<UploadRequestItemRealm> GetObjectsToUpload()
        {
            return _realmSyncData.All<UploadRequestItemRealm>().OrderBy(x => x.DateTime);
            //List<IRealmSyncObject> objectsToUpload = new List<IRealmSyncObject>();
            //foreach (var type in _typesToSync.Keys)
            //{
            //    var filter = (IQueryable<RealmObject>)_realm.All(type).Where(x => (x as IRealmSyncObject).SyncState == (int)SyncState.Unsynced);
            //    objectsToUpload.AddRange(filter.Cast<IRealmSyncObject>());
            //}

            ////objectsToUpload.OrderBy(x => x.LastChangeClient);
            //return objectsToUpload;
        }

        public virtual async Task Download()
        {
            //doesn't work for now

            var result = await _syncApiClient.DownloadData(new DownloadDataRequest()
            {
                LastChangeTime = _syncOptions.LastDownloaded.DateTime,
                Types = _typesToSync.Keys,
            });

            await _realm.WriteAsync((realm) =>
            {
                foreach (var changeObject in result.ChangedObjects)
                {
                    var objInDb = realm.Find(changeObject.Type, changeObject.MobilePrimaryKey);
                    if (objInDb == null)
                    {
                        var obj = (RealmObject)JsonConvert.DeserializeObject(changeObject.SerializedObject, _typesToSync[changeObject.Type]);
                        _realm.Add(obj);
                    }
                    else
                    {
                        JsonConvert.PopulateObject(changeObject.SerializedObject, objInDb);
                    }
                }
            });

            if (result.ChangedObjects.Any())
            {
                _realmSyncData.Write(() =>
                {
                    _syncOptions.LastDownloaded = result.LastChange;
                });
            }
        }
    }
}