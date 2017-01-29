using System;
using System.Data.Entity.Migrations;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NUnit.Framework;
using RealmSync.Server;
using RealmSync.Server.Models;
using RealmSync.SyncService;
using RealmTst.Controllers;
using Z.EntityFramework.Plus;

namespace UnitTestProject
{
    [TestFixture]
    public class ChangeTrackingTests
    {
        private Func<LocalDbContext> _contextFunc;
        private ShareEverythingRealmSyncServerConfiguration _config;
        private Func<SyncStatusDbContext> _syncContextFunc;

        public ChangeTrackingTests()
        {
            _config = new ShareEverythingRealmSyncServerConfiguration(typeof(DbSyncObject));
            _contextFunc = () => new LocalDbContext(_config);
            _syncContextFunc = () => new SyncStatusDbContext(_contextFunc().Database.Connection.ConnectionString);

        }

        [SetUp]
        public void Setup()
        {
            _contextFunc().DbSyncObjects.Delete();

            _syncContextFunc().SyncStatusServerObjects.Delete();
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

            obj.Text = "qwe";
            db.SaveChanges();

            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(2);
            var syncObject = _syncContextFunc().SyncStatusServerObjects.ToList()[1];
            syncObject.ChangesAsJson.Should().BeEquivalentTo("{\"Text\":\"qwe\"}");
        }
    }

}
