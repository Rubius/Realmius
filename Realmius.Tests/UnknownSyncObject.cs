using Realmius.Server;
using Realmius.SyncService;
using Realms;

namespace Realmius.Tests
{
    public class UnknownSyncObjectServer : IRealmiusObjectServer
    {
        #region IRealmiusObject

        public string Id { get; set; }

        public string MobilePrimaryKey { get { return Id; } }

        #endregion
    }
    public class UnknownSyncObject : RealmObject, IRealmiusObjectClient, IRealmiusObjectServer
    {
        #region IRealmiusObject

        [PrimaryKey]
        public string Id { get; set; }

        public string MobilePrimaryKey { get { return Id; } }

        #endregion
    }
}