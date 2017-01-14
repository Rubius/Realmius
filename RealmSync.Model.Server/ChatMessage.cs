using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using RealmSync.SyncService;

namespace RealmSync.Model
{

    public partial class ChatMessage : IRealmSyncObjectServer
    {
        [Index("Sync", Order = 2)]
        public DateTime LastChangeServer { get; set; }
        [Index("Sync", Order = 1)]
        [Key]
        public string MobilePrimaryKey { get; set; }
    }
}