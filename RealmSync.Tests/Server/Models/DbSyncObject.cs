using System.ComponentModel.DataAnnotations;
using Realms;
using RealmSync.SyncService;

namespace RealmSync.Tests.Server.Models
{
    public class DbSyncObject :
        IRealmSyncObjectServer
    {
        public string Text { get; set; }
        public string Tags { get; set; }

        #region IRealmSyncObject
        [PrimaryKey]
        [Key]
        public string Id { get; set; }

        public string MobilePrimaryKey { get { return Id; } }
        #endregion
    }
}