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
using System.Linq;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NUnit.Framework;
using Realmius.Contracts.Models;
using Realmius.Server;
using Realmius.Server.Configurations;
using Realmius.Server.Models;
using Realmius.Server.QuickStart;
using Realmius.Tests.Server.Models;

namespace Realmius.Tests.Server
{
    public class LimitedUser
    {
        public IList<string> Tags { get; set; }

        public LimitedUser(params string[] tags)
        {
            Tags = tags;
        }
    }

    [TestFixture]
    public class TagsTests : TestBase
    {
        private Func<LocalDbContext> _contextFunc;
        private RealmiusServerProcessor<LimitedUser> _processor;
        private Config _config;

        public class Config : RealmiusConfigurationBase<LimitedUser>
        {
            public override IList<string> GetTagsForUser(LimitedUser user, ChangeTrackingDbContext db)
            {
                return user.Tags;
            }

            public override LimitedUser AuthenticateUser(ClaimsPrincipal request)
            {
                return new LimitedUser() { };
            }

            public override bool CheckAndProcess(CheckAndProcessArgs<LimitedUser> args)
            {
                return true;
            }

            public override IList<Type> TypesToSync => new[] { typeof(DbSyncObject) };

            public override IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj)
            {
                var dbObj = obj as DbSyncObject;
                if (dbObj == null)
                    return new[] { "none" };

                return (dbObj.Tags ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }

            public Config(Func<ChangeTrackingDbContext> contextFactoryFunc) : base(contextFactoryFunc)
            {
            }
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _contextFunc = () => new LocalDbContext();
            _config = new Config(_contextFunc);
            _processor = new RealmiusServerProcessor<LimitedUser>(_config);
        }

        [Test]
        public void Authorized()
        {
            var result1 = _processor.Upload(new UploadDataRequest()
            {
                ChangeNotifications = new[]
                {
                    new UploadRequestItem()
                    {
                        PrimaryKey = "123",
                        Type = "DbSyncObject",
                        SerializedObject = JsonConvert.SerializeObject(new DbSyncObject()
                        {
                            Id="123",Text = "zxc",Tags ="u1"
                        }),
                    }
                }
            }, new LimitedUser());


            var result = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now).AddHours(-1).ToDictionary(),
                Types = new[] { nameof(DbSyncObject) },
            }, new LimitedUser(new[] { "u2" }));
            result.ChangedObjects.Should().BeEmpty();

            var result2 = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now).AddHours(-1).ToDictionary(),
                Types = new[] { nameof(DbSyncObject) },
            }, new LimitedUser(new[] { "u1" }));
            result2.ChangedObjects.Count.Should().Be(1);
        }


        [Test]
        public void OnlyDownloadSpecifiedTags_1()
        {
            var result1 = _processor.Upload(new UploadDataRequest()
            {
                ChangeNotifications = new[]
                {
                    new UploadRequestItem()
                    {
                        PrimaryKey = "123",
                        Type = "DbSyncObject",
                        SerializedObject = JsonConvert.SerializeObject(new DbSyncObject()
                        {
                            Id="123",Text = "zxc",Tags ="u1"
                        }),
                    }
                }
            }, new LimitedUser());


            var result = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.MinValue } },
                Types = new[] { nameof(DbSyncObject) },
                OnlyDownloadSpecifiedTags = true,
            }, new LimitedUser(new[] { "u1" }));
            result.ChangedObjects.Should().BeEmpty();

            var result2 = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>() { { "u1", DateTimeOffset.MinValue } },
                Types = new[] { nameof(DbSyncObject) },
                OnlyDownloadSpecifiedTags = true
            }, new LimitedUser(new[] { "u1" }));
            result2.ChangedObjects.Count.Should().Be(1);

            var result3 = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.MinValue } },
                Types = new[] { nameof(DbSyncObject) },
                OnlyDownloadSpecifiedTags = false,
            }, new LimitedUser(new[] { "u1" }));
            result2.ChangedObjects.Count.Should().Be(1);
        }

        [Test]
        public void Query()
        {
            var context = _contextFunc();
            var syncStatusContext = new SyncStatusDbContext(context.Database.GetDbConnection().ConnectionString);
            syncStatusContext.SyncStatusServerObjects.Add(
                new SyncStatusServerObject("a", "1")
                {
                    LastChange = new DateTimeOffset(new DateTime(2017, 2, 1)),
                    Tag0 = "all"
                }
            );
            syncStatusContext.SyncStatusServerObjects.Add(
                new SyncStatusServerObject("a", "2")
                {
                    LastChange = new DateTimeOffset(new DateTime(2017, 2, 2)),
                    Tag0 = "tag1"
                }
            );
            syncStatusContext.SyncStatusServerObjects.Add(
                new SyncStatusServerObject("a", "3")
                {
                    LastChange = new DateTimeOffset(new DateTime(2017, 2, 3)),
                    Tag0 = "tag1",
                    Tag1 = "all"
                }
            );
            syncStatusContext.SyncStatusServerObjects.Add(
                new SyncStatusServerObject("a", "4")
                {
                    LastChange = new DateTimeOffset(new DateTime(2017, 2, 10)),
                    Tag0 = "all"
                }
            );
            syncStatusContext.SaveChanges();

            var user = new LimitedUser(new[] { "all", "tag0", "tag1" });
            var result1 = _processor.CreateQuery(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>()
                {
                },
                Types = new[] { "a" },
            }, user, syncStatusContext).ToList();
            string.Join(", ", result1.Select(x => x.MobilePrimaryKey))
                .Should().BeEquivalentTo("1, 2, 3, 4");


            var result2 = _processor.CreateQuery(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>()
                {
                    {"all",new DateTimeOffset(new DateTime(2017, 2, 3, 1,0,0)) }
                },
                Types = new[] { "a" },
            }, user, syncStatusContext).ToList();
            string.Join(", ", result2.Select(x => x.MobilePrimaryKey))
                .Should().BeEquivalentTo("2, 3, 4");

            var result3 = _processor.CreateQuery(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>()
                {
                    {"all",new DateTimeOffset(new DateTime(2017, 2, 3, 1,0,0)) },
                    {"tag1",new DateTimeOffset(new DateTime(2017, 2, 2, 1,0,0)) },
                },
                Types = new[] { "a" },
            }, user, syncStatusContext).ToList();
            string.Join(", ", result3.Select(x => x.MobilePrimaryKey))
                .Should().BeEquivalentTo("3, 4");
        }
    }

}
