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
using FluentAssertions;
using NUnit.Framework;
using Realmius.Contracts.Models;
using Realmius.Server;
using Realmius.Server.QuickStart;
using Realmius.Server.ServerConfiguration;
using Realmius.Tests.Server.Models;

namespace Realmius.Tests.Integration
{
    [TestFixture]
    public class BaseIntegrationTests : Server.TestBase
    {
        private readonly Func<LocalDbContext> _contextFunc;
        private RealmiusServerProcessor _server;
        private readonly ShareEverythingRealmiusServerConfiguration _config;

        public BaseIntegrationTests()
        {
            _config = new ShareEverythingRealmiusServerConfiguration(typeof(DbSyncObject), typeof(RefSyncObject));
            _contextFunc = () => new LocalDbContext(_config);
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _server = new RealmiusServerProcessor(_contextFunc, _config);
        }

        [Test]
        public void NoData()
        {
            var result = _server.Download(new DownloadDataRequest
            {
                LastChangeTime = new Dictionary<string, DateTimeOffset> { { "all", DateTimeOffset.Now } },
                Types = new[] { nameof(DbSyncObject) },
            }, new SyncUser());

            result.ChangedObjects.Should().BeEmpty();
        }
    }
}