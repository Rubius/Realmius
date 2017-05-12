using System;
using System.Linq;
using System.Threading;

using RealmSync.Server;
using RealmSync.Server.Models;
using RealmSync.Tests.Server.Models;

using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace RealmSync.Tests.Server
{
    [TestFixture]
    public class ChangeTrackingTests : TestBase
    {
        private Func<LocalDbContext> _contextFunc;
        private ShareEverythingRealmSyncServerConfiguration _config;
        private Func<SyncStatusDbContext> _syncContextFunc;

        public ChangeTrackingTests()
        {
            _config = new ShareEverythingRealmSyncServerConfiguration(typeof(DbSyncObject), typeof(DbSyncObjectWithIgnoredFields));
            _contextFunc = () => new LocalDbContext(_config);
            var connectionString = _contextFunc().Database.Connection.ConnectionString;
            _syncContextFunc = () => new SyncStatusDbContext(_contextFunc().Database.Connection.ConnectionString);

        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();
        }

        [Test]
        public void AddData()
        {
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(0);

            var db = _contextFunc();
            var obj = new DbSyncObject()
            {
                Id = Guid.NewGuid().ToString(),
                Text = "asd"
            };
            db.DbSyncObjects.Add(obj);
            db.SaveChanges();

            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(1);
            var syncObject = _syncContextFunc().SyncStatusServerObjects.First();
            var res = JsonConvert.DeserializeObject<DbSyncObject>(syncObject.FullObjectAsJson);
            res.MobilePrimaryKey.Should().Be(obj.Id);
            res.Text.Should().Be("asd");
        }

        [Test]
        public void UpdateData()
        {
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(0);

            var db = _contextFunc();
            var obj = new DbSyncObject()
            {
                Id = Guid.NewGuid().ToString(),
                Text = "asd"
            };
            db.DbSyncObjects.Add(obj);
            db.SaveChanges();

            Thread.Sleep(1);
            obj.Text = "qwe";
            db.SaveChanges();

            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(1);
            var syncObject = _syncContextFunc().SyncStatusServerObjects.ToList()[0];
            var textChange = syncObject.ColumnChangeDates["Text"];
            var idChange = syncObject.ColumnChangeDates["Id"];
            var tagsChange = syncObject.ColumnChangeDates["Tags"];
            idChange.Should().Be(tagsChange);
            textChange.Should().BeAfter(idChange);
        }
        
        [Test]
        public void AddData_IgnoredFieldsNotSerialized()
        {
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(0);

            var db = _contextFunc();
            var obj = new DbSyncObjectWithIgnoredFields()
            {
                Id = Guid.NewGuid().ToString(),
                Text = "asd",
                Tags = "zxc",
            };
            db.DbSyncObjectWithIgnoredFields.Add(obj);
            db.SaveChanges();

            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(1);
            var syncObject = _syncContextFunc().SyncStatusServerObjects.First();
            var res = JsonConvert.DeserializeObject<DbSyncObjectWithIgnoredFields>(syncObject.FullObjectAsJson);

            res.MobilePrimaryKey.Should().Be(obj.Id);
            res.Tags.Should().BeNullOrEmpty();

            var jObject = JObject.Parse(syncObject.FullObjectAsJson);
            jObject.Property("Tags").Should().BeNull();
        }

        [Test]
        public void UpdateData_IgnoredFieldsNotSerialized()
        {
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(0);

            var db = _contextFunc();
            var obj = new DbSyncObjectWithIgnoredFields()
            {
                Id = Guid.NewGuid().ToString(),
                Text = "asd",
                Tags = "123",
            };
            db.DbSyncObjectWithIgnoredFields.Add(obj);
            db.SaveChanges();

            Thread.Sleep(1);
            obj.Text = "qwe";
            db.SaveChanges();

            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(1);
            var syncObject = _syncContextFunc().SyncStatusServerObjects.ToList()[0];
            //var textChange = syncObject.ColumnChangeDates["Text"];
            //var idChange = syncObject.ColumnChangeDates["Id"];
            syncObject.ColumnChangeDates.ContainsKey("Tags").Should().BeFalse();
            syncObject.ColumnChangeDates.ContainsKey("Text").Should().BeTrue();
        }
    }

}
