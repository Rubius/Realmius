using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RealmSync.SyncService
{
    public class SyncApiClient
    {
        public Uri DownloadServerUri { get; set; }
        public Uri UploadServerUri { get; set; }

        public SyncApiClient(Uri downloadServerUri, Uri uploadServerUri)
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
            var content = new StringContent(JsonConvert.SerializeObject(request));
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
    }
}