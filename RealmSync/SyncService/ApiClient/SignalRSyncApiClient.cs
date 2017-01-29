using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;

namespace RealmSync.SyncService
{
    public class SignalRSyncApiClient : IApiClient
    {
        private IHubProxy _hubProxy;
        private HubConnection _hubConnection;
        private ApiClientStartOptions _startOptions;
        public string HubName { get; set; }
        public Uri Uri { get; set; }

        public SignalRSyncApiClient(Uri uri, string hubName)
        {
            Uri = uri;
            HubName = hubName;
        }

        public event EventHandler<DownloadDataResponse> NewDataDownloaded;

        protected virtual void OnNewDataDownloaded(DownloadDataResponse e)
        {
            NewDataDownloaded?.Invoke(this, e);
        }



        public async Task Start(ApiClientStartOptions startOptions)
        {
            _startOptions = startOptions;

            await Reconnect();

        }

        private Action _hubUnsubscribe = () => { };
        private async Task Reconnect()
        {
            try
            {
                _hubUnsubscribe();
                _hubUnsubscribe = () => { };
                var uri = AppendParameterToUri(Uri.ToString(), Constants.LastDownloadParameterName, _startOptions.LastDownloaded.ToString());
                uri = AppendParameterToUri(uri, Constants.SyncTypesParameterName, string.Join(",", _startOptions.Types));
                _hubConnection = new HubConnection(uri);

                _hubProxy = _hubConnection.CreateHubProxy(HubName);
                var downloadHandler = _hubProxy.On<DownloadDataResponse>("DataDownloaded", OnNewDataDownloaded);

                _hubUnsubscribe += () =>
                {
                    downloadHandler.Dispose();
                };

                await _hubConnection.Start();

                OnConnected();

            }
            catch (WebException webEx)
            {
                LogAndReconnectWithDelay(webEx);
            }
            catch (HttpClientException webEx)
            {
                LogAndReconnectWithDelay(webEx);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex}");
            }
        }

        private string AppendParameterToUri(string uri, string parameterName, string parameterValue)
        {
            if (uri.Contains("?"))
                return $"{uri.TrimEnd('&')}&{parameterName}={parameterValue}";
            return $"{uri}?{parameterName}={parameterValue}";
        }

        protected virtual void OnConnected()
        {
            _hubConnection.Closed += _hubConnection_Closed;
            _hubUnsubscribe += () =>
            {
                _hubConnection.Closed -= _hubConnection_Closed;
            };
        }

        void LogAndReconnectWithDelay(Exception exception)
        {
            Debug.WriteLine($"{exception}");
            Task.Delay(1000).ContinueWith((x) => Reconnect());
        }

        void _hubConnection_Closed()
        {
            Reconnect();
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
                if (_hubConnection.State != ConnectionState.Connected)
                    return new UploadDataResponse();

                var response = await _hubProxy.Invoke<UploadDataResponse>("UploadData", request);

                return response;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e}");
                return new UploadDataResponse();
            }
        }

    }
}