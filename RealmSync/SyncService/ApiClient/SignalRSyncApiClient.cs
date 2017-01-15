using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;

namespace RealmSync.SyncService
{
    public class SignalRSyncApiClient : IApiClient
    {
        private IHubProxy _hubProxy;
        private HubConnection _hubConnection;
        public Uri Uri { get; set; }

        public SignalRSyncApiClient(Uri uri)
        {
            Uri = uri;
        }

        public event EventHandler<DownloadDataResponse> NewDataDownloaded;

        protected virtual void OnNewDataDownloaded(DownloadDataResponse e)
        {
            NewDataDownloaded?.Invoke(this, e);
        }



        public async Task Start(ApiClientStartOptions startOptions)
        {
            _hubConnection = new HubConnection(Uri.ToString());
            _hubProxy = _hubConnection.CreateHubProxy(Constants.SignalRHubName);
            _hubProxy.On<DownloadDataResponse>("DataDownloaded", OnNewDataDownloaded);
            await _hubConnection.Start();
        }

        public void Stop()
        {
            _hubConnection?.Stop();
            _hubConnection?.Dispose();
            _hubConnection = null;
        }

        public async Task<UploadDataResponse> UploadData(UploadDataRequest request)
        {
            try
            {
                var response = await _hubProxy.Invoke<UploadDataResponse>("UploadData", request);

                return response;
            }
            catch (Exception)
            {
                return new UploadDataResponse();
            }
        }

    }
}