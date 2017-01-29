using System;

namespace RealmSync.SyncService
{
    public interface IRealmSyncService : IDisposable
    {
        Uri ServerUri { get; set; }
        SyncState GetSyncState(string mobilePrimaryKey);

        SyncState GetFileSyncState(string mobilePrimaryKey);
        void QueueFileUpload(UploadFileInfo fileInfo);
    }
}