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
using NUnit.Framework;
using Realmius.Server.QuickStart;
using Realmius.Tests.Server.Models;
using Z.EntityFramework.Plus;

namespace Realmius.Tests.Server
{
    public static class TestsExtensions
    {
        public static Dictionary<string, DateTimeOffset> ToDictionary(this DateTimeOffset date)
        {
            return new Dictionary<string, DateTimeOffset> { { "all", date } };
        }
    }

    public class TestBase
    {
        [SetUp]
        public virtual void Setup()
        {
            var config = new ShareEverythingConfiguration(typeof(DbSyncObject));
            var context = new LocalDbContext(config);
            context.DbSyncObjects.Delete();
            context.IdIntObjects.Delete();
            context.IdGuidObjects.Delete();
            context.DbSyncObjectWithIgnoredFields.Delete();
            context.UnknownSyncObjectServers.Delete();
            context.CreateSyncStatusContext().SyncStatusServerObjects.Delete();
        }
    }
}