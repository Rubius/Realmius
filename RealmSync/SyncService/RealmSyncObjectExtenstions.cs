namespace RealmSync.SyncService
{
    public static class RealmSyncObjectExtenstions
    {
        public static SyncState GetSyncState(this IRealmSyncObjectClient obj)
        {
            return (SyncState)obj.SyncState;
        }
        public static void SetSyncState(this IRealmSyncObjectClient obj, SyncState syncState)
        {
            obj.SyncState = (int)syncState;
        }
    }
}