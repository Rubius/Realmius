using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Newtonsoft.Json;

namespace RealmSync.SyncService
{
    public class SignalRSyncApiClient : IApiClient
    {
        private IHubProxy _hubProxy;
        private HubConnection _hubConnection;
        private ApiClientStartOptions _startOptions;
        private TaskCompletionSource<bool> _downloadingInitialData = new TaskCompletionSource<bool>();
        public string HubName { get; set; }
        public Uri Uri { get; set; }
        public Task DownloadingInitialData => _downloadingInitialData.Task;


        public SignalRSyncApiClient(Uri uri, string hubName)
        {
            Uri = uri;
            HubName = hubName;
        }

        public event EventHandler<DownloadDataResponse> NewDataDownloaded;
        protected virtual void OnNewDataDownloaded(DownloadDataResponse e)
        {
            Logger.Log.Info("OnNewDataDownloaded");
            NewDataDownloaded?.Invoke(this, e);
        }

        public event EventHandler<UnauthorizedResponse> Unauthorized;
        protected virtual void OnUnauthorized(UnauthorizedResponse e)
        {
            Logger.Log.Info("OnUnauthorized");
            Unauthorized?.Invoke(this, e);
        }

        public async Task Start(ApiClientStartOptions startOptions)
        {
            _startOptions = startOptions;

            await Reconnect();

        }

        private Action _hubUnsubscribe = () => { };
        private IDisposable _downloadHandler;
        private IDisposable _unauthorizedHandler;
        private async Task Reconnect()
        {
            try
            {
                Logger.Log.Info("Reconnect started");
                _hubUnsubscribe();
                _hubUnsubscribe = () => { };

                var parameters = GetParameters(Uri);
                parameters[Constants.LastDownloadParameterName] =
                    WebUtility.UrlEncode(JsonConvert.SerializeObject(_startOptions.LastDownloaded));
                parameters[Constants.SyncTypesParameterName] = string.Join(",", _startOptions.Types);
                var connectionUri = Uri.ToString();
                if (!string.IsNullOrEmpty(Uri.Query))
                {
                    connectionUri = connectionUri.Replace(Uri.Query, "");
                }
                var hubConnection = new HubConnection(connectionUri, parameters);
                 _hubConnection = hubConnection;

                _hubProxy = _hubConnection.CreateHubProxy(HubName);
                _downloadHandler = _hubProxy.On<DownloadDataResponse>("DataDownloaded", OnNewDataDownloaded);
                _unauthorizedHandler = _hubProxy.On<UnauthorizedResponse>("Unauthorized", OnUnauthorized);

                _hubUnsubscribe += () =>
                {
                    _downloadHandler?.Dispose();
                    _unauthorizedHandler?.Dispose();
                    _hubConnection?.Dispose();
                };
                Logger.Log.Info(
                    $"  --Connections configured, connecting to {Uri}?{string.Join("&", parameters.Select(x => x.Key + "=" + x.Value))}");

                await _hubConnection.Start();

                Logger.Log.Info("  --Connected");

                OnConnected();

                Logger.Log.Info("OnConnected finished");

            }
            catch (HttpRequestException webEx)
            {
                LogAndReconnectWithDelay(webEx);
            }
            catch (WebException webEx)
            {
                LogAndReconnectWithDelay(webEx);
            }
            catch (HttpClientException webEx)
            {
                LogAndReconnectWithDelay(webEx);
            }
            catch (TimeoutException webEx)
            {
                LogAndReconnectWithDelay(webEx);
            }
            catch (StartException webEx)
            {
                LogAndReconnectWithDelay(webEx);
            }
            catch (ObjectDisposedException webEx)
            {
				LogAndReconnectWithDelay(webEx);
            }
            catch (Exception ex)
            {
                Logger.Log.Info($"Unknown error in Reconnect! Stop Reconnections!!! {ex}");
#if DEBUG
                throw;
#endif
            }
        }

        private Dictionary<string, string> GetParameters(Uri uri)
        {
            var query = uri?.Query;
            if (string.IsNullOrEmpty(query))
            {
                return new Dictionary<string, string>();
            }

            var arguments = query
                  .Substring(1) // Remove '?'
                  .Split('&')
                  .Select(q => q.Split('='))
                  .ToDictionary(q => q.FirstOrDefault(), q => q.Skip(1).FirstOrDefault());
            return arguments;
        }

        protected virtual void OnConnected()
        {
            _hubConnection.Closed += _hubConnection_Closed;
            var hubConnection = _hubConnection;
            _downloadingInitialData = new TaskCompletionSource<bool>();
            
            _hubUnsubscribe += () =>
            {
                hubConnection.Closed -= _hubConnection_Closed;
            };
        }

        void LogAndReconnectWithDelay(Exception exception)
        {
            const int delay = 1000;
            Logger.Log.Info($"Unable to connect, will attempt to reconnect in {delay / 1000} seconds!!!");
            Task.Delay(delay).ContinueWith((x) => Reconnect());
        }

        void _hubConnection_Closed()
        {
            Logger.Log.Info("Connection closed, will start reconnecting...");
            Reconnect();
        }

        public void Stop()
        {
            Logger.Log.Info("Connection Stopped.");

            _hubUnsubscribe();
            _hubUnsubscribe = () => { };
        }

        public async Task<UploadDataResponse> UploadData(UploadDataRequest request)
        {
            try
            {
                if (_hubConnection?.State != ConnectionState.Connected)
                    return new UploadDataResponse();

                var response = await _hubProxy?.Invoke<UploadDataResponse>("UploadData", request);

                return response;
            }
            catch (Exception e)
            {
                Logger.Log.Exception(e);
                return new UploadDataResponse();
            }
        }

    }
}