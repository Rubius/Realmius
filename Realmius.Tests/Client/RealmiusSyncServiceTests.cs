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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Realmius.Contracts.Models;
using Realmius.SyncService;
using Realmius.SyncService.ApiClient;
using Realmius.SyncService.RealmModels;
using Realmius.Tests.Server;
using Realms;
using Xunit;

namespace Realmius.Tests.Client
{
    public class RealmiusSyncServiceTests : Base.TestBase//, IDisposable
    {
        private Mock<IApiClient> _apiClientMock;
        private string _realmFileName;
        private RealmiusSyncService _realmiusSyncService;
        private Mock<RealmiusSyncService> _syncServiceMock;
        protected UploadDataRequest _lastUploadRequest;
        protected List<UploadDataRequest> _uploadRequests;
        private int _uploadDataCounter = 0;
        private Task _uploadTask;

        public RealmiusSyncServiceTests()
        {

            _apiClientMock = new Mock<IApiClient>();

            _uploadRequests = new List<UploadDataRequest>();
            _lastUploadRequest = null;
            _apiClientMock.Setup(x => x.UploadData(It.IsAny<UploadDataRequest>()))
                .Callback<UploadDataRequest>(x =>
                {
                    _uploadDataCounter++;
                    _lastUploadRequest = x;
                    _uploadRequests.Add(x);
                }).ReturnsAsync(new UploadDataResponse());
            _apiClientMock.SetupGet(x => x.IsConnected).Returns(true);
            _realmFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            RealmiusSyncService.RealmiusDbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "sync");

            _syncServiceMock = CreateSyncService();

            _realmiusSyncService = _syncServiceMock.Object;
            _uploadDataCounter = 0;
            _uploadTask = null;
            RealmiusSyncService.DelayWhenUploadRequestFailed = 10;
        }


        public void Dispose()
        {
            _realmiusSyncService.Dispose();
        }

        private Mock<RealmiusSyncService> CreateSyncService()
        {
            Func<Realm> func = GetRealm;

            var mock = new Mock<RealmiusSyncService>(func, _apiClientMock.Object, false, new Type[]
            {
                typeof(DbSyncClientObject),
                typeof(DbSyncClientObject2),
                typeof(DbSyncWithDoNotUpload),
                typeof(RealmRef),
                typeof(RealmManyRef),
                typeof(Serializer1),
                typeof(Serializer_Link),
                typeof(Serializer_Collection),
            })
            {
                CallBase = true,
            };

            mock.Object.InTests = true;
            mock.Setup(x => x.StartUploadTask()).Callback(
                () =>
                {
                    Console.WriteLine("StartUploadTask");
                    _uploadTask = Task.Factory.StartNew(() =>
                    {
                        mock.Object.Upload().Wait();
                    });
                });

            return mock;
        }

        public Realm GetRealm()
        {
            return Realm.GetInstance(new RealmConfiguration(_realmFileName)
            {
                ObjectClasses = new[]
                {
                    typeof(DbSyncClientObject),
                    typeof(DbSyncClientObject2),
                    typeof(DbSyncWithDoNotUpload),
                    typeof(RealmRef),
                    typeof(RealmManyRef),
                    typeof(Serializer1),
                    typeof(Serializer_Link),
                    typeof(Serializer_Collection),
                }
            });
        }

