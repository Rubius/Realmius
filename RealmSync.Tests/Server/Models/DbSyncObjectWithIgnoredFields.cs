using System.ComponentModel.DataAnnotations;

using Realms;
using RealmSync.SyncService;

using Newtonsoft.Json;

namespace RealmSync.Tests.Server.Models
{
    public class DbSyncObjectWithIgnoredFields : IRealmSyncObjectServer
    {
        public string Text { get; set; }
        [JsonIgnore]
        public string Tags { get; set; }

        #region IRealmSyncObject

        [PrimaryKey]
        [Key]
        public string Id { get; set; }

        public string MobilePrimaryKey => Id;

        #endregion
    }
}