using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using PCLStorage;
using Realms;
using Realms.Schema;

namespace RealmSync.SyncService
{

    public class RealmSyncService : IRealmSyncService
    {
        public static int DelayWhenUploadRequestFailed = 2000;
        public static string RealmSyncDbPath = "realm.sync";
        internal bool InTests { get; set; }
        public string FileUploadUrl { get; set; }
        public string FileParameterName { get; set; } = "file";
        public event EventHandler<UnauthorizedResponse> Unauthorized;
        public event EventHandler DataDownloaded;
        public event EventHandler<FileUploadedEventArgs> FileUploaded;

        private readonly bool _isBuggedRealm;
        private readonly Func<Realm> _realmFactoryMethod;
        private readonly Dictionary<string, RealmObjectTypeInfo> _typesToSync;
        private readonly IApiClient _apiClient;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly object _handleDownloadDataLock = new object();
        private readonly string _realmDatabasePath;
        private static int _downloadIndex;
        public RealmSyncService(Func<Realm> realmFactoryMethod, IApiClient apiClient, bool deleteSyncDatabase, params Type[] typesToSync)
        {
            _realmFactoryMethod = realmFactoryMethod;
            _apiClient = apiClient;

            _isBuggedRealm = "1.1.2.0".Equals(GetRealmVersion());

            _typesToSync = typesToSync.ToDictionary(x => x.Name, x => new RealmObjectTypeInfo(x));
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new RealmObjectResolver()
            };

            if (deleteSyncDatabase)
            {
                DeleteDatabase();
            }
            else
            {
                Initialize();
            }
            var realm = realmFactoryMethod();
            _realmDatabasePath = realm.Config.DatabasePath;
            IList<RealmSyncService> syncServices;
            if (!SyncServiceFactory.SyncServices.TryGetValue(_realmDatabasePath, out syncServices))
            {
                syncServices = new List<RealmSyncService>();
                SyncServiceFactory.SyncServices[realm.Config.DatabasePath] = syncServices;
            }
            syncServices.Add(this);
        }

        private string GetRealmVersion()
        {
            return typeof(Realm).GetTypeInfo().Assembly.GetName().Version.ToString();
        }

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
        public SyncState GetSyncState(Type type, string mobilePrimaryKey)
        {
            return FindSyncStatus(CreateRealmSync(), type.Name, mobilePrimaryKey).GetSyncState();
        }

        public SyncState GetFileSyncState(string mobilePrimaryKey)
        {
            return SyncState.Unsynced;
        }

        private static RealmConfiguration RealmSyncConfiguration
        {
            get
            {
                return new RealmConfiguration(RealmSyncDbPath)
                {
                    ShouldDeleteIfMigrationNeeded = false,
                    ObjectClasses = new Type[]
                    {
                        typeof(ObjectSyncStatusRealm),
                        typeof(UploadFileInfo),
                        typeof(UploadRequestItemRealm),
                        typeof(SyncConfiguration),
                    },
                    SchemaVersion = 10,
                };
            }
        }
        private Realm CreateRealmSync()
        {
            return Realm.GetInstance(RealmSyncConfiguration);
        }

        public static void DeleteDatabaseWhenNotSyncing()
        {
            Realm.DeleteRealm(RealmSyncConfiguration);
        }

        public void DeleteDatabase()
        {
            DeleteDatabaseWhenNotSyncing();
            Unsubscribe();
            Initialize();
        }

        public void AttachNotLoadedObjects()
        {
            using (var realm = _realmFactoryMethod())
            {
                using (var realmSync = CreateRealmSync())
                {
                    foreach (string type in _typesToSync.Keys)
                    {
                        foreach (IRealmSyncObjectClient obj in realm.All(type))
                        {
                            if (FindSyncStatus(realmSync, type, obj.MobilePrimaryKey) == null)
                            {
                                HandleObjectChanged(obj, realmSync, realm);
                            }
                        }
                    }
                }
            }
        }

        private ObjectSyncStatusRealm FindSyncStatus(Realm realm, IRealmSyncObjectClient obj)
        {
            return realm.Find<ObjectSyncStatusRealm>(GetSyncStatusKey(obj));
        }
        private ObjectSyncStatusRealm FindSyncStatus(Realm realm, string typeName, string mobilePrimaryKey)
        {
            return realm.Find<ObjectSyncStatusRealm>(GetSyncStatusKey(typeName, mobilePrimaryKey));
        }

