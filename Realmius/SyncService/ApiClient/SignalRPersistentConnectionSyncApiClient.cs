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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Newtonsoft.Json;
using Realmius.Contracts;
using Realmius.Contracts.Models;
using Realmius.Contracts.SignalR;
using Realmius.Contracts.Logger;

namespace Realmius.SyncService.ApiClient
{
    public class SignalRPersistentConnectionSyncApiClient : IApiClient, ILoggerAware
    {
        private Connection _connection;
        private ApiClientStartOptions _startOptions;
        private TaskCompletionSource<bool> _downloadingInitialData = new TaskCompletionSource<bool>();
        public Uri Uri { get; set; }
        public Task DownloadingInitialData => _downloadingInitialData.Task;
        private Action _hubUnsubscribe = () => { };
        private static JsonSerializerSettings _serializerSettings;

        internal readonly Dictionary<string, Action<object>> Callbacks = new Dictionary<string, Action<object>>();
        private int _callbackId;
        private string _uploadDataCallbackId;

        public SignalRPersistentConnectionSyncApiClient(Uri uri)
        {
            Uri = uri;
            _serializerSettings = new JsonSerializerSettings()
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
            };
        }

        public bool IsConnected { get; set; }

        public ILogger Logger { get; set; } = new Logger();

        public event EventHandler ConnectedStateChanged;
        protected virtual void OnConnectedStateChanged()
        {
            Logger.Info("OnConnectedStateChanged");
            ConnectedStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<DownloadDataResponse> NewDataDownloaded;
        protected virtual void OnNewDataDownloaded(DownloadDataResponse e)
        {
            Logger.Info("OnNewDataDownloaded");
            NewDataDownloaded?.Invoke(this, e);
        }

        public event EventHandler<UnauthorizedResponse> Unauthorized;
        protected virtual void OnUnauthorized(UnauthorizedResponse e)
        {
            Logger.Info("OnUnauthorized");
            Unauthorized?.Invoke(this, e);
        }

        public async Task Start(ApiClientStartOptions startOptions)
        {
            _startOptions = startOptions;

            await Reconnect();
        }

        private async Task Reconnect()
        {
            try
            {
                if (IsConnected)
                {
                    IsConnected = false;
                    OnConnectedStateChanged();
                }

                Logger.Info("Reconnect started");
                _hubUnsubscribe();
                _hubUnsubscribe = () => { };

                var parameters = GetParameters(Uri);
                parameters[Constants.LastDownloadParameterName] =
                    WebUtility.UrlEncode(
                        JsonConvert.SerializeObject(_startOptions.LastDownloaded ??
                                                    new Dictionary<string, DateTimeOffset>()));
                parameters[Constants.SyncTypesParameterName] = string.Join(",", _startOptions.Types);
                var connectionUri = Uri.ToString();
                if (!string.IsNullOrEmpty(Uri.Query))
                {
                    connectionUri = connectionUri.Replace(Uri.Query, string.Empty);
                }
                var connection = new Connection(connectionUri, parameters);
                _connection = connection;

                _connection.Received += ConnectionOnReceived;

                _hubUnsubscribe += () =>
                {
                    _connection.Received -= ConnectionOnReceived;
                    _connection?.Dispose();
                };
                Logger.Info(
                    $"  --Connections configured, connecting to {connectionUri}?{string.Join("&", parameters.Select(x => x.Key + "=" + x.Value))}");

                await _connection.Start();

                Logger.Info("  --Connected");

                OnConnected();

                Logger.Info("OnConnected finished");

            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException ||
                    ex is WebException ||
                    ex is HttpClientException ||
                    ex is TimeoutException ||
                    ex is StartException ||
                    ex is ObjectDisposedException)
                {
                    LogAndReconnectWithDelay(ex);
                    return;
                }

                Logger.Info($"Unknown error in Reconnect! Stop Reconnections!!! {ex}");
#if DEBUG
                throw;
#endif
            }
        }

