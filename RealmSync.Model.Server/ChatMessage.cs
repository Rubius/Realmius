using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using RealmSync.SyncService;

namespace RealmSync.Model
{

    public partial class ChatMessage : IRealmSyncObjectServer
    {
        [Key]
        public string MobilePrimaryKey { get; set; }
    }
}