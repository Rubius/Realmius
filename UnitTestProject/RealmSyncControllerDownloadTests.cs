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
    public class RealmSyncControllerDownloadTests
    {
        private Func<LocalDbContext> _contextFunc;

        public RealmSyncControllerDownloadTests()
        {
            _contextFunc = () => new LocalDbContext();

        }

        [SetUp]
        public void Setup()
        {
            _contextFunc().DbSyncObjects.Delete();
        }

        [Test]
        public void NoData()
        {
            var controller = new RealmSyncServerProcessor(_contextFunc, typeof(DbSyncObject));
            var result = controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now),
                Types = new[] { nameof(DbSyncObject) },
            });

            result.ChangedObjects.Should().BeEmpty();
        }


        [Test]
        public void SomeData_WithOldChangeTime_DoNotReturn()
        {
            var ef = _contextFunc();
            ef.DbSyncObjects.Add(new DbSyncObject()
            {
                LastChangeServer = DateTime.Now.AddDays(-1),
                Id = Guid.NewGuid().ToString(),
                Text = "123"
            });
            ef.SaveChanges();

            var controller = new RealmSyncServerProcessor(_contextFunc, typeof(DbSyncObject));
            var result = controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now),
                Types = new[] { nameof(DbSyncObject) },
            });

            result.ChangedObjects.Should().BeEmpty();
        }


        [Test]
        public void SomeData_WithOldNewerTime_DoReturn()
        {
            var ef = _contextFunc();
            var obj = new DbSyncObject()
            {
                LastChangeServer = DateTime.Now.AddDays(-1),
                Id = Guid.NewGuid().ToString(),
                Text = "123"
            };
            ef.DbSyncObjects.Add(obj);
            ef.SaveChanges();

            var controller = new RealmSyncServerProcessor(_contextFunc, typeof(DbSyncObject));
            var result = controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now.AddDays(-2)),
                Types = new[] { nameof(DbSyncObject) },
            });

            result.ChangedObjects.Select(x => x.MobilePrimaryKey).Should().BeEquivalentTo(new[] { obj.Id });

            DateTime.UtcNow.Subtract(result.LastChange.DateTime).Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void NotConfiguredType()
        {
            var controller = new RealmSyncServerProcessor(_contextFunc, typeof(DbSyncObject));

            controller.Invoking(x => x.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now),
                Types = new[] { nameof(UnknownSyncObject) },
            })).ShouldThrow<Exception>()
                ;
        }
    }

}
