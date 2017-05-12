using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using RealmSync.Server;
using RealmSync.Server.Models;
using RealmSync.SyncService;
using RealmSync.Tests.Server.Models;
using Z.EntityFramework.Plus;

namespace RealmSync.Tests.Server
{
    [TestFixture]
    public class TagsTests : TestBase
    {
        private Func<LocalDbContext> _contextFunc;
        private RealmSyncServerProcessor _processor;
        private Config _config;

        public class Config : SyncConfigurationBase<ISyncUser>
        {
            public override bool CheckAndProcess(CheckAndProcessArgs<ISyncUser> args)
            {
                return true;
            }

            public override IList<Type> TypesToSync => new[] { typeof(DbSyncObject) };
            public override IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmSyncObjectServer obj)
            {
                var dbObj = obj as DbSyncObject;
                if (dbObj == null)
                    return new[] { "none" };

                return (dbObj.Tags ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public TagsTests()
        {
            _config = new Config();
            _contextFunc = () => new LocalDbContext(_config);
        }

        [SetUp]
        public void Setup()
        {
            base.Setup();
            _processor = new RealmSyncServerProcessor(_contextFunc, _config);
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
            }, new SyncUser());


            var result = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now).AddHours(-1).ToDictionary(),
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUserTagged(new[] { "u2" }));
            result.ChangedObjects.Should().BeEmpty();

            var result2 = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now).AddHours(-1).ToDictionary(),
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUserTagged(new[] { "u1" }));
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
            }, new SyncUser());


            var result = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.MinValue } },
                Types = new[] { nameof(DbSyncObject) },
                OnlyDownloadSpecifiedTags = true,
            }, new SyncUserTagged(new[] { "u1" }));
            result.ChangedObjects.Should().BeEmpty();

            var result2 = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>() { { "u1", DateTimeOffset.MinValue } },
                Types = new[] { nameof(DbSyncObject) },
                OnlyDownloadSpecifiedTags = true
            }, new SyncUserTagged(new[] { "u1" }));
            result2.ChangedObjects.Count.Should().Be(1);

            var result3 = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset>() { { "all", DateTimeOffset.MinValue } },
                Types = new[] { nameof(DbSyncObject) },
                OnlyDownloadSpecifiedTags = false,
            }, new SyncUserTagged(new[] { "u1" }));
            result2.ChangedObjects.Count.Should().Be(1);
        }

        [Test]
        public void Query()
        {
            var context = _contextFunc();
            var syncStatusContext = new SyncStatusDbContext(context.Database.Connection.ConnectionString);
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

            var user = new SyncUserTagged(new[] { "all", "tag0", "tag1" });
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
