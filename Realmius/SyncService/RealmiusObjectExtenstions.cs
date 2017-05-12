using Realmius.SyncService.RealmModels;

namespace Realmius.SyncService
{
    public static class RealmiusObjectExtenstions
    {
        public static SyncState GetSyncState(this ObjectSyncStatusRealm obj)
        {
            return (SyncState)obj.SyncState;
        }
        public static void SetSyncState(this ObjectSyncStatusRealm obj, SyncState syncState)
        {
            obj.SyncState = (int)syncState;
        }
    }
}