using System;
using Realms;
using RealmSync.SyncService;

namespace UnitTestProject
{
    public class UnknownSyncObject : RealmObject, IRealmSyncObject
    {
        #region IRealmSyncObject
        [PrimaryKey]
        public string Id { get; set; }

        public int SyncState { get; set; }
        public DateTimeOffset LastChangeClient { get; set; }
        public DateTimeOffset LastChangeServer { get; set; }
        public string MobilePrimaryKey { get { return Id; } }
        [Ignored]
        public string LastSynchronizedVersion { get; set; }
        #endregion
    }
}