using System;
using Newtonsoft.Json;
using RealmSync.SyncService;

namespace RealmSync.Model
{

    public partial class ChatMessage :
#if __IOS__  || __MOBILE__
        Realms.RealmObject,
#endif
        IRealmSyncObject
    {
        public string Author { get; set; }
        public string Text { get; set; }
        public string Text2 { get; set; }
        public DateTimeOffset DateTime { get; set; }

        #region IRealmSyncObject
#if __IOS__  || __MOBILE__
        [Realms.PrimaryKey]
#endif
        public string Id { get; set; }

        public int SyncState { get; set; }

        public string MobilePrimaryKey { get { return Id; } }

        [JsonIgnore]
#if SERVER
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public string LastSynchronizedVersion { get; set; }
        #endregion
    }
}