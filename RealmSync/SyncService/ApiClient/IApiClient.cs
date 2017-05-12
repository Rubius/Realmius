using System;
using System.Threading.Tasks;
using Realmius.Contracts.Models;

namespace Realmius.SyncService.ApiClient
{
    public interface IApiClient
    {
        event EventHandler<DownloadDataResponse> NewDataDownloaded;
        event EventHandler<UnauthorizedResponse> Unauthorized;

        Task Start(ApiClientStartOptions startOptions);
        void Stop();
        Task<UploadDataResponse> UploadData(UploadDataRequest request);


    }
}