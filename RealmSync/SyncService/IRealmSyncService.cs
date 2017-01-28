using System;

namespace RealmSync.SyncService
{
    public interface IRealmSyncService : IDisposable
    {
        Uri ServerUri { get; set; }
        SyncState GetSyncState(string mobilePrimaryKey);
    }
}