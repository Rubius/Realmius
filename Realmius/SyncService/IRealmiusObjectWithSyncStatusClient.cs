namespace Realmius.SyncService
{
    public interface IRealmiusObjectWithSyncStatusClient : IRealmiusObjectClient
    {
        int SyncStatus { get; set; }
    }
}