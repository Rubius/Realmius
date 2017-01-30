using System;
using System.ComponentModel;

namespace RealmSync.SyncService
{
    public interface IRealmSyncService : IDisposable, INotifyPropertyChanged
    {
        bool UploadInProgress { get; }

        Uri ServerUri { get; set; }
        SyncState GetSyncState(string mobilePrimaryKey);

        SyncState GetFileSyncState(string mobilePrimaryKey);
        //void QueueFileUpload(UploadFileInfo fileInfo);
    }
}