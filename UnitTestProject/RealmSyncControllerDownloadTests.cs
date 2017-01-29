using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using RealmSync.Server;
using RealmSync.Server.Models;
using RealmSync.SyncService;
using Z.EntityFramework.Plus;

namespace UnitTestProject
{
    [TestFixture]
    public class RealmSyncControllerDownloadTests
    {
        private Func<LocalDbContext> _contextFunc;
        private RealmSyncServerProcessor _controller;
        private ShareEverythingRealmSyncServerConfiguration _config;

        public RealmSyncControllerDownloadTests()
        {
            _config = new ShareEverythingRealmSyncServerConfiguration(typeof(DbSyncObject));
            _contextFunc = () => new LocalDbContext(_config);

        }

        [SetUp]
        public void Setup()
        {
            _contextFunc().DbSyncObjects.Delete();
            new SyncStatusDbContext().SyncStatusServerObjects.Delete();

            _controller = new RealmSyncServerProcessor(_contextFunc, _config);
        }

        [Test]
        public void NoData()
        {
            var result = _controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now),
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Should().BeEmpty();
        }


        [Test]
        public void SomeData_WithOldChangeTime_DoNotReturn()
        {
            var ef = _contextFunc();
            ef.DbSyncObjects.Add(new DbSyncObject()
            {
                Id = Guid.NewGuid().ToString(),
                Text = "123"
            });
            ef.SaveChanges();

            var result = _controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now),
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Should().BeEmpty();
        }


        [Test]
        public void SomeData_WithOldNewerTime_DoReturn()
        {
            var ef = _contextFunc();
            var obj = new DbSyncObject()
            {
                Id = Guid.NewGuid().ToString(),
                Text = "123"
            };
            ef.DbSyncObjects.Add(obj);
            ef.SaveChanges();

            var result = _controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now.AddDays(-2)),
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Select(x => x.MobilePrimaryKey).Should().BeEquivalentTo(new[] { obj.Id });

            DateTime.UtcNow.Subtract(result.LastChange.DateTime).Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Download_AlreadyDownloadedDataIsNotReturned()
        {
            var ef = _contextFunc();
            var obj = new DbSyncObject()
            {
                Id = Guid.NewGuid().ToString(),
                Text = "123"
            };
            ef.DbSyncObjects.Add(obj);
            ef.SaveChanges();

            var result = _controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now.AddDays(-2)),
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Count.Should().Be(1);


            var result2 = _controller.Download(new DownloadDataRequest()
            {
                LastChangeTime = result.LastChange,
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());
            result2.ChangedObjects.Count.Should().Be(0);

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

            _controller.Invoking(x => x.Download(new DownloadDataRequest()
            {
                LastChangeTime = new DateTimeOffset(DateTime.Now),
                Types = new[] { nameof(UnknownSyncObject) },
            }, new SyncUser())).ShouldThrow<Exception>();
        }
    }

}
