////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Realmius.Contracts.Models;
using Realmius.Infrastructure;

namespace Realmius.SyncService.ApiClient
{
    public class PollingSyncApiClient : IApiClient
    {
        private Timer _timer;
        private ApiClientStartOptions _startOptions;
        public Uri DownloadServerUri { get; set; }
        public Uri UploadServerUri { get; set; }
        public Action<HttpClient> HttpClientConfigurationCallback { get; set; } = x => { };

        public bool IsConnected => true;

        public ILogger Logger { get; set; } = new Logger();

        public event EventHandler ConnectedStateChanged;

        public event EventHandler<DownloadDataResponse> NewDataDownloaded;
        protected virtual void OnNewDataDownloaded(DownloadDataResponse e)
        {
            NewDataDownloaded?.Invoke(this, e);
        }

        public event EventHandler<UnauthorizedResponse> Unauthorized;
        protected virtual void OnUnauthorized(UnauthorizedResponse e)
        {
            Unauthorized?.Invoke(this, e);
        }

        public PollingSyncApiClient(Uri uploadServerUri, Uri downloadServerUri)
        {
            DownloadServerUri = downloadServerUri;
            UploadServerUri = uploadServerUri;
        }

        private HttpClient GetHttpClient()
        {
            var client = new HttpClient();

            HttpClientConfigurationCallback(client);

            return client;
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
            try
            {
                var httpClient = GetHttpClient();
                var content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(DownloadServerUri, content);

                if (!result.IsSuccessStatusCode)
                    return new DownloadDataResponse();

                var resultString = await result.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<DownloadDataResponse>(resultString);
            }
            catch (WebException)
            {
                return new DownloadDataResponse();
            }
            catch (Exception e2)
            {
                Debug.WriteLine($"{e2}");
                return new DownloadDataResponse();
            }
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