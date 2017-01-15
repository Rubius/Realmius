using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Realms;

namespace RealmSync.SyncService
{
    public class RealmSyncService : IDisposable
    {
        private readonly Func<Realm> _realmFactoryMethod;
        private readonly Dictionary<string, Type> _typesToSync;
        private IApiClient _apiClient;

        private JsonSerializerSettings _jsonSerializerSettings;

        public Uri ServerUri { get; set; }
        private SyncConfiguration _syncOptions;

        public RealmSyncService(Func<Realm> realmFactoryMethod, IApiClient apiClient, params Type[] typesToSync)
        {
            _realmFactoryMethod = realmFactoryMethod;
            _apiClient = apiClient;
            var realmSyncData = CreateRealmSync();

            _typesToSync = typesToSync.ToDictionary(x => x.Name, x => x);
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
                if (!syncObjectInterface.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                    throw new InvalidOperationException($"Type {type} does not implement IRealmSyncObjectClient, unable to continue");

                var filter = (IQueryable<RealmObject>)realm.All(type.Name);
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
                var serializedCurrent = JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
                var className = obj.GetType().Name;

                realmSyncData.Write(() =>
                {
                    realmSyncData.Add(new UploadRequestItemRealm()
                    {
                        Type = className,
                        PrimaryKey = obj.MobilePrimaryKey,
                        SerializedObject = serializedCurrent,
                    });
                });
                var obj2 = (IRealmSyncObjectClient)realm.Find(className, obj.MobilePrimaryKey);
                realm.Write(() =>
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

                    realmSyncData.Write(() =>
                    {
                        realmSyncData.Add(new UploadRequestItemRealm()
                        {
                            Type = className,
                            PrimaryKey = obj.MobilePrimaryKey,
                            SerializedObject = serializedDiff,
                        });
                    });
                    var obj2 = (IRealmSyncObjectClient)realm.Find(className, obj.MobilePrimaryKey);
                    realm.Write(() =>
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

            var realmSyncData = CreateRealmSync();
            var realm = _realmFactoryMethod();

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
                    var result = await _apiClient.UploadData(changes);

                    realm.Write(() =>
                    {
                        foreach (var realmSyncObject in result.Results)
                        {
                            var obj = (IRealmSyncObjectClient)realm.Find(realmSyncObject.Type, realmSyncObject.MobilePrimaryKey);
							realmSyncData.Write(() =>
							{
								realmSyncData.Remove(objectsToUpload[realmSyncObject.MobilePrimaryKey]);
							});
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
                        var obj = (RealmObject)JsonConvert.DeserializeObject(changeObject.SerializedObject, _typesToSync[changeObject.Type]);
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
    }
}