using System;
using Realmius.SyncService;
using Realms;

namespace Realmius.Tests.Client
{
    public class DbSyncClientObject2 : RealmObject, IRealmiusObjectClient
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Text { get; set; }
        public string Tags { get; set; }

        public string MobilePrimaryKey => Id;
    }
}