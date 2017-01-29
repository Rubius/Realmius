using System;
using System.Data.Entity.Migrations;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NUnit.Framework;
using RealmSync.Server;
using RealmSync.SyncService;
using RealmTst.Controllers;
using Z.EntityFramework.Plus;

namespace UnitTestProject
{
    [TestFixture]
    public class RealmSyncControllerUploadTests
    {
        private Func<LocalDbContext> _contextFunc;
        private ShareEverythingRealmSyncServerConfiguration _config;

        public RealmSyncControllerUploadTests()
        {
            _config = new ShareEverythingRealmSyncServerConfiguration(typeof(DbSyncObject));
            _contextFunc = () => new LocalDbContext(_config);

        }

        [SetUp]
        public void Setup()
        {
            _contextFunc().DbSyncObjects.Delete();
        }

        [Test]
        public void UnknownType_Exception()
        {
            var controller = new RealmSyncServerProcessor(_contextFunc, new ShareEverythingRealmSyncServerConfiguration(typeof(UnknownSyncObject)));
            var result = controller.Upload(new UploadDataRequest()
            {
                ChangeNotifications =
                {
                     new UploadRequestItem()
                     {
                         Type = "UnknownSyncObject",
                         PrimaryKey = "123",
                         SerializedObject = JsonConvert.SerializeObject(new UnknownSyncObject()),
                     }
                }
            }, null);
            result.Results.Count.Should().Be(1);
            result.Results[0].Error.Should().ContainEquivalentOf("The entity type UnknownSyncObject is not part of the model for the current context");
        }

        [Test]
        public void UnknownType_NotInDb_TypeDoesNotExist()
        {
            var controller = new RealmSyncServerProcessor(_contextFunc, typeof(DbSyncObject));
            var result = controller.Upload(new UploadDataRequest()
            {
                ChangeNotifications =
                {
                     new UploadRequestItem()
                     {
                         Type = "UnknownSyncObject",
                         PrimaryKey = "123",
                         SerializedObject = JsonConvert.SerializeObject(new UnknownSyncObject()),
                     }
                }
            }, null);
            result.Results.Count.Should().Be(0);
        }

        [Test]
        public void KnownType_Saved1()
        {
            _contextFunc().DbSyncObjects.Count().Should().Be(0);

            var controller = new RealmSyncServerProcessor(_contextFunc, typeof(DbSyncObject));
            var objectToSave = new DbSyncObject()
            {
                Text = "123123123",
                Id = Guid.NewGuid().ToString()
            };
            var result = controller.Upload(new UploadDataRequest()
            {
                ChangeNotifications =
                {
                     new UploadRequestItem()
                     {
                         Type = "DbSyncObject",
                         PrimaryKey = objectToSave.MobilePrimaryKey,
                         SerializedObject = JsonConvert.SerializeObject(objectToSave),
                     }
                }
            }, null);
            result.Results.Count.Should().Be(1);
            CheckNoError(result);
            result.Results[0].MobilePrimaryKey.Should().Be(objectToSave.MobilePrimaryKey);

            _contextFunc().DbSyncObjects.Count().Should().Be(1);
        }

        [Test]
        public void KnownType_Updated()
        {
            _contextFunc().DbSyncObjects.Count().Should().Be(0);

            var controller = new RealmSyncServerProcessor(_contextFunc, typeof(DbSyncObject));
            var objectToSave = new DbSyncObject()
            {
                Text = "123123123",
                Id = Guid.NewGuid().ToString()
            };

            var result = controller.Upload(new UploadDataRequest()
            {
                ChangeNotifications =
                {
                     new UploadRequestItem()
                     {
                         Type = "DbSyncObject",
                         PrimaryKey = objectToSave.MobilePrimaryKey,
                         SerializedObject = JsonConvert.SerializeObject(objectToSave),
                     }
                }
            }, null);
            CheckNoError(result);
            _contextFunc().DbSyncObjects.Find(objectToSave.Id).Text.Should().BeEquivalentTo("123123123");

            objectToSave.Text = "zxc";
            result = controller.Upload(new UploadDataRequest()
            {
                ChangeNotifications =
                {
                     new UploadRequestItem()
                     {
                         Type = "DbSyncObject",
                         PrimaryKey = objectToSave.MobilePrimaryKey,
                         SerializedObject = JsonConvert.SerializeObject(objectToSave),
                     }
                }
            }, null);
            CheckNoError(result);
            _contextFunc().DbSyncObjects.Find(objectToSave.Id).Text.Should().BeEquivalentTo("zxc");
            _contextFunc().DbSyncObjects.Count().Should().Be(1);
        }

        [Test]
        public void KnownType_PartialUpdate()
        {
            _contextFunc().DbSyncObjects.Count().Should().Be(0);

            var controller = new RealmSyncServerProcessor(_contextFunc, typeof(DbSyncObject));
            var objectToSave = new DbSyncObject()
            {
                Text = "123123123",
                Id = Guid.NewGuid().ToString()
            };

            var result = controller.Upload(new UploadDataRequest()
            {
                ChangeNotifications =
                {
                     new UploadRequestItem()
                     {
                         Type = "DbSyncObject",
                         PrimaryKey = objectToSave.MobilePrimaryKey,
                         SerializedObject = JsonConvert.SerializeObject(objectToSave),
                     }
                }
            }, null);
            CheckNoError(result);

            result = controller.Upload(new UploadDataRequest()
            {
                ChangeNotifications =
                {
                     new UploadRequestItem()
                     {
                         Type = "DbSyncObject",
                         PrimaryKey = objectToSave.MobilePrimaryKey,
                         SerializedObject = "{Text: 'asd'}",
                     }
                }
            }, null);
            CheckNoError(result);
            _contextFunc().DbSyncObjects.Find(objectToSave.Id).Text.Should().BeEquivalentTo("asd");
            _contextFunc().DbSyncObjects.Count().Should().Be(1);
        }

        private void CheckNoError(UploadDataResponse result)
        {
            result.Results.Count.Should().BeGreaterThan(0);
            string.Join(", ", result.Results.Select(x => x.Error).Where(x => !string.IsNullOrEmpty(x)))
                .ShouldBeEquivalentTo("");
        }
    }

}
