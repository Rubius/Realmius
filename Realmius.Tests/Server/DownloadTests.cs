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
using System.Data.Entity;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Realmius.Contracts.Models;
using Realmius.Server;
using Realmius.Server.QuickStart;
using Realmius.Server.ServerConfiguration;
using Realmius.Tests.Server.Models;

namespace Realmius.Tests.Server
{
    [TestFixture]
    public class DownloadTests : TestBase
    {
        private Func<LocalDbContext> _contextFunc;
        private RealmiusServerProcessor _controller;
        private ShareEverythingRealmiusServerConfiguration _config;

        public DownloadTests()
        {
            _config = new ShareEverythingRealmiusServerConfiguration(typeof(DbSyncObject), typeof(RefSyncObject));
            _contextFunc = () => new LocalDbContext(_config);
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _controller = new RealmiusServerProcessor(_contextFunc, _config);
        }

        [Test]
        public void NoData()
        {
            var result = _controller.Download(new DownloadDataRequest
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset> { { "all", DateTimeOffset.Now } },
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Should().BeEmpty();
        }

        [Test]
        public void ManyToManyRef()
        {
            var ef = _contextFunc();
            var obj1 = new RefSyncObject
            {
                Id = "1",
                Text = "123"
            };
            var obj2 = new RefSyncObject
            {
                Id = "2",
                Text = "zxc"
            };
            var obj3 = new RefSyncObject
            {
                Id = "3",
                Text = "asd",
                References = new List<RefSyncObject> { obj1, obj2 }
            };
            ef.RefSyncObjects.Add(obj1);
            ef.RefSyncObjects.Add(obj2);
            ef.RefSyncObjects.Add(obj3);
            ef.SaveChanges();

            var result = _controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.MinValue } },
                Types = new[] { nameof(RefSyncObject) },
            }, new SyncUser());

            string.Join(", ", result.ChangedObjects)
                .Should().BeEquivalentTo("Type: RefSyncObject, Key: 1, SerializedObject: { \"Text\": \"123\", \"References\": null, \"Id\": \"1\"}, Type: RefSyncObject, Key: 2, SerializedObject: { \"Text\": \"zxc\", \"References\": null, \"Id\": \"2\"}, Type: RefSyncObject, Key: 3, SerializedObject: { \"Text\": \"asd\", \"References\": [  \"1\",  \"2\" ], \"Id\": \"3\"}");
        }

        [Test]
        public void SomeData_WithOldChangeTime_DoNotReturn()
        {
            var ef = _contextFunc();
            ef.DbSyncObjects.Add(new DbSyncObject
            {
                Id = Guid.NewGuid().ToString(),
                Text = "123"
            });
            ef.SaveChanges();

            var result = _controller.Download(new DownloadDataRequest
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset> { { "all", DateTimeOffset.Now } },
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Should().BeEmpty();
        }

        [Test]
        public void Delete()
        {
            var ef = _contextFunc();
            var obj = new DbSyncObject
            {
                Id = "2",
                Text = "123"
            };
            ef.DbSyncObjects.Add(obj);
            ef.SaveChanges();
            Thread.Sleep(2);

            var time = DateTimeOffset.Now;
            Thread.Sleep(2);
            ef.DbSyncObjects.Remove(obj);
            ef.SaveChanges();

            var result = _controller.Download(new DownloadDataRequest
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset> { { "all", time } },
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            string.Join(", ", result.ChangedObjects)
                .Should().BeEquivalentTo("Type: DbSyncObject, Key: 2, Deleted");
        }

        [Test]
        public void SomeData_WithOldNewerTime_DoReturn()
        {
            var ef = _contextFunc();
            var obj = new DbSyncObject
            {
                Id = Guid.NewGuid().ToString(),
                Text = "123"
            };
            ef.DbSyncObjects.Add(obj);
            ef.SaveChanges();

            var result = _controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>() { { "all", new DateTimeOffset(DateTime.Now.AddDays(-2)) } },
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Select(x => x.MobilePrimaryKey).Should().BeEquivalentTo(obj.Id);

            DateTime.UtcNow.Subtract(result.LastChange["all"].DateTime).Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void UpdateModelViaAttachToContext_NotAllFieldsAreUpdated()
        {
            var ef = _contextFunc();
            var obj = new DbSyncObject
            {
                Id = Guid.NewGuid().ToString(),
                Text = "123"
            };
            ef.DbSyncObjects.Add(obj);
            ef.SaveChanges();

            var date = DateTimeOffset.Now.AddMilliseconds(1);
            Thread.Sleep(3);

            var ef2 = _contextFunc();
            var obj2 = new DbSyncObject
            {
                Id = obj.Id,
                Text = "123456"
            };
            ef2.Entry(obj2).State = EntityState.Modified;
            ef2.SaveChanges();

            var result = _controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>() { { "all", date } },
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Select(x => x.MobilePrimaryKey).Should().BeEquivalentTo(new[] { obj.Id });
            //result.ChangedObjects.Select(x => x.SerializedObject).Should().BeEquivalentTo(""{\r\n  \"Text\": \"123456\",\r\n  \"Tags\": null,\r\n  \"Id\": \"b3586bfb-6e34-4daa-95c1-ddbeb02b4006\"\r\n}"");
            result.ChangedObjects[0].SerializedObject.Should().NotContainEquivalentOf($"\"{nameof(DbSyncObject.Id)}\"");
            result.ChangedObjects[0].SerializedObject.Should().NotContainEquivalentOf($"\"{nameof(DbSyncObject.Tags)}\"");
            result.ChangedObjects[0].SerializedObject.Should().ContainEquivalentOf($"\"{nameof(DbSyncObject.Text)}\"");

            DateTime.UtcNow.Subtract(result.LastChange["all"].DateTime).Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void UpdateModelViaUpdate_NotAllFieldsAreUpdated()
        {
            var ef = _contextFunc();
            var obj = new DbSyncObject
            {
                Id = Guid.NewGuid().ToString(),
                Text = "123"
            };
            ef.DbSyncObjects.Add(obj);
            ef.SaveChanges();

            var date = DateTimeOffset.Now.AddMilliseconds(1);
            Thread.Sleep(3);
            obj.Text = "456";
            ef.SaveChanges();

            var result = _controller.Download(new DownloadDataRequest
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset> { { "all", date } },
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Select(x => x.MobilePrimaryKey).Should().BeEquivalentTo(obj.Id);
            result.ChangedObjects[0].SerializedObject.Should().NotContainEquivalentOf($"\"{nameof(DbSyncObject.Id)}\"");
            result.ChangedObjects[0].SerializedObject.Should().NotContainEquivalentOf($"\"{nameof(DbSyncObject.MobilePrimaryKey)}\"");
            result.ChangedObjects[0].SerializedObject.Should().NotContainEquivalentOf($"\"{nameof(DbSyncObject.Tags)}\"");
            result.ChangedObjects[0].SerializedObject.Should().ContainEquivalentOf($"\"{nameof(DbSyncObject.Text)}\"");

            DateTime.UtcNow.Subtract(result.LastChange["all"].DateTime).Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Download_AlreadyDownloadedDataIsNotReturned()
        {
            var ef = _contextFunc();
            var obj = new DbSyncObject
            {
                Id = Guid.NewGuid().ToString(),
                Text = "123"
            };
            ef.DbSyncObjects.Add(obj);
            ef.SaveChanges();

            var result = _controller.Download(new DownloadDataRequest
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset> { { "all", new DateTimeOffset(DateTime.Now.AddDays(-2)) } },

                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Count.Should().Be(1);

            var result2 = _controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = result.LastChange,
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());
            result2.ChangedObjects.Count.Should().Be(0);

            Thread.Sleep(10);
            obj.Text = "rty";
            ef.SaveChanges();

            var result3 = _controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = result2.LastChange,
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());
            result3.ChangedObjects.Count.Should().Be(1);
        }

        [Test]
        public void NotConfiguredType()
        {
            var db = _contextFunc();
            db.DbSyncObjects.Add(
                new DbSyncObject
                {
                    Id = "2",
                    Text = "123"
                });
            db.UnknownSyncObjectServers.Add(
                new UnknownSyncObjectServer
                {
                    Id = "123"
                });
            db.SaveChanges();

            var res = _controller.Download(new DownloadDataRequest
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset> { { "all", DateTimeOffset.MinValue } },

                Types = new[] { nameof(UnknownSyncObjectServer), nameof(DbSyncObject) },
            }, new SyncUser());
            string.Join(", ", res.ChangedObjects.Select(x => x.Type)).Should().BeEquivalentTo("DbSyncObject");
        }
    }
}
