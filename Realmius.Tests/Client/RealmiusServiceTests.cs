using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Realmius.Contracts.Models;
using Realmius.SyncService;
using Realmius.SyncService.ApiClient;
using Realmius.SyncService.RealmModels;
using Realmius.Tests.Server;
using Realms;

namespace Realmius.Tests.Client
{
    [TestFixture]
    public class RealmiusServiceTests : Base.TestBase
    {
        private Mock<IApiClient> _apiClientMock;
        private string _realmFileName;
        private RealmiusService _syncService;
        private Mock<RealmiusService> _syncServiceMock;
        protected UploadDataRequest _lastUploadRequest;
        protected List<UploadDataRequest> _uploadRequests;
        private int _uploadDataCounter = 0;
        private Task _uploadTask;

        [SetUp]
        public void Setup()
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

            _realmFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            RealmiusService.RealmiusDbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "sync");

            _syncServiceMock = CreateSyncService();

            _syncService = _syncServiceMock.Object;
            _uploadDataCounter = 0;
            _uploadTask = null;
            RealmiusService.DelayWhenUploadRequestFailed = 10;
        }

        [TearDown]
        public void TearDown()
        {
            _syncService.Dispose();
        }

        private Mock<RealmiusService> CreateSyncService()
        {
            Func<Realm> func = GetRealm;
            var mock = new Mock<RealmiusService>(func, _apiClientMock.Object, false, new[] { typeof(DbSyncClientObject), typeof(DbSyncClientObject2), typeof(DbSyncWithDoNotUpload) })
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
            return Realm.GetInstance(_realmFileName);
        }


        [Test]
        public void PartialUpdate_NoObjectOnClient()
        {
            var realm = GetRealm();
            realm.Refresh();

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem()
                    {
                        Type = nameof(DbSyncWithDoNotUpload),
                        MobilePrimaryKey = "456",
                        SerializedObject = "{\"Tags\":\"qwe\"}",
                    }
                }
            });
            Thread.Sleep(20);
            realm.Refresh();
            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();

            var obj2 = realm.Find<DbSyncWithDoNotUpload>("456");

            obj2.Should().NotBeNull();
            obj2.Tags.ShouldBeEquivalentTo("qwe");
            obj2.Text.Should().BeNullOrEmpty();

            _syncService.Realmius.All<UploadRequestItemRealm>().Count().Should().Be(0);
        }


        [Test]
        public void AddObject_UploadDataIsCalled()
        {

            var realm = GetRealm();


            var obj = new DbSyncClientObject()
            {
                Text = "zxczxczxc",
            };
            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();

            _uploadTask.Wait();

            _lastUploadRequest.Should().NotBeNull("UploadData should be called");
            string.Join(", ", _lastUploadRequest.ChangeNotifications)
                .Should().MatchEquivalentOf($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Id\": \"{obj.Id}\", \"Text\": \"zxczxczxc\", \"Tags\": null, \"MobilePrimaryKey\": \"{obj.Id}\"}}");
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Once);
        }


        [Test]
        public void AddObject_NotSucceeded_Update_UploadDataIsCalled()
        {
            var realm = GetRealm();

            var obj = new DbSyncClientObject()
            {
                Text = "zxczxczxc",
            };
            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();

            _uploadTask.Wait();
            SetupCorrectUploadResponse();
            realm.Write(() =>
            {
                obj.Text = "123";
            });
            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();

            _uploadTask.Wait();

            _lastUploadRequest.Should().NotBeNull("UploadData should be called");
            string.Join(", ", _lastUploadRequest.ChangeNotifications)
                .Should().MatchEquivalentOf($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Id\": \"{obj.Id}\", \"Text\": \"123\", \"Tags\": null, \"MobilePrimaryKey\": \"{obj.Id}\"}}");
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.AtLeast(2));
        }

        [Test]
        public void AddObject_Succeeded_Update_UploadDataIsCalled()
        {
            var realm = GetRealm();

            var obj = new DbSyncClientObject()
            {
                Text = "444",
            };

            SetupCorrectUploadResponse();

            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();

            _uploadTask.Wait();

            _uploadTask.Wait();
            _uploadTask = null;
            _lastUploadRequest = null;
            Console.WriteLine("Before update");
            realm.Write(() =>
            {
                obj.Text = "555";
            });
            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();

            Wait(() => _lastUploadRequest != null);

            _lastUploadRequest.Should().NotBeNull("UploadData should be called");
            string.Join(", ", _lastUploadRequest.ChangeNotifications)
                .Should().MatchEquivalentOf($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Text\": \"555\"}}");
            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.AtLeast(2));
        }



        [Test]
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
            obj2.Tags.ShouldBeEquivalentTo("qwe");
        }

        [Test]
        public void DoNotUploadAttribute_Upload()
        {
            var obj = new DbSyncWithDoNotUpload()
            {
                Text = "zxczxczxc",
                Tags = "zxc",
            };

            var res = _syncService.SerializeObject(obj);
            res.Should().NotContain("Tags");
        }


        [Test]
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
                        SerializedObject = _syncService.SerializeObject(obj),
                    }
                }
            });

            realm.Refresh();
            var obj2 = realm.Find<DbSyncClientObject>(key);

            obj2.Should().NotBeNull();
            obj2.Text.ShouldBeEquivalentTo("zxczxczxc");
            obj2.Id.ShouldBeEquivalentTo(key);

            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();


            _apiClientMock.Verify(x => x.UploadData(It.IsAny<UploadDataRequest>()), Times.Never);
        }


        [Test]
        public void Reconnect_LastChangeIsPreserved_IfFlagIsNotSpecified()
        {
            var offset = new DateTimeOffset(2017, 1, 1, 1, 1, 1, 1, TimeSpan.FromHours(0));

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", offset } },
            });
            _apiClientMock.Reset();

            var syncService = new RealmiusService(GetRealm, _apiClientMock.Object, false, new[] { typeof(DbSyncClientObject) });
            _apiClientMock.Verify(x => x.Start(It.Is<ApiClientStartOptions>(z => z.LastDownloaded.ContainsKey("all") == false && string.Join(", ", z.Types) == "DbSyncClientObject")), Times.Once);
            _apiClientMock.Reset();
        }


        [Test]
        public void Reconnect_LastChangeIsPreserved_FlagIsSpecified()
        {
            var offset = new DateTimeOffset(2017, 1, 1, 1, 1, 1, 1, TimeSpan.FromHours(0));

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", offset } },
                LastChangeContainsNewTags = true,
            });
            _apiClientMock.Reset();

            var syncService2 = new RealmiusService(GetRealm, _apiClientMock.Object, false, new[] { typeof(DbSyncClientObject) });
            _apiClientMock.Verify(x => x.Start(It.Is<ApiClientStartOptions>(z => z.LastDownloaded["all"] == offset && string.Join(", ", z.Types) == "DbSyncClientObject")));
        }


        [Test]
        public void HandleDownloadedData_NewObject_Updated()
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
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem()
                    {
                        Type = nameof(DbSyncClientObject),
                        MobilePrimaryKey = key,
                        SerializedObject = _syncService.SerializeObject(obj),
                    }
                }
            });
            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem()
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
            obj2.Text.ShouldBeEquivalentTo("qwe");
            obj2.Id.ShouldBeEquivalentTo(key);

            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();


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
        [Test]
        public void Serializer()
        {
            _syncService.SerializeObject(new Serializer1()
            {
                Id = "1",
                Text = "123",
                Time = new DateTimeOffset(2017, 1, 1, 1, 1, 1, 1, TimeSpan.FromHours(1)),
            }).ShouldBeEquivalentTo("{\"Id\":\"1\",\"Text\":\"123\",\"Time\":\"2017-01-01T01:01:01.001+01:00\",\"MobilePrimaryKey\":\"1\"}");
        }
        [Test]
        public void Serializer_Links1()
        {
            _syncService.SerializeObject(new Serializer_Link()
            {
                Id = "1",
                DbSyncClientObject = new DbSyncClientObject(),
            }).ShouldBeEquivalentTo("{\"Id\":\"1\",\"MobilePrimaryKey\":\"1\"}");
        }
        [Test]
        public void Serializer_Collection1()
        {
            _syncService.SerializeObject(new Serializer_Collection()
            {
                Id = "1",
                DbSyncClientObject =
                {
                    new DbSyncClientObject(),
                    new DbSyncClientObject(),
                }
            }).ShouldBeEquivalentTo("{\"Id\":\"1\",\"MobilePrimaryKey\":\"1\"}");
        }

        [Test]
        public void Dispose_NoNewNotificationsAreReceived()
        {
            _syncService.Dispose();
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(new DbSyncClientObject());
            });

            _syncServiceMock.Verify(x => x.ObjectChanged(It.IsAny<IRealmCollection<RealmObject>>(), It.IsAny<ChangeSet>(), It.IsAny<Exception>()), Times.Never);
        }


        [Test]
        public void Dispose_NotDisposed_ObjectChangedCalled()
        {
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(new DbSyncClientObject());
            });
            _syncService.Realm.Refresh();
            _syncServiceMock.Verify(x => x.ObjectChanged(It.IsAny<IRealmCollection<RealmObject>>(), It.Is<ChangeSet>(z => z != null), It.IsAny<Exception>()), Times.Once);
        }

        [Test]
        public void SyncEnabledAfterSomeObjectsWereInsertedInRealm_Modify()
        {
            _syncService.Dispose();

            var obj = new DbSyncClientObject()
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


        [Test]
        public void TwoObjects_DuplicateKeys()
        {
            var obj = new DbSyncClientObject()
            {
                Text = "123",
            };
            var obj2 = new DbSyncClientObject2()
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


            _syncService.Realm.Refresh();
            _uploadTask.Wait();
            _syncService.Realmius.Refresh();
            _uploadTask.Wait();
            //Thread.Sleep(100);

            _apiClientMock.Verify(z => z.UploadData(It.IsAny<UploadDataRequest>()), Times.AtLeastOnce);
            string.Join(", ", _uploadRequests.SelectMany(x => x.ChangeNotifications).Select(x => x.Type + ": " + x.PrimaryKey))
                .Should()
                .MatchEquivalentOf($"DbSyncClientObject: {obj.Id}, DbSyncClientObject2: {obj.Id}");
        }


        [Test]
        public void ObjectDownloadedWhenThereAreLocalChanges_ChangesPreserved()
        {
            var obj = new DbSyncClientObject()
            {
                Text = "123",
            };
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                ChangedObjects = new List<DownloadResponseItem>()
                {
                    new DownloadResponseItem()
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


        [Test]
        public void AddSkipUpload()
        {
            SetupCorrectUploadResponse();
            var obj = new DbSyncClientObject()
            {
                Text = "123",
                Tags = "qwe"
            };
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.AddSkipUpload(obj);
            });

            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();

            Thread.Sleep(70);
            _uploadDataCounter.Should().Be(0);

            realm.Write(
                () =>
                {
                    obj.Text = "zxc";
                });

            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();
            Thread.Sleep(20);

            _uploadDataCounter.Should().Be(1);
            string.Join(", ", _lastUploadRequest.ChangeNotifications).Should().BeEquivalentTo($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Text\": \"zxc\"}}");
        }


        [Test]
        public void SkipUpload()
        {
            SetupCorrectUploadResponse();
            var obj = new DbSyncClientObject()
            {
                Text = "123",
                Tags = "qwe"
            };
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(obj);
            });

            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();

            _uploadTask?.Wait();

            _uploadDataCounter.Should().Be(1);
            _uploadDataCounter = 0;

            realm.Write(
                () =>
                {
                    obj.Text = "zxc";
                    realm.SkipUpload(obj);
                });

            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();
            Thread.Sleep(20);

            _uploadDataCounter.Should().Be(0);


            realm.Write(
                () =>
                {
                    obj.Text = "qwe";
                });

            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();
            Thread.Sleep(20);

            _uploadDataCounter.Should().Be(1);
            string.Join(", ", _lastUploadRequest.ChangeNotifications).Should().BeEquivalentTo($"Type: DbSyncClientObject, PrimaryKey: {obj.Id}, SerializedObject: {{ \"Text\": \"qwe\"}}");
        }


        [Test]
        public void DeleteObject()
        {
            SetupCorrectUploadResponse();
            var obj = new DbSyncClientObject()
            {
                Text = "123",
                Tags = "qwe"
            };
            var realm = GetRealm();
            realm.Write(() =>
            {
                realm.Add(obj);
            });
            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();
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

            _syncService.Realm.Refresh();
            _syncService.Realmius.Refresh();
            _uploadTask.Wait();

            _uploadDataCounter.Should().Be(1);

            string.Join(", ", _lastUploadRequest.ChangeNotifications).Should().BeEquivalentTo($"Type: DbSyncClientObject, PrimaryKey: {id}, Deleted");
        }


        [Test]
        public void DownloadData_DeleteObject()
        {
            var realm = GetRealm();
            var key = "1";
            var obj = new DbSyncClientObject()
            {
                Text = "zxczxczxc",
                Id = key
            };
            realm.Write(
                () =>
                { realm.Add(obj); });

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
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


        [Test]
        public void DownloadData_NoObjectInDb()
        {
            var realm = GetRealm();

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem()
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
                    var response = new UploadDataResponse()
                    {
                        Results = x.ChangeNotifications.Select(z => new UploadDataResponseItem(z.PrimaryKey, z.Type)
                        {
                        }).ToList(),
                    };
                    return Task.FromResult(response);
                });
        }
    }
}