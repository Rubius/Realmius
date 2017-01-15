using System;
using Realms;
using RealmSync.SyncService;

namespace UnitTestProject
{
    public class UnknownSyncObject : RealmObject, IRealmSyncObjectClient, IRealmSyncObjectServer
    {
        #region IRealmSyncObject
        [PrimaryKey]
        public string Id { get; set; }

        public int SyncState { get; set; }
        public DateTimeOffset LastChangeClient { get; set; }
        public DateTime LastChangeServer { get; set; }
        public string MobilePrimaryKey { get { return Id; } }
        [Ignored]
        public string LastSynchronizedVersion { get; set; }
        #endregion
    }
}