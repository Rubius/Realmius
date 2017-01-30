using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Realms;

namespace RealmSync.SyncService
{
    public class RealmSyncService : IRealmSyncService
    {
        private class TypeInfo
        {
            public Type Type { get; private set; }
            public bool ImplementsSyncState { get; private set; }

            private static Type syncObjectWithSyncStatusInterface = typeof(IRealmSyncObjectWithSyncStatusClient);
            public TypeInfo(Type type)
            {
                Type = type;
                ImplementsSyncState =
                    syncObjectWithSyncStatusInterface.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
            }
        }
        private readonly Func<Realm> _realmFactoryMethod;
        private readonly Dictionary<string, TypeInfo> _typesToSync;
        private IApiClient _apiClient;

        private JsonSerializerSettings _jsonSerializerSettings;

        private bool _uploadInProgress1;
        public bool UploadInProgress
        {
            get { return _uploadInProgress1; }
            private set
            {
                if (_uploadInProgress1 == value)
                    return;

                _uploadInProgress1 = value;
                OnPropertyChanged();
            }
        }

        public Uri ServerUri { get; set; }
        public SyncState GetSyncState(string mobilePrimaryKey)
        {
            return _realmFactoryMethod().Find<ObjectSyncStatusRealm>(mobilePrimaryKey).GetSyncState();
        }

        public SyncState GetFileSyncState(string mobilePrimaryKey)
        {
            return SyncState.Unsynced;
        }

        private SyncConfiguration _syncOptions;

        public RealmSyncService(Func<Realm> realmFactoryMethod, IApiClient apiClient, params Type[] typesToSync)
        {
            _realmFactoryMethod = realmFactoryMethod;
            _apiClient = apiClient;
            var realmSyncData = CreateRealmSync();

            _typesToSync = typesToSync.ToDictionary(x => x.Name, x => new TypeInfo(x));
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new RealmObjectResolver()
            };

            _syncOptions = realmSyncData.Find<SyncConfiguration>(1);
            if (_syncOptions == null)
            {
                _syncOptions = new SyncConfiguration()
                {
                    Id = 1,
                };
                realmSyncData.Write(() =>
                {
                    realmSyncData.Add(_syncOptions);
                });
            }


            Initialize();

            _apiClient.NewDataDownloaded += HandleDownloadedData;
            _apiClient.Start(new ApiClientStartOptions(_syncOptions.LastDownloaded, _typesToSync.Keys));
        }

        private Realm CreateRealmSync()
        {
            return Realm.GetInstance(new RealmConfiguration("realm.sync")
            {
                ShouldDeleteIfMigrationNeeded = true,
            });
        }

        private void HandleDownloadedData(object sender, DownloadDataResponse e)
        {
            HandleDownloadedData(e);
        }

        private void Initialize()
        {
            var realm = _realmFactoryMethod();
            var syncObjectInterface = typeof(IRealmSyncObjectClient);
            foreach (var type in _typesToSync.Values)
            {
                if (!syncObjectInterface.GetTypeInfo().IsAssignableFrom(type.Type.GetTypeInfo()))
                    throw new InvalidOperationException($"Type {type} does not implement IRealmSyncObjectClient, unable to continue");

                var filter = (IQueryable<RealmObject>)realm.All(type.Type.Name);
                filter.AsRealmCollection().SubscribeForNotifications(ObjectChanged);

            }

            CreateRealmSync().All<UploadRequestItemRealm>().AsRealmCollection().SubscribeForNotifications(UploadRequestItemChanged);
        }

        private void UploadRequestItemChanged(IRealmCollection<RealmObject> sender, ChangeSet changes, Exception error)
        {
            Upload();
        }

        private void ObjectChanged(IRealmCollection<RealmObject> sender, ChangeSet changes, Exception error)
        {
            if (changes == null)
                return;

            var realmSyncData = CreateRealmSync();
            var realm = _realmFactoryMethod();

            foreach (var changesInsertedIndex in changes.InsertedIndices)
            {
                var obj = (IRealmSyncObjectClient)sender[changesInsertedIndex];
                var className = obj.GetType().Name;
                if (_typesToSync.ContainsKey(className))
                    continue;

                var serializedCurrent = JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
                realmSyncData.Write(() =>
                {
                    realmSyncData.Add(new UploadRequestItemRealm()
                    {
                        Type = className,
                        PrimaryKey = obj.MobilePrimaryKey,
                        SerializedObject = serializedCurrent,
                    });

                    realmSyncData.Add(new ObjectSyncStatusRealm()
                    {
                        Type = className,
                        MobilePrimaryKey = obj.MobilePrimaryKey,
                        SerializedObject = serializedCurrent,
                        SyncState = (int)SyncState.Unsynced,
                    });

                    SetSyncState(realm, obj, SyncState.Unsynced);
                });
            }

            foreach (var changesModifiedIndex in changes.ModifiedIndices)
            {
                var obj = (IRealmSyncObjectClient)sender[changesModifiedIndex];
                var className = obj.GetType().Name;
                if (_typesToSync.ContainsKey(className))
                    continue;


                var serializedCurrent = JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
                var syncStatusObject = realmSyncData.Find<ObjectSyncStatusRealm>(obj.MobilePrimaryKey);

                var serializedDiff = JsonHelper.GetJsonDiff(syncStatusObject.SerializedObject ?? "{}", serializedCurrent);
                if (serializedDiff != "{}")
                {
                    realmSyncData.Write(() =>
                    {
                        realmSyncData.Add(new UploadRequestItemRealm()
                        {
                            Type = className,
                            PrimaryKey = obj.MobilePrimaryKey,
                            SerializedObject = serializedDiff,
                        });
                        syncStatusObject.SerializedObject = serializedCurrent;
                        syncStatusObject.SyncState = (int)SyncState.Unsynced;
                    });

                    if (_typesToSync[className].ImplementsSyncState)
                        SetSyncState(realm, obj, SyncState.Unsynced);

                }
            }

        }

