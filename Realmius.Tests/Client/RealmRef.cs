using System;
using Newtonsoft.Json;
using Realmius.Infrastructure;
using Realmius.SyncService;
using Realms;

namespace Realmius.Tests.Client
{
    public class RealmRef : RealmObject, IRealmiusObjectClient
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Text { get; set; }

        [JsonConverter(typeof(RealmReferencesSerializer))]
        public RealmRef Parent { get; set; }

        public string MobilePrimaryKey => Id;
    }
}