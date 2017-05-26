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
using System.Linq;
using System.Threading;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Realmius.Server.Models;
using Realmius.Server.QuickStart;
using Realmius.Tests.Server.Models;

namespace Realmius.Tests.Server
{
    [TestFixture]
    public class ChangeTrackingTests : TestBase
    {
        private Func<LocalDbContext> _contextFunc;
        private ShareEverythingConfiguration _config;
        private Func<SyncStatusDbContext> _syncContextFunc;

        public ChangeTrackingTests()
        {
            _config = new ShareEverythingConfiguration(typeof(DbSyncObject), typeof(DbSyncObjectWithIgnoredFields));
            _contextFunc = () => new LocalDbContext(_config);
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
            // Arrange
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(0);

            var db = _contextFunc();
            var objectToAdd = new DbSyncObject
            {
                Id = Guid.NewGuid().ToString(),
                Text = "TestText"
            };

            // Act
            db.DbSyncObjects.Add(objectToAdd);
            db.SaveChanges();

            // Assert
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(1);
            var syncObject = _syncContextFunc().SyncStatusServerObjects.First();
            var res = JsonConvert.DeserializeObject<DbSyncObject>(syncObject.FullObjectAsJson);
            res.MobilePrimaryKey.Should().Be(objectToAdd.Id);
            res.Text.Should().Be("TestText");
        }

        [Test]
        public void UpdateData()
        {
            // Arrange
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(0);

            var db = _contextFunc();
            var objectToUpdate = new DbSyncObject
            {
                Id = Guid.NewGuid().ToString(),
                Text = "asd"
            };
            db.DbSyncObjects.Add(objectToUpdate);
            db.SaveChanges();

            // Act
            Thread.Sleep(1);
            objectToUpdate.Text = "qwe";
            db.SaveChanges();

            // Assert
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
            // Arrange
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(0);

            var db = _contextFunc();
            var objectToAdd = new DbSyncObjectWithIgnoredFields
            {
                Id = Guid.NewGuid().ToString(),
                Text = "asd",
                Tags = "zxc",
            };

            // Act
            db.DbSyncObjectWithIgnoredFields.Add(objectToAdd);
            db.SaveChanges();

            // Assert
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(1);
            var syncObject = _syncContextFunc().SyncStatusServerObjects.First();
            var res = JsonConvert.DeserializeObject<DbSyncObjectWithIgnoredFields>(syncObject.FullObjectAsJson);

            res.MobilePrimaryKey.Should().Be(objectToAdd.Id);
            res.Tags.Should().BeNullOrEmpty();

            var jObject = JObject.Parse(syncObject.FullObjectAsJson);
            jObject.Property("Tags").Should().BeNull();
        }

        [Test]
        public void UpdateData_IgnoredFieldsNotSerialized()
        {
            // Arrange
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(0);

            var db = _contextFunc();
            var objectToUpdate = new DbSyncObjectWithIgnoredFields
            {
                Id = Guid.NewGuid().ToString(),
                Text = "asd",
                Tags = "123",
            };
            db.DbSyncObjectWithIgnoredFields.Add(objectToUpdate);
            db.SaveChanges();

            // Act
            Thread.Sleep(1);
            objectToUpdate.Text = "qwe";
            db.SaveChanges();

            // Assert
            _syncContextFunc().SyncStatusServerObjects.Count().Should().Be(1);
            var syncObject = _syncContextFunc().SyncStatusServerObjects.ToList()[0];
            //var textChange = syncObject.ColumnChangeDates["Text"];
            //var idChange = syncObject.ColumnChangeDates["Id"];
            syncObject.ColumnChangeDates.ContainsKey("Tags").Should().BeFalse();
            syncObject.ColumnChangeDates.ContainsKey("Text").Should().BeTrue();
        }
    }
}
