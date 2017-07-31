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
using PCLStorage;
using Realmius.Contracts.Helpers;
using Realmius.Contracts.Models;
using Realmius.Infrastructure;
using Realmius.SyncService.ApiClient;
using Realmius.SyncService.RealmModels;
using Realms;
using Realms.Schema;
using Realmius.Contracts.Logger;

namespace Realmius.SyncService
{
    public class RealmiusSyncService : IRealmiusSyncService
    {
        public static int DelayWhenUploadRequestFailed = 2000;
        public static string RealmiusDbPath = "realm.sync";
        internal bool InTests { get; set; }
        public string FileUploadUrl { get; set; }
        public string FileParameterName { get; set; } = "file";

        public ILogger Logger
        {
            get { return _logger; }
            set
            {
                _logger = value;
                if (_apiClient is ILoggerAware apiILoggerAware)
                {
                    apiILoggerAware.Logger = value;
                }
            }
        }

        public event EventHandler<UnauthorizedResponse> Unauthorized;
        public event EventHandler DataDownloaded;
        public event EventHandler<FileUploadedEventArgs> FileUploaded;

        private Func<Realm> _realmFactoryMethod;
        private readonly Dictionary<string, RealmObjectTypeInfo> _typesToSync;
        private IApiClient _apiClient;
        private ILogger _logger;
        private JsonSerializerSettings _jsonSerializerSettings;
        private readonly object _handleDownloadDataLock = new object();
        private string _realmDatabasePath;
        private static int _downloadIndex;
        private string _syncServiceId;
        private Timer _delayedUploadsTriggerTimer;
        public RealmiusSyncService(Func<Realm> realmFactoryMethod, IApiClient apiClient, bool deleteSyncDatabase, params Type[] typesToSync)
        {
            _typesToSync = typesToSync.ToDictionary(x => x.Name, x => new RealmObjectTypeInfo(x));

            Initialize(realmFactoryMethod, apiClient, deleteSyncDatabase);
        }

        public RealmiusSyncService(Func<Realm> realmFactoryMethod, IApiClient apiClient, bool deleteSyncDatabase, Assembly assemblyWithModels)
        {
            var a = assemblyWithModels.ExportedTypes.Where(
                type => type.GetTypeInfo().IsClass &&
                        typeof(IRealmiusObjectClient).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()) &&
                        typeof(RealmObject).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()));

