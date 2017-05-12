using Realms;
using RealmSync.SyncService;

namespace RealmSync.Tests
{
    public class UnknownSyncObjectServer : IRealmSyncObjectServer
    {
        #region IRealmSyncObject
        public string Id { get; set; }

        public string MobilePrimaryKey { get { return Id; } }
        #endregion
    }
    public class UnknownSyncObject : RealmObject, IRealmSyncObjectClient, IRealmSyncObjectServer
    {
        #region IRealmSyncObject
        [PrimaryKey]
        public string Id { get; set; }

        public string MobilePrimaryKey { get { return Id; } }
        #endregion
    }
}