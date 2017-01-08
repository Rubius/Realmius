namespace RealmSync.SyncService
{
    public static class RealmSyncObjectExtenstions
    {
        public static SyncState GetSyncState(this IRealmSyncObject obj)
        {
            return (SyncState)obj.SyncState;
        }
        public static void SetSyncState(this IRealmSyncObject obj, SyncState syncState)
        {
            obj.SyncState = (int)syncState;
        }
    }
}