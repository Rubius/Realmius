namespace RealmSync.SyncService
{
    public static class RealmSyncObjectExtenstions
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