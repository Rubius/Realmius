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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Realmius.Contracts.Models;
using Realmius.SyncService;
using Realmius.SyncService.ApiClient;
using Realms;
using Xunit;

namespace Realmius.Tests.Client
{
    public class RealmReferenceTests
    {
        private Mock<IApiClient> _apiClientMock;
        private string _realmFileName;
        private RealmiusSyncService _realmiusSyncService;
        private Mock<RealmiusSyncService> _syncServiceMock;
        protected UploadDataRequest _lastUploadRequest;
        protected List<UploadDataRequest> _uploadRequests;
        private int _uploadDataCounter = 0;
        private Task _uploadTask;

        public RealmReferenceTests()
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
            RealmiusSyncService.RealmiusDbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "sync");

            _syncServiceMock = CreateSyncService();
            _syncServiceMock.Setup(x => x.StartUploadTask()).Callback(
                () =>
                {
                    _uploadTask = Task.Factory.StartNew(() =>
                    {
                        _syncServiceMock.Object.Upload();
                    });
                });
            _realmiusSyncService = _syncServiceMock.Object;
            _uploadDataCounter = 0;
            _uploadTask = null;
        }

        private Mock<RealmiusSyncService> CreateSyncService()
        {
            Func<Realm> func = GetRealm;
            return new Mock<RealmiusSyncService>(func, _apiClientMock.Object, false, Assembly.GetExecutingAssembly())
            {
                CallBase = true,
            };
        }

        public Realm GetRealm()
        {
            return Realm.GetInstance(_realmFileName);
        }

        [Fact]
        public void Many_Serialize_Deserialize()
        {
            var realm = GetRealm();

            var ref1 = new RealmRef
            {
                Text = "123",
                Id = "1",
            };
            var ref2 = new RealmRef
            {
                Text = "456",
                Id = "2",
            };
            var refParent = new RealmManyRef
            {
                Id = "3",
                Text = "zxc",
            };
            refParent.Children.Add(ref1);
            refParent.Children.Add(ref2);

            realm.Write(
                () =>
                {
                    realm.Add(ref1);
                    realm.Add(ref2);
                    realm.Add(refParent);
                });

            var s1 = _realmiusSyncService.SerializeObject(ref1);
            s1.Should().BeEquivalentTo($"{{\"Id\":\"1\",\"Text\":\"123\",\"Parent\":null,\"MobilePrimaryKey\":\"1\"}}");

            var s2 = _realmiusSyncService.SerializeObject(refParent);
            s2.Should().BeEquivalentTo("{\"Id\":\"3\",\"Text\":\"zxc\",\"Children\":[\"1\",\"2\"],\"MobilePrimaryKey\":\"3\"}");

            realm.Write(
                () =>
                {
                    realm.Remove(refParent);
                });

            var ref11 = new RealmManyRef();
            realm.Write(
                () =>
                {
                    _realmiusSyncService.Populate(s2, ref11, realm);
                });

            ref11.Text.Should().Be("zxc");
            ref11.Id.Should().Be("3");
            ref11.Children.Should().NotBeNull();
            string.Join(", ", ref11.Children.Select(x => x.Id)).Should().BeEquivalentTo("1, 2");
        }

        [Fact]
        public void ManyRef_DownloadData_ReferencesCorrectOrder()
        {
            var realm = GetRealm();

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset> { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmRef),
                        MobilePrimaryKey = "1",
                        SerializedObject = "{ 'Id': '1', Text:'123', Parent: null}",
                    },
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmManyRef),
                        MobilePrimaryKey = "2",
                        SerializedObject = "{ 'Id': '2', Text:'345', Children: ['1']}",
                    }
                }
            });

            realm.Refresh();
            var parent = realm.All<RealmManyRef>().First();
            parent.Id.Should().Be("2");
            string.Join(", ", parent.Children.Select(x => x.Id)).Should().BeEquivalentTo("1");
        }

        [Fact]
        public void ManyRef_DownloadData_NoChildren()
        {
            var realm = GetRealm();
            
            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmManyRef),
                        MobilePrimaryKey = "2",
                        SerializedObject = "{ 'Id': '2', Text:'345', Children: null}",
                    }
                }
            });

            realm.Refresh();
            var parent = realm.All<RealmManyRef>().First();
            parent.Id.Should().Be("2");
            string.Join(", ", parent.Children.Select(x => x.Id)).Should().BeEquivalentTo("");
        }

        [Fact]
        public void ManyRef_DownloadData_Update()
        {
            var realm = GetRealm();

            RealmManyRef parent;

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse
            {
                LastChange = new Dictionary<string, DateTimeOffset> { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmRef),
                        MobilePrimaryKey = "1",
                        SerializedObject = "{ 'Id': '1', Text:'123', Parent: null}",
                    },
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmRef),
                        MobilePrimaryKey = "2",
                        SerializedObject = "{ 'Id': '2', Text:'456', Parent: null}",
                    },
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmManyRef),
                        MobilePrimaryKey = "3",
                        SerializedObject = "{ 'Id': '3', Text:'345', Children: null}",
                    }
                }
            });
            realm.Refresh();

            parent = realm.All<RealmManyRef>().First();
            string.Join(", ", parent.Children.Select(x => x.Id)).Should().BeEquivalentTo("");

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse
            {
                LastChange = new Dictionary<string, DateTimeOffset> { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmManyRef),
                        MobilePrimaryKey = "3",
                        SerializedObject = "{ Children: ['1']}",
                    }
                }
            });
            realm.Refresh();

            parent = realm.All<RealmManyRef>().First();
            string.Join(", ", parent.Children.Select(x => x.Id)).Should().BeEquivalentTo("1");

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmManyRef),
                        MobilePrimaryKey = "3",
                        SerializedObject = "{ Children: ['2']}",
                    }
                }
            });
            realm.Refresh();

            parent = realm.All<RealmManyRef>().First();
            string.Join(", ", parent.Children.Select(x => x.Id)).Should().BeEquivalentTo("2");

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmManyRef),
                        MobilePrimaryKey = "3",
                        SerializedObject = "{ Children: ['1','2']}",
                    }
                }
            });
            realm.Refresh();

            parent = realm.All<RealmManyRef>().First();
            string.Join(", ", parent.Children.Select(x => x.Id)).Should().BeEquivalentTo("1, 2");
        }

        [Fact]
        public void Serialize_Deserialize()
        {
            var realm = GetRealm();

            var ref1 = new RealmRef
            {
                Text = "123",
                Id = "1",
            };

            var ref2 = new RealmRef
            {
                Id = "2",
                Text = "zxc",
                Parent = ref1,
            };

            realm.Write(
                () =>
                {
                    realm.Add(ref1);
                    realm.Add(ref2);
                });

            var s1 = _realmiusSyncService.SerializeObject(ref1);
            s1.Should().BeEquivalentTo($"{{\"Id\":\"1\",\"Text\":\"123\",\"Parent\":null,\"MobilePrimaryKey\":\"1\"}}");

            var s2 = _realmiusSyncService.SerializeObject(ref2);
            s2.Should().BeEquivalentTo($"{{\"Id\":\"2\",\"Text\":\"zxc\",\"Parent\":\"1\",\"MobilePrimaryKey\":\"2\"}}");

            realm.Write(
                () =>
                {
                    realm.Remove(ref1);
                    realm.Remove(ref2);
                });

            var ref11 = new RealmRef();
            realm.Write(
                () =>
                {
                    realm.Add(ref11);
                    _realmiusSyncService.Populate(s1, ref11, realm);
                });

            ref11.Text.Should().Be("123");
            ref11.Id.Should().Be("1");
            ref11.Parent.Should().BeNull();

            var ref21 = new RealmRef();
            realm.Write(
                () =>
                {
                    realm.Add(ref21);
                    _realmiusSyncService.Populate(s2, ref21, realm);
                });

            ref21.Text.Should().Be("zxc");
            ref21.Id.Should().Be("2");
            ref21.Parent.Should().NotBeNull();
            ref21.Parent.Id.Should().Be("1");
        }

        [Fact]
        public void DownloadData_ReferencesCorrectOrder()
        {
            var realm = GetRealm();

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse
            {
                LastChange = new Dictionary<string, DateTimeOffset> { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmRef),
                        MobilePrimaryKey = "1",
                        SerializedObject = "{ 'Id': '1', Text:'123', Parent: null}",
                    },
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmRef),
                        MobilePrimaryKey = "2",
                        SerializedObject = "{ 'Id': '2', Text:'345', Parent: '1'}",
                    }
                }
            });

            realm.Refresh();
            var objects = realm.All<RealmRef>().OrderBy(x => x.Id).ToList();
            string.Join(", ", objects.Select(x => $"{x.Id}: {x.Text} - {x.Parent?.Id}"))
                .Should().BeEquivalentTo("1: 123 - , 2: 345 - 1");
        }

        [Fact]
        public void DownloadData_ReferencesWrongOrder()
        {
            var realm = GetRealm();

            _apiClientMock.Raise(x => x.NewDataDownloaded += null, _apiClientMock.Object, new DownloadDataResponse()
            {
                LastChange = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.Now } },
                ChangedObjects =
                {
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmRef),
                        MobilePrimaryKey = "2",
                        SerializedObject = "{ 'Id': '2', Text:'345', Parent: '1'}",
                    },
                    new DownloadResponseItem
                    {
                        Type = nameof(RealmRef),
                        MobilePrimaryKey = "1",
                        SerializedObject = "{ 'Id': '1', Text:'123', Parent: null}",
                    }
                }
            });

            realm.Refresh();
            var objects = realm.All<RealmRef>().OrderBy(x => x.Id).ToList();
            string.Join(", ", objects.Select(x => $"{x.Id}: {x.Text} - {x.Parent?.Id}"))
                .Should().BeEquivalentTo("1: 123 - , 2: 345 - 1");
        }
    }
}