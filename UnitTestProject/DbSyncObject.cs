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
        IRealmSyncObjectClient
    {
        public string Text { get; set; }

        #region IRealmSyncObject
        [PrimaryKey]
        [Key]
        public string Id { get; set; }

        public int SyncState { get; set; }
        public DateTimeOffset LastChangeClient { get; set; } = new DateTimeOffset(new DateTime(1970, 1, 1));
        public DateTimeOffset LastChangeServer { get; set; } = new DateTimeOffset(new DateTime(1970, 1, 1));
        public string MobilePrimaryKey { get { return Id; } }
        [Ignored]
        public string LastSynchronizedVersion { get; set; }
        #endregion
    }
}