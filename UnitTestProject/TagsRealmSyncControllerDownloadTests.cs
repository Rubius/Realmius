using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using RealmSync.Server;
using RealmSync.Server.Models;
using RealmSync.SyncService;
using Z.EntityFramework.Plus;

namespace UnitTestProject
{
    [TestFixture]
    public class TagsRealmSyncControllerDownloadTests
    {
        private Func<LocalDbContext> _contextFunc;
        private RealmSyncServerProcessor _processor;
        private Config _config;

        public class Config : IRealmSyncServerConfiguration<ISyncUser>
        {
            public bool CheckAndProcess(IRealmSyncObjectServer deserialized, ISyncUser user)
            {
                return true;
            }

            public IList<Type> TypesToSync => new[] { typeof(DbSyncObject) };
            public IList<string> GetTagsForObject(IRealmSyncObjectServer obj)
            {
                var dbObj = obj as DbSyncObject;
                if (dbObj == null)
                    return new[] { "none" };

                return (dbObj.Tags ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public TagsRealmSyncControllerDownloadTests()
        {
            _config = new Config();
            _contextFunc = () => new LocalDbContext(_config);
        }

        [SetUp]
        public void Setup()
        {
            _contextFunc().DbSyncObjects.Delete();
            new SyncStatusDbContext().SyncStatusServerObjects.Delete();
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
                LastChangeTime = new DateTimeOffset(DateTime.Now).AddHours(-1),
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUserTagged(new[] { "u2" }));
            result.ChangedObjects.Should().BeEmpty();

            var result2 = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now).AddHours(-1),
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUserTagged(new[] { "u1" }));
            result2.ChangedObjects.Count.Should().Be(1);
        }

    }

}
