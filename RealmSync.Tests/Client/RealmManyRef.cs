using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Realmius.Infrastructure;
using Realmius.SyncService;
using Realms;

namespace Realmius.Tests.Client
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