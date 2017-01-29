using System;
using System.ComponentModel.DataAnnotations;
using Realms;
using RealmSync.SyncService;

namespace UnitTestProject
{
    public class DbSyncObject :
#if __IOS__
        RealmObject, 
#endif
        IRealmSyncObjectClient, IRealmSyncObjectServer
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