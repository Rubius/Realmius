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

        public ChangeTrackingTests()
        {
            _config = new ShareEverythingRealmSyncServerConfiguration(typeof(DbSyncObject));
            _contextFunc = () => new LocalDbContext(_config);

        }

        [SetUp]
        public void Setup()
        {
            _contextFunc().DbSyncObjects.Delete();
            new SyncStatusDbContext().SyncStatusServerObjects.Delete();
        }

        [Test]
        public void AddData()
        {
            new SyncStatusDbContext().SyncStatusServerObjects.Count().Should().Be(0);

            var db = _contextFunc();
            var obj = new DbSyncObject()
            {
                Id = Guid.NewGuid().ToString(),
                Text = "asd"
            };
            db.DbSyncObjects.Add(obj);
            db.SaveChanges();

            new SyncStatusDbContext().SyncStatusServerObjects.Count().Should().Be(1);
            var syncObject = new SyncStatusDbContext().SyncStatusServerObjects.First();
            var res = JsonConvert.DeserializeObject<DbSyncObject>(syncObject.FullObjectAsJson);
            res.MobilePrimaryKey.Should().Be(obj.Id);
            res.Text.Should().Be("asd");
        }

        [Test]
        public void UpdateData()
        {
            new SyncStatusDbContext().SyncStatusServerObjects.Count().Should().Be(0);

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

            new SyncStatusDbContext().SyncStatusServerObjects.Count().Should().Be(2);
            var syncObject = new SyncStatusDbContext().SyncStatusServerObjects.ToList()[1];
            syncObject.ChangesAsJson.Should().BeEquivalentTo("{\"Text\":\"qwe\"}");
        }
    }

}
