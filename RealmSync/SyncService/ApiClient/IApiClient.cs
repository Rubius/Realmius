using System;
using System.Threading.Tasks;

namespace RealmSync.SyncService
{
    public interface IApiClient
    {
        event EventHandler<DownloadDataResponse> NewDataDownloaded;

        Task Start(ApiClientStartOptions startOptions);
        void Stop();
        Task<UploadDataResponse> UploadData(UploadDataRequest request);


    }
}