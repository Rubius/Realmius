using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Realms;
using RealmSync.SyncService;

namespace RealmSync.Tests.Server.Models
{
    public class DbSyncObjectWithIgnoredFields :
        IRealmSyncObjectServer
    {
        public string Text { get; set; }
        [JsonIgnore]
        public string Tags { get; set; }

        #region IRealmSyncObject
        [PrimaryKey]
        [Key]
        public string Id { get; set; }

        public string MobilePrimaryKey { get { return Id; } }
        #endregion
    }
}