        private string GetSyncStatusKey(IRealmSyncObjectClient obj)
        {
            return GetSyncStatusKey(obj.GetType().Name, obj.MobilePrimaryKey);
        }
        private string GetSyncStatusKey(string typeName, string mobilePrimaryKey)
        {
            return $"{typeName}{ObjectSyncStatusRealm.SplitSymbols}{mobilePrimaryKey}";
        }

        private async void HandleDownloadedData(object sender, DownloadDataResponse e)
        {
            await HandleDownloadedData(e);
            OnDataDownloaded();
        }

        private Action _unsubscribeFromRealm = () => { };
        private Realm _strongReferencedRealm;
        private Realm _strongReferencedRealmSync;
        internal Realm Realm => _strongReferencedRealm;
        internal Realm RealmSync => _strongReferencedRealmSync;
        private void Initialize()
        {
            SyncConfiguration syncOptions;
            using (var realmSyncData = CreateRealmSync())
            {
                syncOptions = realmSyncData.Find<SyncConfiguration>(1);
                if (syncOptions == null)
                {
                    syncOptions = new SyncConfiguration()
                    {
                        Id = 1,
                    };
                    realmSyncData.Write(() =>
                    {
                        realmSyncData.Add(syncOptions);
                    });
                }


                _strongReferencedRealm = _realmFactoryMethod();
                var syncObjectInterface = typeof(IRealmSyncObjectClient);
                foreach (var type in _typesToSync.Values)
                {
                    if (!syncObjectInterface.GetTypeInfo().IsAssignableFrom(type.Type.GetTypeInfo()))
                        throw new InvalidOperationException($"Type {type} does not implement IRealmSyncObjectClient, unable to continue");

                    var filter = (IQueryable<RealmObject>)_strongReferencedRealm.All(type.Type.Name);
                    var subscribeHandler = filter.AsRealmCollection().SubscribeForNotifications(ObjectChanged);
                    _unsubscribeFromRealm += () => { subscribeHandler.Dispose(); };

                }

                _strongReferencedRealmSync = CreateRealmSync();
                var subscribe1 = _strongReferencedRealmSync.All<UploadRequestItemRealm>().AsRealmCollection().SubscribeForNotifications(UploadRequestItemChanged);
                _unsubscribeFromRealm += () => { subscribe1.Dispose(); };

                var subscribe2 = _strongReferencedRealmSync.All<UploadFileInfo>().AsRealmCollection().SubscribeForNotifications(UploadFileChanged);
                _unsubscribeFromRealm += () => { subscribe2.Dispose(); };

                _apiClient.Unauthorized += ApiClientOnUnauthorized;
                _apiClient.NewDataDownloaded += HandleDownloadedData;
                _apiClient.Start(new ApiClientStartOptions(syncOptions.LastDownloadedTags, _typesToSync.Keys));
            }
        }

        private void ApiClientOnUnauthorized(object sender, UnauthorizedResponse unauthorizedResponse)
        {
            Logger.Log.Info($"Unauthorized - reconnections stop. {unauthorizedResponse.Error}");
            _apiClient.Stop();
            OnUnauthorized(unauthorizedResponse);
        }

        private void UploadFileChanged(IRealmCollection<UploadFileInfo> sender, ChangeSet changes, Exception error)
        {
            UploadFiles();
        }

        private void UploadRequestItemChanged(IRealmCollection<RealmObject> sender, ChangeSet changes, Exception error)
        {
            StartUploadTask();
        }

        internal virtual void StartUploadTask()
        {
            Task.Factory.StartNew(() =>
            {
                Upload();
            });
        }

        internal string SerializeObject(IRealmSyncObjectClient obj)
        {
            return JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
        }

