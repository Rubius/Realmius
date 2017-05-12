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
            return new Dictionary<string, DateTimeOffset>() { { "all", date } };
        }
    }

    public class TestBase
    {
        [SetUp]
        public virtual void Setup()
        {
            var config = new ShareEverythingRealmSyncServerConfiguration(typeof(DbSyncObject));
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