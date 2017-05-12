using System;
using System.ComponentModel.DataAnnotations;
using RealmSync.SyncService;

namespace RealmSync.Tests
{
    public class IdGuidObject :
        IRealmSyncObjectServer
    {
        public string Text { get; set; }
        public string Tags { get; set; }

        #region IRealmSyncObject
        [Key]
        public Guid Id { get; set; }

        public string MobilePrimaryKey => Id.ToString();

        #endregion
    }
}