        private void SetSyncState(Realm realm, IRealmSyncObjectClient obj, SyncState syncState)
        {
            var objWithSyncState = obj as IRealmSyncObjectWithSyncStatusClient;
            if (objWithSyncState != null)
            {
                realm.Write(() =>
                {
                    objWithSyncState.SyncStatus = (int)syncState;
                });
            }
        }

        private bool _uploadInProgress;
        public virtual async Task Upload()
        {
            if (_uploadInProgress)
                return;

            var realmSyncData = CreateRealmSync();
            var realm = _realmFactoryMethod();

            _uploadInProgress = true;
            var uploadSucceeeded = false;
            try
            {
                var objectsToUpload = GetObjectsToUpload().Take(10).ToDictionary(x => x.PrimaryKey, x => x);

                if (objectsToUpload.Count == 0)
                {
                    UploadInProgress = false;
                    return;
                }

                UploadInProgress = true;

                //var sendObjectsTime = DateTime.Now;
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
                    var result = await _apiClient.UploadData(changes);

                    foreach (var realmSyncObject in result.Results)
                    {
                        var syncStateObject =
                            realmSyncData.Find<ObjectSyncStatusRealm>(realmSyncObject.MobilePrimaryKey);

                        realmSyncData.Write(() =>
                        {
                            realmSyncData.Remove(objectsToUpload[realmSyncObject.MobilePrimaryKey]);
                            syncStateObject.SetSyncState(SyncState.Synced);
                        });

                        if (_typesToSync[realmSyncObject.Type].ImplementsSyncState)
                        {
                            var obj =
                                (IRealmSyncObjectWithSyncStatusClient)
                                realm.Find(realmSyncObject.Type, realmSyncObject.MobilePrimaryKey);

                            realm.Write(() =>
                            {
                                obj.SyncStatus = (int)SyncState.Synced;
                            });
                        }
                        //if (obj.DateTime > sendObjectsTime)
                        //{
                        //    //object has changed since we sent the result, will not change the SyncState
                        //}
                        //else
                        //{

                        //    obj.SetSyncState(SyncState.Synced);
                        //}
                    }
                    uploadSucceeeded = result.Results.Any();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{ex}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex}");
            }
            finally
            {
                _uploadInProgress = false;
            }

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    if (!uploadSucceeeded)
                        await Task.Delay(2000); //ToDo: delays might be increased in case of consequent errors

                    await Upload();
                }
                catch (Exception ex)
                {

                }
            });
        }

        private IEnumerable<UploadRequestItemRealm> GetObjectsToUpload()
        {
            return CreateRealmSync().All<UploadRequestItemRealm>().OrderBy(x => x.DateTime);
        }

        private async Task HandleDownloadedData(DownloadDataResponse result)
        {
            var realm = _realmFactoryMethod();
            await realm.WriteAsync((realmLocal) =>
            {
                foreach (var changeObject in result.ChangedObjects)
                {
                    var objInDb = realmLocal.Find(changeObject.Type, changeObject.MobilePrimaryKey);
                    if (objInDb == null)
                    {
                        var obj = (RealmObject)JsonConvert.DeserializeObject(changeObject.SerializedObject, _typesToSync[changeObject.Type].Type);
                        realm.Add(obj);
                    }
                    else
                    {
                        JsonConvert.PopulateObject(changeObject.SerializedObject, objInDb);
                    }
                }
            });

            if (result.ChangedObjects.Any())
            {
                CreateRealmSync().Write(() =>
                {
                    _syncOptions.LastDownloaded = result.LastChange;
                });
            }
        }

        public void Dispose()
        {
            _apiClient.NewDataDownloaded -= HandleDownloadedData;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}