        private object _objectChangedLock = new object();
        private bool _skipObjectChanges = false;
        internal virtual void ObjectChanged(IRealmCollection<RealmObject> sender, ChangeSet changes, Exception error) //internal for Tests
        {
            if (changes == null)
                return;
            if (_skipObjectChanges)
                return;

            lock (_objectChangedLock)
            {
                try
                {
                    using (var realmSyncData = CreateRealmSync())
                    {
                        using (var realm = _realmFactoryMethod())
                        {
                            realmSyncData.Refresh();
                            foreach (var changesInsertedIndex in changes.InsertedIndices)
                            {
                                var obj = (IRealmSyncObjectClient)sender[changesInsertedIndex];

                                HandleObjectChanged(obj, realmSyncData, realm);
                            }

                            foreach (var changesModifiedIndex in changes.ModifiedIndices)
                            {
                                var obj = (IRealmSyncObjectClient)sender[changesModifiedIndex];

                                HandleObjectChanged(obj, realmSyncData, realm);
                            }

                            //delete can not be handled that way
                            /*
                            foreach (var changesDeletedIndex in changes.DeletedIndices)
                            {
                                var obj = (IRealmSyncObjectClient)sender[changesDeletedIndex];

                                HandleObjectChanged(obj, realmSyncData, realm, isDeleted: true);
                            }*/
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log.Exception(e);
                    throw;
                }
            }
        }

        private void HandleObjectChanged(IRealmSyncObjectClient obj, Realm realmSyncData, Realm realm, bool skipUpload = false, bool isDeleted = false)
        {
            var className = obj.GetType().Name;
            if (!_typesToSync.ContainsKey(className))
                return;

            var serializedCurrent = SerializeObject(obj);

            var syncStatusObject = FindSyncStatus(realmSyncData, className, obj.MobilePrimaryKey);
            if (syncStatusObject != null && syncStatusObject.SerializedObject == serializedCurrent && syncStatusObject.IsDeleted == isDeleted)
            {
                return; //could happen when new objects were downloaded from Server
            }
            if (!skipUpload && _skipObjectChanges)
                return;

            if (syncStatusObject == null)
            {
                realmSyncData.Write(() =>
                {
                    syncStatusObject = new ObjectSyncStatusRealm()
                    {
                        Type = className,
                        MobilePrimaryKey = obj.MobilePrimaryKey,
                        SerializedObject = null,
                        SyncState = (int)SyncState.Unsynced,
                    };
                    realmSyncData.Add(syncStatusObject);
                });
            }

            if (skipUpload)
            {
                realmSyncData.Write(
                        () =>
                        {
                            syncStatusObject.SerializedObject = serializedCurrent;
                            syncStatusObject.IsDeleted = isDeleted;
                            syncStatusObject.SyncState = (int)SyncState.Synced;
                        });
            }
            else
            {
                var serializedDiff = JsonHelper.GetJsonDiff(
                    syncStatusObject.SerializedObject ?? "{}",
                    serializedCurrent);
                if (serializedDiff != "{}" || isDeleted)
                {
                    realmSyncData.Write(
                        () =>
                        {
                            realmSyncData.Add(
                                new UploadRequestItemRealm()
                                {
                                    Type = className,
                                    IsDeleted = isDeleted,
                                    PrimaryKey = obj.MobilePrimaryKey,
                                    SerializedObject = serializedDiff,
                                    DateTime = DateTimeOffset.Now,
                                });
                            syncStatusObject.SerializedObject = serializedCurrent;
                            syncStatusObject.IsDeleted = isDeleted;
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



        private int _fileUploadsInProgress;
        public int ParallelFileUploads { get; set; } = 2;
        public virtual async Task UploadFiles()
        {
            if (_fileUploadsInProgress >= ParallelFileUploads)
                return;

            var uploadSucceeded = false;

            using (var realmSyncData = CreateRealmSync())
            {
                _fileUploadsInProgress++;
                string id = null;
                try
                {
                    var files = realmSyncData.All<UploadFileInfo>().Where(x => !x.UploadFinished).OrderBy(x => x.Added);
                    var file = _isBuggedRealm ? files.ToList().FirstOrDefault() : files.FirstOrDefault();

                    if (file == null)
                    {
                        _fileUploadsInProgress--;
                        return;
                    }

                    Logger.Log.Info($"File Uploading: started {file.PathToFile}");

                    id = file.Id;
                    var path = file.PathToFile;
                    var fileParameterName = file.FileParameterName;
                    var url = file.Url;
                    var queryParams = file.QueryParams;

                    var fullPath = Path.Combine(FileSystem.Current.LocalStorage.Path, path);
                    var fileContent = await FileSystem.Current.GetFileFromPathAsync(fullPath);
                    var stream = await fileContent.OpenAsync(FileAccess.Read);

                    var streamContent = new StreamContent(stream);

                    var client = GetHttpClient();
                    using (var content = new MultipartFormDataContent())
                    {

                        streamContent.Headers.Add("Content-Type", "application/octet-stream");
                        streamContent.Headers.Add(
                            "Content-Disposition",
                            $"form-data; name=\"{fileParameterName}\"; filename=\"{Path.GetFileName(path)}\"");

                        content.Add(streamContent, fileParameterName, Path.GetFileName(path));


                        if (!url.Contains("?"))
                        {
                            url = url + "?" + queryParams;
                        }
                        else
                        {
                            if (!url.EndsWith("&"))
                            {
                                url = url + "&";
                            }
                            url = url + queryParams;
                        }

                        var result = await client.PostAsync(url, content);
                        result.EnsureSuccessStatusCode();

                        Logger.Log.Info($"File Uploading: finished successfully {file.PathToFile}");

                        using (var realmSyncData2 = CreateRealmSync())
                        {
                            var file2 = realmSyncData2.Find<UploadFileInfo>(id);
                            realmSyncData2.Write(
                            () =>
                                { file2.UploadFinished = true; });

                            OnFileUploaded(new FileUploadedEventArgs(file2.AdditionalInfo, file2.QueryParams, file2.PathToFile));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Exception(ex, "Error during file upload");
                    if (id != null)
                        using (var realmSyncData2 = CreateRealmSync())
                        {
                            var file2 = realmSyncData2.Find<UploadFileInfo>(id);
                            realmSyncData2.Write(
                                () =>
                                { file2.Added = file2.Added.AddDays(2); });
                        }
                }
                finally
                {
                    _fileUploadsInProgress--;
                }
            }

            Task.Factory.StartNew(async () =>
            {
                if (!uploadSucceeded)
                    await Task.Delay(2000); //ToDo: delays might be increased in case of consequent errors

                await UploadFiles();
            });
        }

        private HttpClient GetHttpClient()
        {
            return new HttpClient();
        }


        private bool _uploadInProgress;
        private object _uploadLock = new object();
        private bool _disposed;

        public virtual async Task Upload()
        {
            if (_uploadInProgress || _disposed)
            {
                return;
            }
            if (!InTests)
            {
                lock (_uploadLock)
                {
                    if (_uploadInProgress)
                        return;
                    _uploadInProgress = true;
                }
            }
            var uploadSucceeded = false;


            Dictionary<string, List<string>> changesIds;
            var changes = new UploadDataRequest();
            try
            {
                using (var realmSyncData = CreateRealmSync())
                {
                    var objects = GetObjectsToUpload(realmSyncData);
                    var objectsToUpload = objects.GroupBy(
                        x => new
                        {
                            x.Type,
                            x.PrimaryKey
                        }).ToDictionary(x => x.Key);

                    changesIds =
                        objectsToUpload.ToDictionary(
                            x => GetSyncStatusKey(x.Key.Type, x.Key.PrimaryKey),
                            x => x.Value.Select(z => z.Id).ToList());
                    if (objectsToUpload.Count == 0)
                    {
                        UploadInProgress = false;
                        return;
                    }

                    UploadInProgress = true;

                    //var sendObjectsTime = DateTime.Now;

                    foreach (var uploadRequestItemRealm in objectsToUpload.Values)
                    {
                        //remove upload items that should not be synced anymore
                        if (!_typesToSync.ContainsKey(uploadRequestItemRealm.Key.Type))
                        {
                            realmSyncData.Write(
                                () =>
                                {
                                    foreach (var requestItemRealm in uploadRequestItemRealm)
                                    {
                                        realmSyncData.Remove(requestItemRealm);
                                    }
                                });
                            continue;
                        }
                        var serializedObject = MergeJsonStrings(uploadRequestItemRealm.Select(x => x.SerializedObject));
                        var changeNotification = new UploadRequestItem()
                        {
                            SerializedObject = serializedObject,
                            PrimaryKey = uploadRequestItemRealm.Key.PrimaryKey,
                            Type = uploadRequestItemRealm.Key.Type,
                            IsDeleted = uploadRequestItemRealm.Any(x => x.IsDeleted),
                        };
                        changes.ChangeNotifications.Add(changeNotification);
                        Logger.Log.Debug($"Up: {changeNotification.Type}.{changeNotification.PrimaryKey}: {changeNotification.SerializedObject}");
                    }
                }
                try
                {
                    var result = await _apiClient.UploadData(changes);
                    using (var realmSyncData = CreateRealmSync())
                    {
                        var notSynced = changes.ChangeNotifications.Select(x => new { x.Type, x.PrimaryKey })
                            .Except(result.Results.Where(x => x.IsSuccess).Select(x => new { x.Type, PrimaryKey = x.MobilePrimaryKey }));
                        if (notSynced.Any())
                        {
                            Logger.Log.Info($"Some objects were not accepted by the server: {string.Join("; ", notSynced.Select(x => x.Type + ": " + x.PrimaryKey))}");
                        }
                        using (var realm = _realmFactoryMethod())
                        {
                            foreach (var realmSyncObject in result.Results.Where(x => x.IsSuccess))
                            {
                                var syncStateObject = FindSyncStatus(
                                    realmSyncData,
                                    realmSyncObject.Type,
                                    realmSyncObject.MobilePrimaryKey);

                                realmSyncData.Write(
                                    () =>
                                    {
                                        foreach (var key in changesIds[GetSyncStatusKey(realmSyncObject.Type, realmSyncObject.MobilePrimaryKey)])
                                        {
                                            var uploadRequestItemRealm =
                                                realmSyncData.Find<UploadRequestItemRealm>(key);
                                            if (uploadRequestItemRealm == null)
                                            {
                                                var q = 1;
                                            }
                                            Logger.Log.Debug(
                                                $"Removed UploadRequest {uploadRequestItemRealm?.Id} for {realmSyncObject.Type}:{realmSyncObject.MobilePrimaryKey}");

                                            realmSyncData.Remove(uploadRequestItemRealm);

                                        }

                                        syncStateObject.SetSyncState(SyncState.Synced);
                                    });

                                if (_typesToSync[realmSyncObject.Type].ImplementsSyncState)
                                {
                                    var obj =
                                        (IRealmSyncObjectWithSyncStatusClient)
                                        realm.Find(realmSyncObject.Type, realmSyncObject.MobilePrimaryKey);

                                    realm.Write(
                                        () =>
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
                        }
                    }
                    uploadSucceeded = result.Results.Any();
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


#pragma warning disable 4014
            Task.Factory.StartNew(async () =>
#pragma warning restore 4014
                {
                    try
                    {
                        if (!InTests)
                        {
                            if (!uploadSucceeded)
                                await Task.Delay(DelayWhenUploadRequestFailed);
                            //ToDo: delays might be increased in case of consequent errors

                            await Upload();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log.Exception(e, $"Error in Upload");
                    }
                });
        }

        private string MergeJsonStrings(IEnumerable<string> objects)
        {
            JObject o1 = null;
            if (objects.Count() == 1)
                return objects.First();

            var mergeSettings = new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace
            };
            foreach (var obj in objects)
            {
                if (o1 == null)
                {
                    o1 = JObject.Parse(obj);
                }
                else
                {
                    JObject o2 = JObject.Parse(obj);
                    o1.Merge(o2, mergeSettings);
                }
            }

            return o1.ToString();
        }

        private IEnumerable<UploadRequestItemRealm> GetObjectsToUpload(Realm realmSync)
        {
            var items = realmSync.All<UploadRequestItemRealm>().OrderBy(x => x.DateTime);
            //var result = new List<UploadRequestItemRealm>();
            //var i = 0;
            //foreach (var item in items)
            //{
            //    result.Add(item);
            //    i++;
            //    if (i == 10)
            //        break;
            //}

            //return result;
            return items;
        }


        private async Task HandleDownloadedData(DownloadDataResponse result)
        {
            Logger.Log.Info($"HandleDownloadedData: {result?.LastChange}");
            lock (_handleDownloadDataLock)
            {
                try
                {
                    var realmLocal = _realmFactoryMethod();
                    var realmSyncData = CreateRealmSync();
                    realmSyncData.Refresh();

                    var changedObjects = result.ChangedObjects.ToList();

                    StoreChangedObjects(changedObjects, realmSyncData, realmLocal);

                    //if (result.ChangedObjects.Any())
                    {
                        var syncOptions = realmSyncData.Find<SyncConfiguration>(1);
                        foreach (KeyValuePair<string, DateTimeOffset> dateTimeOffset in result.LastChange)
                        {
                            if (result.LastChangeContainsNewTags || syncOptions.LastDownloadedTags.ContainsKey(dateTimeOffset.Key))
                                syncOptions.LastDownloadedTags[dateTimeOffset.Key] = dateTimeOffset.Value;
                        }
                        realmSyncData.Write(() =>
                        {
                            syncOptions.SaveLastDownloadedTags();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Info("HandleDownloadedData");
                    Logger.Log.Exception(ex);
                }
            }
        }

        private void StoreChangedObjects(List<DownloadResponseItem> changedObjects, Realm realmSyncData, Realm realmLocal)
        {
            var problematicChangedObjects = new List<DownloadResponseItem>();
            foreach (var changeObject in changedObjects)
            {
                _downloadIndex++;
                Logger.Log.Debug(
                    $"Down:{_downloadIndex}, {changeObject.Type}.{changeObject.MobilePrimaryKey}: {changeObject.SerializedObject}");
                try
                {
                    var syncStateObject = FindSyncStatus(realmSyncData, changeObject.Type, changeObject.MobilePrimaryKey);
                    if (syncStateObject == null)
                    {
                        syncStateObject = new ObjectSyncStatusRealm()
                        {
                            Type = changeObject.Type,
                            MobilePrimaryKey = changeObject.MobilePrimaryKey,
                            SyncState = (int)SyncState.Synced,
                        };
                    }

                    var objInDb = (IRealmSyncObjectClient)realmLocal.Find(changeObject.Type, changeObject.MobilePrimaryKey);
                    if (objInDb == null)
                    {
                        //object not found in database - let's create new one
                        if (!changeObject.IsDeleted)
                        {
                            IRealmSyncObjectClient obj =
                                (IRealmSyncObjectClient)Activator.CreateInstance(_typesToSync[changeObject.Type].Type);

                            try
                            {
                                _skipObjectChanges = true;
                                realmLocal.Write(
                                    () =>
                                    {

                                        AssignKey(obj, changeObject.MobilePrimaryKey, realmLocal);
                                        var success = Populate(changeObject.SerializedObject, obj, realmLocal);
                                        realmLocal.AddSkipUpload(obj, false);

                                        //    realmSyncData.Write(() =>
                                        //{
                                        //    syncStateObject.SerializedObject = SerializeObject(obj);
                                        //    if (!syncStateObject.IsManaged)
                                        //    {
                                        //        realmSyncData.Add(syncStateObject);
                                        //    }
                                        //});


                                        if (!success)
                                        {
                                            problematicChangedObjects.Add(changeObject);
                                        }


                                    });
                            }
                            finally
                            {
                                _skipObjectChanges = false;
                            }
                        }
                    }
                    else
                    {
                        //object exists in database, let's update it
                        var uploadItems =
                            realmSyncData.All<UploadRequestItemRealm>()
                                .Where(
                                    x =>
                                        x.Type == changeObject.Type &&
                                        x.PrimaryKey == changeObject.MobilePrimaryKey);

                        if (changeObject.IsDeleted)
                        {
                            realmLocal.Write(
                                () =>
                                {
                                    realmSyncData.Write(
                                        () =>
                                        {
                                            syncStateObject.SerializedObject = SerializeObject(objInDb);
                                            syncStateObject.IsDeleted = changeObject.IsDeleted;
                                        });

                                    realmLocal.Remove((RealmObject)objInDb);
                                });
                        }
                        else
                        {
                            try
                            {
                                _skipObjectChanges = true;
                                realmLocal.Write(
                                    () =>
                                    {


                                        var success = Populate(changeObject.SerializedObject, objInDb, realmLocal);
                                        if (!success)
                                        {
                                            problematicChangedObjects.Add(changeObject);
                                        }

                                        //we need to apply all "uploads" to our object to keep it consistent
                                        foreach (UploadRequestItemRealm uploadRequestItemRealm in uploadItems)
                                        {
                                            Populate(uploadRequestItemRealm.SerializedObject, objInDb, realmLocal);
                                        }


                                        realmSyncData.Write(
                                        () =>
                                        {
                                            syncStateObject.SerializedObject = SerializeObject(objInDb);
                                            syncStateObject.IsDeleted = changeObject.IsDeleted;
                                        });

                                    });
                            }
                            finally
                            {
                                _skipObjectChanges = false;
                            }
                        }


                        //Logger.Log.Info("     syncState.Change.Finished");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Debug($"error applying changed objects {ex}");
                }
            }

            if (problematicChangedObjects.Count < changedObjects.Count)
                StoreChangedObjects(problematicChangedObjects, realmSyncData, realmLocal);
        }


        private void AssignKey(IRealmSyncObjectClient realmSyncObjectClient, string key, Realm realmLocal)
        {
            var schema = realmLocal.Schema.Find(realmSyncObjectClient.GetType().Name);

            var keyPropertySchema = schema.FirstOrDefault(x => x.IsPrimaryKey);
            var keyProperty = realmSyncObjectClient.GetType().GetRuntimeProperty(keyPropertySchema.Name);
            if (keyPropertySchema.Type == PropertyType.String)
                keyProperty.SetValue(realmSyncObjectClient, key);
            if (keyPropertySchema.Type == PropertyType.Int)
                keyProperty.SetValue(realmSyncObjectClient, int.Parse(key));
        }

        protected internal bool Populate(string changeObjectSerializedObject, IRealmSyncObjectClient objInDb, Realm realm)
        {
            var serializer = new RealmReferencesSerializer()
            {
                Realm = realm,
            };
            var settings = new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>()
                {
                    serializer
                },
                ObjectCreationHandling = ObjectCreationHandling.Reuse,
            };
            JsonConvert.PopulateObject(changeObjectSerializedObject, objInDb, settings);

            return !serializer.NotFoundReferencesDetected;
        }

        private void Unsubscribe()
        {
            _apiClient.NewDataDownloaded -= HandleDownloadedData;
            _apiClient.Unauthorized += ApiClientOnUnauthorized;
            _unsubscribeFromRealm();
            _unsubscribeFromRealm = () => { };
        }
        public void Dispose()
        {
            _disposed = true;
            Unsubscribe();

            IList<RealmSyncService> syncServices;
            if (SyncServiceFactory.SyncServices.TryGetValue(_realmDatabasePath, out syncServices))
            {
                syncServices.Remove(this);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// puts the file uploading task to the queue
        /// </summary>
        /// <param name="pathToFile">path to file</param>
        /// <param name="queryParams">query parameters to pass to the server</param>
        /// <param name="fileUploadUrl">uri to upload file to (FileUploadUrl is used as default)</param>
        /// <param name="fileParameterName">server-side parameter name for a file (FileParameterName is used as default)</param>
        /// <param name="additionalInfo">any information that will be stored along with the file and passed back in </param>
        public void QueueFileUpload(string pathToFile,
            string queryParams = "",
            string fileUploadUrl = null,
            string fileParameterName = null,
            string additionalInfo = null)
        {
            if (string.IsNullOrEmpty(FileUploadUrl) && string.IsNullOrEmpty(fileUploadUrl))
                throw new InvalidOperationException(
                    $"Either {nameof(fileUploadUrl)} parameter or {nameof(FileUploadUrl)} property must be specified");

            if (string.IsNullOrEmpty(fileParameterName) && string.IsNullOrEmpty(FileParameterName))
                throw new InvalidOperationException(
                    $"Either {nameof(fileParameterName)} parameter or {nameof(FileParameterName)} property must be specified");

            var realm = CreateRealmSync();

            Uri uri1 = new Uri(pathToFile);
            Uri uri2 = new Uri(FileSystem.Current.LocalStorage.Path + "/");
            string relativePath = uri2.MakeRelativeUri(uri1).ToString();

            var fileInfo = new UploadFileInfo()
            {
                Added = DateTimeOffset.Now,
                Url = fileUploadUrl ?? FileUploadUrl,
                QueryParams = queryParams,
                PathToFile = relativePath,
                FileParameterName = fileParameterName ?? FileParameterName,
                AdditionalInfo = additionalInfo
            };
            realm.Write(
                () =>
                {
                    realm.Add(fileInfo);
                });
        }

        public void SkipUpload(IRealmSyncObjectClient realmObject)
        {
            var realmSync = CreateRealmSync();
            HandleObjectChanged(realmObject, realmSync, _realmFactoryMethod(), true);
        }

        protected virtual void OnUnauthorized(UnauthorizedResponse e)
        {
            Unauthorized?.Invoke(this, e);
        }

        public void RemoveObject(IRealmSyncObjectClient realmObject)
        {
            using (var realmSyncData = CreateRealmSync())
            {
                using (var realm = _realmFactoryMethod())
                {
                    HandleObjectChanged(realmObject, realmSyncData, realm, isDeleted: true);
                }
            }
        }

        protected virtual void OnDataDownloaded()
        {
            DataDownloaded?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnFileUploaded(FileUploadedEventArgs e)
        {
            FileUploaded?.Invoke(this, e);
        }
    }
}