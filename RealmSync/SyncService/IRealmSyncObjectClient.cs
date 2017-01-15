namespace RealmSync.SyncService
{
    public interface IRealmSyncObjectClient
    {
        /// <summary>
        /// values are from SyncState enum. enums are not supported by Realm yet
        /// </summary>
        int SyncState { get; set; }
        string MobilePrimaryKey { get; }
        string LastSynchronizedVersion { get; set; }
    }
}