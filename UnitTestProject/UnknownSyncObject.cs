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

        public string MobilePrimaryKey { get { return Id; } }
        #endregion
    }
}