            _typesToSync = a.ToDictionary(x => x.Name, x => new RealmObjectTypeInfo(x));
            Initialize(realmFactoryMethod, apiClient, deleteSyncDatabase);
        }

        private void Initialize(Func<Realm> realmFactoryMethod, IApiClient apiClient, bool deleteSyncDatabase)
        {
            _syncServiceId = Guid.NewGuid().ToString();
            _realmFactoryMethod = realmFactoryMethod;
            _apiClient = apiClient;
            Logger = new Logger();

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
            IList<RealmiusSyncService> syncServices;
            if (!SyncServiceFactory.SyncServices.TryGetValue(_realmDatabasePath, out syncServices))
            {
                syncServices = new List<RealmiusSyncService>();
                SyncServiceFactory.SyncServices[realm.Config.DatabasePath] = syncServices;
            }
            syncServices.Add(this);
        }

        private string GetRealmVersion()
        {
            return typeof(Realm).GetTypeInfo().Assembly.GetName().Version.ToString();
        }

        private bool _uiUploadInProgress;
        /// <summary>
        /// this one can be shown in UI
        /// </summary>
        public bool UIUploadInProgress
        {
            get { return _uiUploadInProgress; }
            private set
            {
                if (_uiUploadInProgress == value)
                    return;

                _uiUploadInProgress = value;
                OnPropertyChanged();
            }
        }
        public Uri ServerUri { get; set; }
        public SyncState GetSyncState(Type type, string mobilePrimaryKey)
        {
            return FindSyncStatus(CreateRealmius(), type.Name, mobilePrimaryKey).GetSyncState();
        }

        public SyncState GetFileSyncState(string mobilePrimaryKey)
        {
            return SyncState.UnSynced;
        }

        private static RealmConfiguration RealmiusConfiguration => new RealmConfiguration(RealmiusDbPath)
        {
            ShouldDeleteIfMigrationNeeded = false,
            ObjectClasses = new[]
            {
                typeof(ObjectSyncStatusRealm),
                typeof(UploadFileInfo),
                typeof(UploadRequestItemRealm),
                typeof(SyncConfiguration),
            },
            SchemaVersion = 12,
        };

        private Realm CreateRealmius()
        {
            return Realm.GetInstance(RealmiusConfiguration);
        }

        public static void DeleteDatabaseWhenNotSyncing()
        {
            Realm.DeleteRealm(RealmiusConfiguration);
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
                using (var realmius = CreateRealmius())
                {
                    foreach (string type in _typesToSync.Keys)
                    {
                        foreach (IRealmiusObjectClient obj in realm.All(type))
                        {
                            if (FindSyncStatus(realmius, type, obj.MobilePrimaryKey) == null)
                            {
                                HandleObjectChanged(obj, realmius, realm);
                            }
                        }
                    }
                }
            }
        }

        private ObjectSyncStatusRealm FindSyncStatus(Realm realm, IRealmiusObjectClient obj)
        {
            return realm.Find<ObjectSyncStatusRealm>(GetSyncStatusKey(obj));
        }
        private ObjectSyncStatusRealm FindSyncStatus(Realm realm, string typeName, string mobilePrimaryKey)
        {
            return realm.Find<ObjectSyncStatusRealm>(GetSyncStatusKey(typeName, mobilePrimaryKey));
        }

        private string GetSyncStatusKey(IRealmiusObjectClient obj)
        {
            return GetSyncStatusKey(obj.GetType().Name, obj.MobilePrimaryKey);
        }
        private string GetSyncStatusKey(string typeName, string mobilePrimaryKey)
        {
            return $"{typeName}{ObjectSyncStatusRealm.SplitSymbols}{mobilePrimaryKey}";
        }

        private async void HandleDownloadedData(object sender, DownloadDataResponse e)
        {
            if (_disposed)
                return;

            await HandleDownloadedData(e);
            OnDataDownloaded();
        }

        private Action _unsubscribeFromRealm = () => { };
        private Realm _strongReferencedRealm;
        private Realm _strongReferencedRealmius;
        internal Realm Realm => _strongReferencedRealm;
        internal Realm Realmius => _strongReferencedRealmius;
        private void Initialize()
        {
            SyncConfiguration syncOptions;
            using (var realmius = CreateRealmius())
            {
                syncOptions = realmius.Find<SyncConfiguration>(1);
                if (syncOptions == null)
                {
                    syncOptions = new SyncConfiguration { Id = 1 };
                    realmius.Write(() => { realmius.Add(syncOptions); });
                }

                _strongReferencedRealm = _realmFactoryMethod();
                var syncObjectType = typeof(IRealmiusObjectClient).GetTypeInfo();
                foreach (var type in _typesToSync.Values)
                {
                    if (!syncObjectType.IsAssignableFrom(type.Type.GetTypeInfo()))
                        throw new InvalidOperationException($"Type {type} does not implement IRealmiusObjectClient, unable to continue");

                    var filter = (IQueryable<RealmObject>)_strongReferencedRealm.All(type.Type.Name);
                    var subscribeHandler = filter.AsRealmCollection().SubscribeForNotifications(ObjectChanged);
                    _unsubscribeFromRealm += () => { subscribeHandler.Dispose(); };
                }

                _strongReferencedRealmius = CreateRealmius();
                var now = DateTimeOffset.Now.AddSeconds(-1);
                var notUploaded = _strongReferencedRealmius.All<UploadRequestItemRealm>().Where(x => x.NextUploadAttemptDate > now);
                _strongReferencedRealmius.Write(() =>
                {
                    foreach (var item in notUploaded)
                    {
                        item.NextUploadAttemptDate = now;
                        item.UploadAttempts = 0;
                    }
                });
                var subscribe1 = _strongReferencedRealmius.All<UploadRequestItemRealm>().SubscribeForNotifications(UploadRequestItemChanged);
                _unsubscribeFromRealm += () => { subscribe1.Dispose(); };

                var subscribe2 = _strongReferencedRealmius.All<UploadFileInfo>().SubscribeForNotifications(UploadFileChanged);
                _unsubscribeFromRealm += () => { subscribe2.Dispose(); };

                _apiClient.Unauthorized += ApiClientOnUnauthorized;
                _apiClient.NewDataDownloaded += HandleDownloadedData;
                _apiClient.ConnectedStateChanged += ConnectedStateChanged;

                _apiClient.Start(new ApiClientStartOptions(syncOptions.LastDownloadedTags, _typesToSync.Keys));

                _delayedUploadsTriggerTimer = new Timer(TriggerDelayedUploads, null, 0, 10000, true);
            }
        }

        private async Task TriggerDelayedUploads(object state)
        {
            if (InTests)
                return;

            if (!_apiClient.IsConnected)
                return;

            Upload();
            UploadFiles();
        }

        void ConnectedStateChanged(object sender, EventArgs e)
        {
            if (_apiClient.IsConnected == true)
            {
                Upload();
                UploadFiles();
            }
        }

        private void ApiClientOnUnauthorized(object sender, UnauthorizedResponse unauthorizedResponse)
        {
            Logger.Info($"Unauthorized - reconnections stop. {unauthorizedResponse.Error}");
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

        internal string SerializeObject(IRealmiusObjectClient obj)
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
                    using (var realmius = CreateRealmius())
                    {
                        using (var realm = _realmFactoryMethod())
                        {
                            realmius.Refresh();
                            foreach (var changesInsertedIndex in changes.InsertedIndices)
                            {
                                var obj = (IRealmiusObjectClient)sender[changesInsertedIndex];

                                HandleObjectChanged(obj, realmius, realm);
                            }

                            foreach (var changesModifiedIndex in changes.ModifiedIndices)
                            {
                                var obj = (IRealmiusObjectClient)sender[changesModifiedIndex];

                                HandleObjectChanged(obj, realmius, realm);
                            }

                            //delete can not be handled that way
                            /*
                            foreach (var changesDeletedIndex in changes.DeletedIndices)
                            {
                                var obj = (IRealmiusObjectClient)sender[changesDeletedIndex];

                                HandleObjectChanged(obj, realmius, realm, isDeleted: true);
                            }*/
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Exception(e);
                    throw;
                }
            }
        }

        private void HandleObjectChanged(IRealmiusObjectClient obj, Realm realmiusData, Realm realm, bool skipUpload = false, bool isDeleted = false)
        {
            var className = obj.GetType().Name;
            if (!_typesToSync.ContainsKey(className))
                return;

            var serializedCurrent = SerializeObject(obj);

            realmiusData.Refresh();
            var syncStatusObject = FindSyncStatus(realmiusData, className, obj.MobilePrimaryKey);
            if (syncStatusObject != null && syncStatusObject.SerializedObject == serializedCurrent && syncStatusObject.IsDeleted == isDeleted)
            {
                return; //could happen when new objects were downloaded from Server
            }
            if (!skipUpload && _skipObjectChanges)
                return;

            if (syncStatusObject == null)
            {
                realmiusData.Write(() =>
                {
                    syncStatusObject = new ObjectSyncStatusRealm()
                    {
                        Type = className,
                        MobilePrimaryKey = obj.MobilePrimaryKey,
                        SerializedObject = null,
                        SyncState = (int)SyncState.UnSynced,
                    };
                    realmiusData.Add(syncStatusObject);
                });
                Logger.Debug($"  Created SyncStatus {obj.MobilePrimaryKey}");
            }

            if (skipUpload)
            {
                realmiusData.Write(
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
                    realmiusData.Write(
                        () =>
                        {
                            //Logger.Log.Debug("UploadRequestItemRealm added");
                            realmiusData.Add(
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
                            syncStatusObject.SyncState = (int)SyncState.UnSynced;
                        });


                    if (_typesToSync[className].ImplementsSyncState)
                        SetSyncState(realm, obj, SyncState.UnSynced);
                }
            }
        }

        private void SetSyncState(Realm realm, IRealmiusObjectClient obj, SyncState syncState)
        {
            var objWithSyncState = obj as IRealmiusObjectWithSyncStatusClient;
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
            if (!_apiClient.IsConnected)
                return;

            if (_fileUploadsInProgress >= ParallelFileUploads)
                return;

            var uploadSucceeded = false;

            using (var realmius = CreateRealmius())
            {
                _fileUploadsInProgress++;
                string id = null;
                try
                {
                    var files = realmius.All<UploadFileInfo>().Where(x => !x.UploadFinished).OrderBy(x => x.Added);
                    var file = files.FirstOrDefault();

                    if (file == null)
                    {
                        _fileUploadsInProgress--;
                        return;
                    }

                    Logger.Info($"File Uploading: started {file.PathToFile}");

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

                        Logger.Info($"File Uploading: finished successfully {path}");

                        using (var realmius2 = CreateRealmius())
                        {
                            var file2 = realmius2.Find<UploadFileInfo>(id);
                            realmius2.Write(
                            () =>
                                { file2.UploadFinished = true; });

                            OnFileUploaded(new FileUploadedEventArgs(file2.AdditionalInfo, file2.QueryParams, file2.PathToFile));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "Error during file upload");
                    if (id != null)
                        using (var realmius2 = CreateRealmius())
                        {
                            var file2 = realmius2.Find<UploadFileInfo>(id);
                            realmius2.Write(
                                () =>
                                { file2.Added = file2.Added.AddDays(2); });
                        }
                }
                finally
                {
                    _fileUploadsInProgress--;
                }
            }

            await Task.Factory.StartNew(async () =>
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
            //Logger.Log.Debug($"Attempt to Upload");
            if (_uploadInProgress || _disposed)
            {
                return;
            }
            if (!_apiClient.IsConnected)
                return;

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
                using (var realmius = CreateRealmius())
                {
                    realmius.Refresh();
                    var objects = GetObjectsToUpload(realmius);
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
                        return;
                    }

                    UIUploadInProgress = true;

                    foreach (var uploadRequestItemRealm in objectsToUpload.Values)
                    {
                        //remove upload items that should not be synced anymore
                        if (!_typesToSync.ContainsKey(uploadRequestItemRealm.Key.Type))
                        {
                            realmius.Write(
                                () =>
                                {
                                    foreach (var requestItemRealm in uploadRequestItemRealm)
                                    {
                                        realmius.Remove(requestItemRealm);
                                    }
                                });
                            continue;
                        }
                        var serializedObject = MergeJsonStrings(uploadRequestItemRealm.Select(x => x.SerializedObject).ToList());
                        var changeNotification = new UploadRequestItem()
                        {
                            SerializedObject = serializedObject,
                            PrimaryKey = uploadRequestItemRealm.Key.PrimaryKey,
                            Type = uploadRequestItemRealm.Key.Type,
                            IsDeleted = uploadRequestItemRealm.Any(x => x.IsDeleted),
                        };
                        changes.ChangeNotifications.Add(changeNotification);
                        Logger.Debug($"Up: {changeNotification.Type}.{changeNotification.PrimaryKey}: {changeNotification.SerializedObject}");
                    }
                }
                try
                {
                    var result = await _apiClient.UploadData(changes);
                    //Logger.Log.Debug($"Upload finished " + JsonConvert.SerializeObject(result));
                    using (var realmius = CreateRealmius())
                    {
                        realmius.Refresh();
                        var notSynced = changes.ChangeNotifications
                            .Select(x => new { x.Type, x.PrimaryKey })
                            .Except(result.Results.Where(x => x.IsSuccess)
                            .Select(x => new { x.Type, PrimaryKey = x.MobilePrimaryKey }))
                            .ToList();
                        if (notSynced.Count > 0)
                        {
                            var notSyncedObjects = notSynced.Select(x => $"{x.Type}: {x.PrimaryKey}");
                            Logger.Info($"Some objects were not accepted by the server: {string.Join("; ", notSyncedObjects)}");
                        }
                        using (var realm = _realmFactoryMethod())
                        {
                            foreach (var realmiusObject in result.Results)
                            {
                                if (realmiusObject.IsSuccess)
                                {
                                    var syncStateObject = FindSyncStatus(
                                    realmius,
                                    realmiusObject.Type,
                                    realmiusObject.MobilePrimaryKey);

                                    realmius.Write(
                                        () =>
                                        {
                                            try
                                            {
                                                foreach (var key in changesIds[GetSyncStatusKey(realmiusObject.Type, realmiusObject.MobilePrimaryKey)])
                                                {
                                                    try
                                                    {
                                                        var uploadRequestItemRealm = realmius.Find<UploadRequestItemRealm>(key);
                                                        Logger.Debug(
                                                            $"Removed UploadRequest {uploadRequestItemRealm?.Id} for {realmiusObject.Type}:{realmiusObject.MobilePrimaryKey}");

                                                        if (uploadRequestItemRealm != null)
                                                            realmius.Remove(uploadRequestItemRealm);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        //System.Console.WriteLine(e);
                                                        throw;
                                                    }
                                                }

                                                syncStateObject.SetSyncState(SyncState.Synced);
                                            }
                                            catch (Exception e)
                                            {
                                                //System.Console.WriteLine(e);
                                                throw;
                                            }
                                        });

                                    if (_typesToSync[realmiusObject.Type].ImplementsSyncState)
                                    {
                                        var obj =
                                            (IRealmiusObjectWithSyncStatusClient)
                                            realm.Find(realmiusObject.Type, realmiusObject.MobilePrimaryKey);

                                        realm.Write(
                                            () =>
                                            {
                                                obj.SyncStatus = (int)SyncState.Synced;
                                            });
                                    }
                                }
                                else
                                {
                                    realmius.Write(
                                        () =>
                                        {
                                            foreach (var key in changesIds[GetSyncStatusKey(realmiusObject.Type, realmiusObject.MobilePrimaryKey)])
                                            {
                                                var uploadRequestItemRealm =
                                                    realmius.Find<UploadRequestItemRealm>(key);

                                                uploadRequestItemRealm.UploadAttempts++;
                                                if (uploadRequestItemRealm.UploadAttempts > 30)
                                                {
                                                    Logger.Debug($"UploadRequest {realmiusObject.Type}.{realmiusObject.MobilePrimaryKey}, failed for attempts {uploadRequestItemRealm.UploadAttempts}, removing");

                                                    realmius.Remove(uploadRequestItemRealm);
                                                }
                                                else
                                                {
                                                    if (uploadRequestItemRealm.UploadAttempts >= 3)
                                                    {
                                                        uploadRequestItemRealm.NextUploadAttemptDate = DateTimeOffset.Now.AddSeconds(10 * uploadRequestItemRealm.UploadAttempts);
                                                    }

                                                    Logger.Debug($"Delaying UploadRequest {realmiusObject.Type}.{realmiusObject.MobilePrimaryKey}, attempt {uploadRequestItemRealm.UploadAttempts}, scheduled for {uploadRequestItemRealm.NextUploadAttemptDate}");
                                                }
                                            }
                                        });
                                }
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

                            Logger.Debug($"Upload requeued");
                            await Upload();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Exception(e, $"Error in Upload");
                    }
                });
        }

        private string MergeJsonStrings(IList<string> objects)
        {
            JObject o1 = null;
            if (objects.Count == 1)
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

        private IEnumerable<UploadRequestItemRealm> GetObjectsToUpload(Realm realmius)
        {
            var now = DateTimeOffset.Now;
            var items = realmius.All<UploadRequestItemRealm>()
                                 .Where(x => x.NextUploadAttemptDate < now)
                                 .OrderBy(x => x.DateTime);

            return items;
        }


        private async Task HandleDownloadedData(DownloadDataResponse result)
        {
            Logger.Info($"HandleDownloadedData: {result?.LastChange}");
            lock (_handleDownloadDataLock)
            {
                try
                {
                    var realmLocal = _realmFactoryMethod();
                    var realmius = CreateRealmius();
                    realmius.Refresh();
                    realmLocal.Refresh();

                    var changedObjects = result.ChangedObjects.ToList();

                    StoreChangedObjects(changedObjects, realmius, realmLocal);

                    //if (result.ChangedObjects.Any())
                    {
                        var syncOptions = realmius.Find<SyncConfiguration>(1);
                        foreach (KeyValuePair<string, DateTimeOffset> dateTimeOffset in result.LastChange)
                        {
                            if (result.LastChangeContainsNewTags || syncOptions.LastDownloadedTags.ContainsKey(dateTimeOffset.Key))
                                syncOptions.LastDownloadedTags[dateTimeOffset.Key] = dateTimeOffset.Value;
                        }
                        realmius.Write(() =>
                        {
                            syncOptions.SaveLastDownloadedTags();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info("HandleDownloadedData");
                    Logger.Exception(ex);
                }
            }
        }

        private void StoreChangedObjects(List<DownloadResponseItem> changedObjects, Realm realmiusData, Realm realmLocal)
        {
            var problematicChangedObjects = new List<DownloadResponseItem>();

            realmLocal.Write(() =>
            {
                foreach (var changeObject in changedObjects)
                {
                    _downloadIndex++;
                    string changedObjectInfo = !changeObject.IsDeleted
                        ? changeObject.SerializedObject
                        : "{" + $"\n  \"{nameof(changeObject.Type)}\": \"{changeObject.Type}\",\n  \"{nameof(changeObject.MobilePrimaryKey)}\": \"{changeObject.MobilePrimaryKey}\"\n  \"{nameof(changeObject.IsDeleted)}\": \"{changeObject.IsDeleted}\"\n" + "}";
                    Logger.Debug(
                        $"Down:{_downloadIndex}, {changeObject.Type}.{changeObject.MobilePrimaryKey}: {changedObjectInfo}");
                    try
                    {
                        var syncStateObject = FindSyncStatus(realmiusData, changeObject.Type, changeObject.MobilePrimaryKey);
                        if (syncStateObject == null)
                        {
                            syncStateObject = new ObjectSyncStatusRealm
                            {
                                Type = changeObject.Type,
                                MobilePrimaryKey = changeObject.MobilePrimaryKey,
                                SyncState = (int)SyncState.Synced,
                            };
                            realmiusData.Write(() =>
                            {
                                realmiusData.Add(syncStateObject);
                            });
                            //Logger.Log.Debug($"  Created SyncStatus {changeObject.MobilePrimaryKey}");
                        }

                        var objInDb = (IRealmiusObjectClient)realmLocal.Find(changeObject.Type, changeObject.MobilePrimaryKey);
                        if (objInDb == null)
                        {
                            //object not found in database - let's create new one
                            if (!changeObject.IsDeleted)
                            {
                                IRealmiusObjectClient obj =
                                    (IRealmiusObjectClient)Activator.CreateInstance(_typesToSync[changeObject.Type].Type);

                                try
                                {
                                    _skipObjectChanges = true;
                                    //realmLocal.Write(
                                    //    () =>
                                    //    {
                                    AssignKey(obj, changeObject.MobilePrimaryKey, realmLocal);
                                    var success = Populate(changeObject.SerializedObject, obj, realmLocal);
                                    realmLocal.AddSkipUpload(obj, false);

                                    if (!success)
                                    {
                                        problematicChangedObjects.Add(changeObject);
                                    }
                                    //});
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
                                realmiusData.All<UploadRequestItemRealm>()
                                    .Where(
                                        x =>
                                            x.Type == changeObject.Type &&
                                            x.PrimaryKey == changeObject.MobilePrimaryKey);

                            if (changeObject.IsDeleted)
                            {
                                //realmLocal.Write(
                                //    () =>
                                //    {
                                realmiusData.Write(
                                    () =>
                                    {
                                        syncStateObject.SerializedObject = SerializeObject(objInDb);
                                        syncStateObject.IsDeleted = changeObject.IsDeleted;
                                    });

                                realmLocal.Remove((RealmObject)objInDb);
                                //});
                            }
                            else
                            {
                                try
                                {
                                    _skipObjectChanges = true;
                                    //realmLocal.Write(
                                    //    () =>
                                    //    {


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

                                    realmiusData.Write(
                                    () =>
                                    {
                                        syncStateObject.SerializedObject = SerializeObject(objInDb);
                                        syncStateObject.IsDeleted = changeObject.IsDeleted;
                                    });

                                    //});
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
                        Logger.Debug($"error applying changed objects {ex}");
                    }
                }
            });

            if (problematicChangedObjects.Count < changedObjects.Count)
                StoreChangedObjects(problematicChangedObjects, realmiusData, realmLocal);
        }

        private void AssignKey(IRealmiusObjectClient realmiusObjectClient, string key, Realm realmLocal)
        {
            var schema = realmLocal.Schema.Find(realmiusObjectClient.GetType().Name);

            var keyPropertySchema = schema.FirstOrDefault(x => x.IsPrimaryKey);
            var keyProperty = realmiusObjectClient.GetType().GetRuntimeProperty(keyPropertySchema.Name);
            if (keyPropertySchema.Type == PropertyType.String)
                keyProperty.SetValue(realmiusObjectClient, key);
            if (keyPropertySchema.Type == PropertyType.Int)
                keyProperty.SetValue(realmiusObjectClient, int.Parse(key));
        }

        protected internal bool Populate(string changeObjectSerializedObject, IRealmiusObjectClient objInDb, Realm realm)
        {
            var serializer = new RealmReferencesSerializer
            {
                Realm = realm,
            };
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { serializer },
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
            Task.Factory.StartNew(() =>
            {
                try
                {
                    (_apiClient as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    // ignored
                }
            });


            Unsubscribe();

            lock (SyncServiceFactory.SyncServices)
            {
                IList<RealmiusSyncService> syncServices;
                if (SyncServiceFactory.SyncServices.TryGetValue(_realmDatabasePath, out syncServices))
                {
                    syncServices.Remove(this);
                }
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

            var realm = CreateRealmius();

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

        public void SkipUpload(IRealmiusObjectClient realmObject)
        {
            var realmius = CreateRealmius();
            HandleObjectChanged(realmObject, realmius, _realmFactoryMethod(), true);
        }

        protected virtual void OnUnauthorized(UnauthorizedResponse e)
        {
            Unauthorized?.Invoke(this, e);
        }

        public void RemoveObject(IRealmiusObjectClient realmObject)
        {
            using (var realmius = CreateRealmius())
            {
                using (var realm = _realmFactoryMethod())
                {
                    HandleObjectChanged(realmObject, realmius, realm, isDeleted: true);
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