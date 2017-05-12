using System;
using System.Collections.Generic;

using Realms;
using RealmSync.SyncService;

using Newtonsoft.Json;

namespace RealmSync.Tests.Client
{
    public class RealmManyRef : RealmObject, IRealmSyncObjectClient
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Text { get; set; }

        [JsonConverter(typeof(RealmReferencesSerializer))]
        public IList<RealmRef> Children { get; }

        public string MobilePrimaryKey => Id;
    }
}