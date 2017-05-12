using System;

using Realms;
using RealmSync.SyncService;

using Newtonsoft.Json;

namespace RealmSync.Tests.Client
{
    public class RealmRef : RealmObject, IRealmSyncObjectClient
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Text { get; set; }

        [JsonConverter(typeof(RealmReferencesSerializer))]
        public RealmRef Parent { get; set; }

        public string MobilePrimaryKey => Id;
    }
}