using System;
using Newtonsoft.Json;
using RealmSync.SyncService;

namespace RealmSync.Model
{

    public partial class ChatMessage :
        Realms.RealmObject,
        IRealmSyncObjectClient
    {

        [Realms.PrimaryKey]
        public string Id { get; set; }

        [Realms.Ignored]
        public string MobilePrimaryKey { get { return Id; } set { Id = value; } }
    }
}