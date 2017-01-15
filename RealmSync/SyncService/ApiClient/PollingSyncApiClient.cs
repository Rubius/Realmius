using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RealmSync.SyncService
{
    public class PollingSyncApiClient : IApiClient
    {
        private Timer _timer;
        private ApiClientStartOptions _startOptions;
        public Uri DownloadServerUri { get; set; }
        public Uri UploadServerUri { get; set; }

        public event EventHandler<DownloadDataResponse> NewDataDownloaded;
        protected virtual void OnNewDataDownloaded(DownloadDataResponse e)
        {
            NewDataDownloaded?.Invoke(this, e);
        }

        public PollingSyncApiClient(Uri uploadServerUri, Uri downloadServerUri)
        {
            DownloadServerUri = downloadServerUri;
            UploadServerUri = uploadServerUri;
        }


        private HttpClient GetHttpClient()
        {
            return new HttpClient();
        }

        public async Task<UploadDataResponse> UploadData(UploadDataRequest request)
        {
            var httpClient = GetHttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json");
            var result = await httpClient.PostAsync(UploadServerUri, content);

            if (!result.IsSuccessStatusCode)
                return new UploadDataResponse();

            var resultString = await result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<UploadDataResponse>(resultString);
        }

        public async Task<DownloadDataResponse> DownloadData(DownloadDataRequest request)
        {
            var httpClient = GetHttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(request));
            var result = await httpClient.PostAsync(DownloadServerUri, content);

            if (!result.IsSuccessStatusCode)
                return new DownloadDataResponse();

            var resultString = await result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<DownloadDataResponse>(resultString);
        }


        public Task Start(ApiClientStartOptions startOptions)
        {
            _startOptions = startOptions;
            _timer = new Timer(StartDownloadRequests, null, 0, 1000, true);

            return Task.FromResult(true);
        }

        private async Task StartDownloadRequests(object state)
        {
            var result = await DownloadData(new DownloadDataRequest()
            {
                LastChangeTime = _startOptions.LastDownloaded,
                Types = _startOptions.Types,
            });
            OnNewDataDownloaded(result);
        }
        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}