        private void ConnectionOnReceived(string data)
        {
            if (data.Length < 4)
                return;

            var command = data.Substring(0, 4);
            var parameter = data.Substring(4);

            switch (command)
            {
                case MethodConstants.ClientDataDownloaded:
                    OnNewDataDownloaded(Deserialize<DownloadDataResponse>(parameter));
                    break;

                case MethodConstants.ClientUnauthorized:
                    OnUnauthorized(Deserialize<UnauthorizedResponse>(parameter));
                    break;

                case MethodConstants.ServerUploadData:
                    var value = Deserialize<UploadDataResponse>(parameter);
                    if (!Callbacks.ContainsKey(_uploadDataCallbackId))
                    {
                        Logger.Exception(new Exception("UploadDataResponse received but there's no callback to call!"));
                        return;
                    }

                    Callbacks[_uploadDataCallbackId](value);
                    break;

                default:
                    Logger.Exception(new InvalidOperationException($"Unknown command {command}"));
                    break;
            }
        }


        protected static T Deserialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data, _serializerSettings);
        }

        internal static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, _serializerSettings);
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
            _connection.Closed += ConnectionClosed;
            var hubConnection = _connection;
            _downloadingInitialData = new TaskCompletionSource<bool>();

            _hubUnsubscribe += () =>
            {
                hubConnection.Closed -= ConnectionClosed;
            };
            IsConnected = true;
            OnConnectedStateChanged();
        }

        private void LogAndReconnectWithDelay(Exception exception)
        {
            const int delay = 1000;
            Logger.Info($"Unable to connect, will attempt to reconnect in {delay / 1000} seconds!!!");
            Task.Delay(delay).ContinueWith(_ => Reconnect());
        }

        private void ConnectionClosed()
        {
            Logger.Info("Connection closed, will start reconnecting...");
            ClearInvocationCallbacks("close");
            Reconnect();
        }

        public void Stop()
        {
            Logger.Info("Connection Stopped.");

            _hubUnsubscribe();
            _hubUnsubscribe = () => { };
        }

        public Task<UploadDataResponse> UploadData(UploadDataRequest request)
        {
            try
            {
                if (_connection?.State != ConnectionState.Connected)
                    return Task.FromResult(new UploadDataResponse());

                var task = SendAndReceive<UploadDataResponse>(MethodConstants.ServerUploadData, request);

                return task;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return Task.FromResult(new UploadDataResponse());
            }
        }

        //private void Send(string command, object data)
        //{
        //    var serializedData = JsonConvert.SerializeObject(data, new JsonSerializerSettings()
        //    {
        //        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        //    });

        //    _connection.Send(command + serializedData);

        //    //var response = await _hubProxy?.Invoke<UploadDataResponse>("UploadData", request);
        //}

        private Task<TResult> SendAndReceive<TResult>(string command, object data)
        {
            var serializedData = JsonConvert.SerializeObject(data, new JsonSerializerSettings()
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
            });

            var tcs = new TaskCompletionSource<TResult>();

            var callbackId = RegisterCallback(result =>
            {
                if (result is Exception exception)
                {
                    tcs.TrySetException(exception);
                }
                else
                {
                    tcs.TrySetResult((TResult) result);
                }
            });
            if (command == MethodConstants.ServerUploadData)
            {
                _uploadDataCallbackId = callbackId;
            }

            _connection.Send(command + serializedData).

            ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    RemoveCallback(callbackId);
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    RemoveCallback(callbackId);
                    tcs.SetException(task.Exception);
                }
            },
            TaskContinuationOptions.NotOnRanToCompletion);

            return tcs.Task;
        }
        public void UpdateOptions(ApiClientStartOptions startOptions)
        {
            _startOptions = startOptions;
        }

        #region copied from SignalR

        string RegisterCallback(Action<object> callback)
        {
            lock (Callbacks)
            {
                string id = _callbackId.ToString();
                Callbacks[id] = callback;
                _callbackId++;
                return id;
            }
        }

        void RemoveCallback(string callbackId)
        {
            lock (Callbacks)
            {
                Callbacks.Remove(callbackId);
            }
        }
        private void ClearInvocationCallbacks(string error)
        {
            // Copy the callbacks then clear the list so if any of them happen to do an Invoke again
            // they can safely register their own callback into the global list again.
            // Once the global list is clear, dispatch the callbacks on their own threads (BUG #3101)

            Action<object>[] callbacks;

            lock (Callbacks)
            {
                callbacks = Callbacks.Values.ToArray();
                Callbacks.Clear();
            }

            foreach (var callback in callbacks)
            {
                // Create a new HubResult each time as it's mutable and we don't want callbacks
                // changing it during their parallel invocation
                Task.Factory.StartNew(() => callback(new Exception(error)));
                //.Catch(connection: this);
            }
        }
        #endregion
    }
}