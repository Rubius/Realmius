using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Realms;

namespace RealmSync.SyncService
{

    public class RealmSyncService
    {
        private readonly Realm _realm;
        private readonly Dictionary<string, Type> _typesToSync;
        private SyncApiClient _syncApiClient;

        private DateTimeOffset _lastDownloadTime = DateTimeOffset.MinValue;
        private JsonSerializerSettings _jsonSerializerSettings;
        private Realm _realmSyncData;

        public Uri ServerUri { get; set; }

        public RealmSyncService(Realm realm, Uri serverUri, params Type[] typesToSync)
        {
            ServerUri = serverUri;
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

            _syncApiClient = new SyncApiClient(new Uri(ServerUri, "upload"), new Uri(ServerUri, "download"));

            Initialize();
        }

        private void Initialize()
        {
            var syncObjectInterface = typeof(IRealmSyncObject);
            foreach (var type in _typesToSync.Values)
            {
                if (!syncObjectInterface.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                    throw new InvalidOperationException($"Type {type} does not implement IRealmSyncObject, unable to continue");

                var filter = (IQueryable<RealmObject>)_realm.All(type.Name);
                filter.AsRealmCollection().SubscribeForNotifications(ObjectChanged);


                //if (lastChange > _lastDownloadTime)
                //    _lastDownloadTime = lastChange;

                //var filter = (IQueryable<RealmObject>)_realm.All(type.Name).Where(x => (x as IRealmSyncObject).SyncState == SyncState.Unsynced);
                //filter.AsRealmCollection().SubscribeForNotifications(ObjectChanged);
            }
        }

        private void ObjectChanged(IRealmCollection<RealmObject> sender, ChangeSet changes, Exception error)
        {
            if (changes == null)
                return;

            foreach (var changesInsertedIndex in changes.InsertedIndices)
            {
                var obj = (IRealmSyncObject)sender[changesInsertedIndex];
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
                var obj2 = (IRealmSyncObject)_realm.Find(className, obj.MobilePrimaryKey);
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
                var obj = (IRealmSyncObject)sender[changesModifiedIndex];
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
                    var obj2 = (IRealmSyncObject)_realm.Find(className, obj.MobilePrimaryKey);
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

        public virtual async Task Upload()
        {
            var objectsToUpload = GetObjectsToUpload().Take(10).ToDictionary(x => x.PrimaryKey, x => x);

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

            var result = await _syncApiClient.UploadData(changes);

            _realm.Write(() =>
            {
                foreach (var realmSyncObject in result.Results)
                {
                    var obj = objectsToUpload[realmSyncObject.MobilePrimaryKey];
                    if (obj.DateTime > sendObjectsTime)
                    {
                        //object has changed since we sent the result, will not change the SyncState
                    }
                    else
                    {

                        obj.SetSyncState(SyncState.Synced);
                    }
                }
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
            var result = await _syncApiClient.DownloadData(new DownloadDataRequest()
            {
                LastChangeTime = _lastDownloadTime.DateTime,
                Types = _typesToSync.Keys,
            });

            await _realm.WriteAsync((realm) =>
            {
                foreach (var changeObject in result.ChangedObjects)
                {
                    var objInDb = realm.Find(changeObject.Type, changeObject.Type);
                    if (objInDb == null)
                    {

                    }
                    else
                    {

                    }
                }
            });

        }
    }
}