        [Fact]
        public void PartialUpdate_NoObjectOnClient()
        {
            var realm = GetRealm();
            realm.Refresh();

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset> { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(DbSyncWithDoNotUpload),
                        MobilePrimaryKey = "456",
                        SerializedObject = "{\"Tags\":\"qwe\"}",
                    }
                }
            });
            Thread.Sleep(20);
            realm.Refresh();
            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            var obj2 = realm.Find<DbSyncWithDoNotUpload>("456");

            obj2.Should().NotBeNull();
            obj2.Tags.Should().BeEquivalentTo("qwe");
            obj2.Text.Should().BeNullOrEmpty();

            _realmiusSyncService.Realmius.All<UploadRequestItemRealm>().Count().Should().Be(0);
        }

        [Fact]
        public void AddObject_UploadDataIsCalled()
        {
            var realm = GetRealm();

            var obj = new DbSyncClientObject
            {
                Text = "zxczxczxc",
            };
            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            _uploadTask.Wait();

            _lastUploadRequest.Should().NotBeNull("UploadData should be called");
            string.Join(", ", _lastUploadRequest.ChangeNotifications)
                .Should().MatchEquivalentOf($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Id\": \"{obj.Id}\", \"Text\": \"zxczxczxc\", \"Tags\": null, \"MobilePrimaryKey\": \"{obj.Id}\"}}");
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Once);
        }

        [Fact]
        public void AddObject_NotConnected_DataNotSent_Connected_DataSent()
        {
            var realm = GetRealm();
            _apiClientMock.SetupGet(x => x.IsConnected).Returns(false);

            var obj = new DbSyncClientObject
            {
                Text = "zxczxczxc",
            };
            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            _uploadTask?.Wait();

            _lastUploadRequest.Should().BeNull("UploadData should not be called, because client is not connected");

            _apiClientMock.SetupGet(x => x.IsConnected).Returns(true);
            _apiClientMock.Raise(x => x.ConnectedStateChanged += null, _apiClientMock.Object, EventArgs.Empty);

            _uploadTask?.Wait();

            _lastUploadRequest.Should().NotBeNull("UploadData should be called after client is connected");


            string.Join(", ", _lastUploadRequest.ChangeNotifications)
                .Should().MatchEquivalentOf($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Id\": \"{obj.Id}\", \"Text\": \"zxczxczxc\", \"Tags\": null, \"MobilePrimaryKey\": \"{obj.Id}\"}}");
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Once);
        }

        [Fact]
        public void AddObject_NotSucceeded_Update_UploadDataIsCalled()
        {
            var realm = GetRealm();

            var obj = new DbSyncClientObject
            {
                Text = "zxczxczxc",
            };
            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            _uploadTask.Wait();
            SetupCorrectUploadResponse();
            realm.Write(() =>
            {
                obj.Text = "123";
            });
            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            _uploadTask.Wait();

            _lastUploadRequest.Should().NotBeNull("UploadData should be called");
            string.Join(", ", _lastUploadRequest.ChangeNotifications)
                .Should().MatchEquivalentOf($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Id\": \"{obj.Id}\", \"Text\": \"123\", \"Tags\": null, \"MobilePrimaryKey\": \"{obj.Id}\"}}");
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.AtLeast(2));
        }

        [Fact]
        public void Ctor_UsingAllRealmTypes()
        {
            var syncService = new RealmiusSyncService(GetRealm, _apiClientMock.Object, false, Assembly.GetExecutingAssembly());
            string.Join(", ", syncService._typesToSync.Select(x => x.Key))
                .Should().BeEquivalentTo("DbSyncClientObject, DbSyncClientObject2, DbSyncWithDoNotUpload, RealmManyRef, RealmRef");
        }


        [Fact]
        public void AddObject_NotSucceeded_DelayedDataUpload()
        {
            var realm = GetRealm();

            _syncServiceMock.Setup(x => x.StartUploadTask()).Callback(() => { });
            SetupIncorrectUploadResponse();
            _apiClientMock.ResetCalls();
            var obj = new DbSyncClientObject
            {
                Text = "zxczxczxc",
            };
            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            _realmiusSyncService.Upload().Wait();
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Exactly(1));

            _realmiusSyncService.Upload().Wait();
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Exactly(2));

            _realmiusSyncService.Upload().Wait();
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Exactly(3));

            //after 3 attempts upload should be delayed for several seconds
            _realmiusSyncService.Upload().Wait();
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Exactly(3));

            _realmiusSyncService.Upload().Wait();
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Exactly(3));

            var uploadRequestItem = _realmiusSyncService.Realmius.All<UploadRequestItemRealm>().First();
            uploadRequestItem.NextUploadAttemptDate.Should()
                .BeAfter(DateTimeOffset.Now + TimeSpan.FromSeconds(10))
                .And.BeBefore(DateTimeOffset.Now + TimeSpan.FromSeconds(60));

            _realmiusSyncService.Realmius.Write(() =>
            {
                uploadRequestItem.NextUploadAttemptDate = DateTimeOffset.Now;
            });
            _realmiusSyncService.Upload().Wait();
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Exactly(4));
        }

        [Fact]
        public void AddObject_Succeeded_Update_UploadDataIsCalled()
        {
            var realm = GetRealm();

            var obj = new DbSyncClientObject
            {
                Text = "444",
            };

            SetupCorrectUploadResponse();

            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            _uploadTask.Wait();

            _uploadTask.Wait();
            _uploadTask = null;
            _lastUploadRequest = null;
            Console.WriteLine("Before update");
            realm.Write(() =>
            {
                obj.Text = "555";
            });
            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            Wait(() => _lastUploadRequest != null);

            _lastUploadRequest.Should().NotBeNull("UploadData should be called");
            string.Join(", ", _lastUploadRequest.ChangeNotifications)
                .Should().MatchEquivalentOf($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Text\": \"555\"}}");
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.AtLeast(2));
        }

        [Fact]
        public void DoNotUploadAttribute_Download()
        {
            var realm = GetRealm();

            var key = Guid.NewGuid().ToString();
            var obj = new DbSyncWithDoNotUpload()
            {
                Text = "zxczxczxc",
                Id = key
            };

            realm.Write(() =>
            {
                realm.AddSkipUpload(obj);
            });

            realm.Refresh();

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem()
                    {
                        Type = nameof(DbSyncWithDoNotUpload),
                        MobilePrimaryKey = key,
                        SerializedObject = "{\"Id\":\""+key+"\", \"Tags\":\"qwe\", \"Text\":\"zxczxczxc\",\"MobilePrimaryKey\":\""+key+"\"}",
                    }
                }
            });
            Thread.Sleep(20);
            realm.Refresh();
            var obj2 = realm.Find<DbSyncWithDoNotUpload>(key);

            obj2.Should().NotBeNull();
            obj2.Tags.Should().BeEquivalentTo("qwe");
        }

        [Fact]
        public void DoNotUploadAttribute_Upload()
        {
            var obj = new DbSyncWithDoNotUpload()
            {
                Text = "zxczxczxc",
                Tags = "zxc",
            };

            var res = _realmiusSyncService.SerializeObject(obj);
            res.Should().NotContain("Tags");
        }

        [Fact]
        public void HandleDownloadedData_NewObject()
        {
            var realm = GetRealm();

            var key = Guid.NewGuid().ToString();
            var obj = new DbSyncClientObject()
            {
                Text = "zxczxczxc",
                Id = key
            };

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = DateTimeOffset.Now.ToDictionary(),
                ChangedObjects =
                {
                    new DownloadResponseItem()
                    {
                        Type = nameof(DbSyncClientObject),
                        MobilePrimaryKey = key,
                        SerializedObject = _realmiusSyncService.SerializeObject(obj),
                    }
                }
            });

            realm.Refresh();
            var obj2 = realm.Find<DbSyncClientObject>(key);

            obj2.Should().NotBeNull();
            obj2.Text.Should().BeEquivalentTo("zxczxczxc");
            obj2.Id.Should().BeEquivalentTo(key);

            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();


            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Never);
        }

        [Fact]
        public void Reconnect_LastChangeIsPreserved_IfFlagIsNotSpecified()
        {
            var offset = new DateTimeOffset(2017, 1, 1, 1, 1, 1, 1, TimeSpan.FromHours(0));

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", offset } },
            });
            _apiClientMock.Reset();

            var syncService = new RealmiusSyncService(GetRealm, _apiClientMock.Object, false, new[] { typeof(DbSyncClientObject) });
            _apiClientMock.Verify(x => x.Start(It.Is<ApiClientStartOptions>(z => z.LastDownloaded.ContainsKey("all") == false && string.Join(", ", z.Types) == "DbSyncClientObject")), Times.Once);
            _apiClientMock.Reset();
        }

        [Fact]
        public void Reconnect_LastChangeIsPreserved_FlagIsSpecified()
        {
            var offset = new DateTimeOffset(2017, 1, 1, 1, 1, 1, 1, TimeSpan.FromHours(0));

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", offset } },
                LastChangeContainsNewTags = true,
            });
            _apiClientMock.Reset();

            var syncService2 = new SyncService.RealmiusSyncService(GetRealm, _apiClientMock.Object, false, new[] { typeof(DbSyncClientObject) });
            _apiClientMock.Verify(x => x.Start(It.Is<ApiClientStartOptions>(z => z.LastDownloaded["all"] == offset && string.Join(", ", z.Types) == "DbSyncClientObject")));
        }

        [Fact]
        public void HandleDownloadedData_NewObject_Updated()
        {
            var realm = GetRealm();

            var key = Guid.NewGuid().ToString();
            var obj = new DbSyncClientObject
            {
                Text = "zxczxczxc",
                Id = key
            };

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(DbSyncClientObject),
                        MobilePrimaryKey = key,
                        SerializedObject = _realmiusSyncService.SerializeObject(obj),
                    }
                }
            });
            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(DbSyncClientObject),
                        MobilePrimaryKey = key,
                        SerializedObject = "{ Text: \"qwe\" }",
                    }
                }
            });

            realm.Refresh();
            var obj2 = realm.Find<DbSyncClientObject>(key);

            obj2.Should().NotBeNull();
            obj2.Text.Should().BeEquivalentTo("qwe");
            obj2.Id.Should().BeEquivalentTo(key);

            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Never);
        }

        private class Serializer1 : RealmObject, IRealmiusObjectClient
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public DateTimeOffset Time { get; set; }
            public string MobilePrimaryKey => Id;
        }
        private class Serializer_Link : RealmObject, IRealmiusObjectClient
        {
            public string Id { get; set; }
            public DbSyncClientObject DbSyncClientObject { get; set; }
            public string MobilePrimaryKey => Id;
        }
        private class Serializer_Collection : RealmObject, IRealmiusObjectClient
        {
            public string Id { get; set; }
            public IList<DbSyncClientObject> DbSyncClientObject { get; }
            public string MobilePrimaryKey => Id;
        }

        [Fact]
        public void Serializer()
        {
            _realmiusSyncService.SerializeObject(new Serializer1
            {
                Id = "1",
                Text = "123",
                Time = new DateTimeOffset(2017, 1, 1, 1, 1, 1, 1, TimeSpan.FromHours(1)),
            }).Should().BeEquivalentTo("{\"Id\":\"1\",\"Text\":\"123\",\"Time\":\"2017-01-01T01:01:01.001+01:00\",\"MobilePrimaryKey\":\"1\"}");
        }

        [Fact]
        public void Serializer_Links1()
        {
            _realmiusSyncService.SerializeObject(new Serializer_Link
            {
                Id = "1",
                DbSyncClientObject = new DbSyncClientObject(),
            }).Should().BeEquivalentTo("{\"Id\":\"1\",\"MobilePrimaryKey\":\"1\"}");
        }

        [Fact]
        public void Serializer_Collection1()
        {
            _realmiusSyncService.SerializeObject(new Serializer_Collection()
            {
                Id = "1",
                DbSyncClientObject =
                {
                    new DbSyncClientObject(),
                    new DbSyncClientObject(),
                }
            }).Should().BeEquivalentTo("{\"Id\":\"1\",\"MobilePrimaryKey\":\"1\"}");
        }

        [Fact]
        public void Dispose_NoNewNotificationsAreReceived()
        {
            _syncServiceMock.ResetCalls();
            _realmiusSyncService.Dispose();
            
            var realm = GetRealm();

            realm.Write(() =>
            {
                realm.Add(new DbSyncClientObject());
            });
            
            _syncServiceMock.Verify(x => x.ObjectChanged(It.IsAny<IRealmCollection<RealmObject>>(), It.IsAny<ChangeSet>(), It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public void Dispose_NotDisposed_ObjectChangedCalled()
        {
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(new DbSyncClientObject());
            });
            _realmiusSyncService.Realm.Refresh();
            _syncServiceMock.Verify(x => x.ObjectChanged(It.IsAny<IRealmCollection<RealmObject>>(), It.Is<ChangeSet>(z => z != null), It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public void SyncEnabledAfterSomeObjectsWereInsertedInRealm_Modify()
        {
            _realmiusSyncService.Dispose();

            var obj = new DbSyncClientObject
            {
                Text = "123",
            };
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(obj);
            });

            var syncServiceMock = CreateSyncService();
            var syncService = syncServiceMock.Object;

            var realm2 = GetRealm();
            var obj2 = realm2.Find<DbSyncClientObject>(obj.Id);
            _lastUploadRequest = null;

            realm2.Write(() =>
            {
                obj2.Text = "456";
                realm2.Add(obj2, true);
            });

            syncService.Realm.Refresh();
            syncService.Realmius.Refresh();
            syncServiceMock.Verify(x => x.ObjectChanged(It.Is<IRealmCollection<RealmObject>>(z => z != null), It.Is<ChangeSet>(z => z != null), It.IsAny<Exception>()), Times.Once);
            _uploadTask.Wait();
            Wait(() => _lastUploadRequest != null);

            _apiClientMock.Verify(z => z.UploadData(It.IsAny<UploadDataRequest>()), Times.Once);
            string.Join(", ", _lastUploadRequest.ChangeNotifications.Select(x => x.SerializedObject))
                .Should()
                .MatchEquivalentOf("{\r\n  \"Id\": \"" + obj.Id + "\",\r\n  \"Text\": \"456\",\r\n  \"Tags\": null,\r\n  \"MobilePrimaryKey\": \"" + obj.Id + "\"\r\n}");
        }

        [Fact]
        public void TwoObjects_DuplicateKeys()
        {
            var obj = new DbSyncClientObject
            {
                Text = "123",
            };
            var obj2 = new DbSyncClientObject2
            {
                Id = obj.Id,
                Text = "1234",
            };
            SetupCorrectUploadResponse();

            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(obj);
                realm.Add(obj2);
            });

            _realmiusSyncService.Realm.Refresh();
            _uploadTask.Wait();
            _realmiusSyncService.Realmius.Refresh();
            _uploadTask.Wait();
            //Thread.Sleep(100);

            _apiClientMock.Verify(z => z.UploadData(It.IsAny<UploadDataRequest>()), Times.AtLeastOnce);
            string.Join(", ", _uploadRequests.SelectMany(x => x.ChangeNotifications).Select(x => x.Type + ": " + x.PrimaryKey))
                .Should()
                .MatchEquivalentOf($"DbSyncClientObject: {obj.Id}, DbSyncClientObject2: {obj.Id}");
        }

        [Fact]
        public void ObjectDownloadedWhenThereAreLocalChanges_ChangesPreserved()
        {
            var obj = new DbSyncClientObject
            {
                Text = "123",
            };
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                ChangedObjects = new List<DownloadResponseItem>
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(DbSyncClientObject),
                        SerializedObject = "{Text: 'zxc' }",
                        MobilePrimaryKey = obj.MobilePrimaryKey,
                    }
                }
            });
            realm.Refresh();

            var obj2 = realm.Find<DbSyncClientObject>(obj.Id);
            obj2.Text.Should().BeEquivalentTo("123");
        }

        [Fact]
        public void AddSkipUpload()
        {
            SetupCorrectUploadResponse();
            var obj = new DbSyncClientObject
            {
                Text = "123",
                Tags = "qwe"
            };
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.AddSkipUpload(obj);
            });

            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            Thread.Sleep(70);
            _uploadDataCounter.Should().Be(0);

            realm.Write(
                () =>
                {
                    obj.Text = "zxc";
                });

            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();
            Thread.Sleep(20);

            _uploadDataCounter.Should().Be(1);
            string.Join(", ", _lastUploadRequest.ChangeNotifications).Should().BeEquivalentTo($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Text\": \"zxc\"}}");
        }

        [Fact]
        public void SkipUpload()
        {
            SetupCorrectUploadResponse();
            var obj = new DbSyncClientObject
            {
                Text = "123",
                Tags = "qwe"
            };
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(obj);
            });

            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();

            _uploadTask?.Wait();

            _uploadDataCounter.Should().Be(1);
            _uploadDataCounter = 0;

            realm.Write(
                () =>
                {
                    obj.Text = "zxc";
                    realm.SkipUpload(obj);
                });

            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();
            Thread.Sleep(20);

            _uploadDataCounter.Should().Be(0);

            realm.Write(
                () =>
                {
                    obj.Text = "qwe";
                });

            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();
            Thread.Sleep(20);

            _uploadDataCounter.Should().Be(1);
            string.Join(", ", _lastUploadRequest.ChangeNotifications).Should().BeEquivalentTo($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Text\": \"qwe\"}}");
        }

        [Fact]
        public void DeleteObject()
        {
            SetupCorrectUploadResponse();
            var obj = new DbSyncClientObject
            {
                Text = "123",
                Tags = "qwe"
            };
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();
            _uploadTask?.Wait();
            _uploadTask?.Wait();
            _uploadDataCounter = 0;

            var id = obj.Id;
            realm.Write(
                () =>
                {
                    realm.RemoveAndSync(obj);
                });
            _uploadTask.Wait();

            _realmiusSyncService.Realm.Refresh();
            _realmiusSyncService.Realmius.Refresh();
            _uploadTask.Wait();

            _uploadDataCounter.Should().Be(1);

            string.Join(", ", _lastUploadRequest.ChangeNotifications).Should().BeEquivalentTo($"Type: DbSyncClientObject, PrimaryKey: {id}, Deleted");
        }

        [Fact]
        public void DownloadData_DeleteObject()
        {
            var realm = GetRealm();
            var key = "1";
            var obj = new DbSyncClientObject
            {
                Text = "zxczxczxc",
                Id = key
            };
            realm.Write(
                () =>
                { realm.Add(obj); });

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem()
                    {
                        Type = nameof(DbSyncClientObject),
                        MobilePrimaryKey = obj.Id,
                        IsDeleted = true,
                        SerializedObject = "",
                    }
                }
            });

            realm.Refresh();

            var obj2 = realm.Find<DbSyncClientObject>(key);

            obj2.Should().BeNull();
        }

        [Fact]
        public void DownloadData_NoObjectInDb()
        {
            var realm = GetRealm();

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse
            {
                LastChange = new Dictionary<string, DateTimeOffset> { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(DbSyncClientObject),
                        MobilePrimaryKey = "123",
                        IsDeleted = true,
                        SerializedObject = "",
                    }
                }
            });

            realm.Refresh();

            var obj2 = realm.Find<DbSyncClientObject>("123");

            obj2.Should().BeNull();
        }

        private void SetupCorrectUploadResponse()
        {
            _apiClientMock.Setup(x => x.UploadData(It.IsAny<UploadDataRequest>()))
                .Returns((UploadDataRequest x) =>
                {
                    _uploadDataCounter++;

                    _lastUploadRequest = x;
                    _uploadRequests.Add(x);

                    Console.WriteLine("UploadData_1");
                    var response = new UploadDataResponse
                    {
                        Results = x.ChangeNotifications
                            .Select(z => new UploadDataResponseItem(z.PrimaryKey, z.Type))
                            .ToList(),
                    };
                    return Task.FromResult(response);
                });
        }

        private void SetupIncorrectUploadResponse()
        {
            _apiClientMock.Setup(x => x.UploadData(It.IsAny<UploadDataRequest>()))
                .Returns((UploadDataRequest x) =>
                {
                    _uploadDataCounter++;

                    _lastUploadRequest = x;
                    _uploadRequests.Add(x);

                    Console.WriteLine("UploadData_1");
                    var response = new UploadDataResponse
                    {
                        Results = x.ChangeNotifications
                            .Select(z => new UploadDataResponseItem(z.PrimaryKey, z.Type)
                            {
                                Error = "123",
                            })
                            .ToList(),
                    };
                    return Task.FromResult(response);
                });
        }
    }
}