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
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Newtonsoft.Json;
using Realmius.Contracts;
using Realmius.Contracts.Models;
using Realmius.Contracts.SignalR;

namespace Realmius.SyncService.ApiClient
{
    public class SignalRPersistentConnectionSyncApiClient : IApiClient
    {
        private Connection _connection;
        private ApiClientStartOptions _startOptions;
        private TaskCompletionSource<bool> _downloadingInitialData = new TaskCompletionSource<bool>();
        public Uri Uri { get; set; }
        public Task DownloadingInitialData => _downloadingInitialData.Task;
        private Action _hubUnsubscribe = () => { };
        private static JsonSerializerSettings SerializerSettings;

        internal readonly Dictionary<string, Action<object>> _callbacks = new Dictionary<string, Action<object>>();
        private int _callbackId;
        private string _uploadDataCallbackId;

        public SignalRPersistentConnectionSyncApiClient(Uri uri)
        {
            Uri = uri;
            SerializerSettings = new JsonSerializerSettings()
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
            };
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

        private async Task Reconnect()
        {
            try
            {
                Logger.Log.Info("Reconnect started");
                _hubUnsubscribe();
                _hubUnsubscribe = () => { };

                var parameters = GetParameters(Uri);
                parameters[Constants.LastDownloadParameterName] =
                    WebUtility.UrlEncode(JsonConvert.SerializeObject(_startOptions.LastDownloaded ?? new Dictionary<string, DateTimeOffset>()));
                parameters[Constants.SyncTypesParameterName] = string.Join(",", _startOptions.Types);
                var connectionUri = Uri.ToString();
                if (!string.IsNullOrEmpty(Uri.Query))
                {
                    connectionUri = connectionUri.Replace(Uri.Query, "");
                }
                var connection = new Connection(connectionUri, parameters);
                _connection = connection;

                _connection.Received += ConnectionOnReceived;

                _hubUnsubscribe += () =>
                {
                    _connection.Received -= ConnectionOnReceived;
                    _connection?.Dispose();
                };
                Logger.Log.Info(
                    $"  --Connections configured, connecting to {connectionUri}?{string.Join("&", parameters.Select(x => x.Key + "=" + x.Value))}");

                await _connection.Start();

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

        private void ConnectionOnReceived(string data)
        {
            if (data.Length < 4)
                return;

            var command = data.Substring(0, 4);
            var parameter = data.Substring(4);

            switch (command)
            {
                case MethodConstants.Client_DataDownloaded:
                    OnNewDataDownloaded(Deserialize<DownloadDataResponse>(parameter));
                    break;

                case MethodConstants.Client_Unauthorized:
                    OnUnauthorized(Deserialize<UnauthorizedResponse>(parameter));
                    break;

                case MethodConstants.Server_UploadData:
                    var value = Deserialize<UploadDataResponse>(parameter);
                    if (!_callbacks.ContainsKey(_uploadDataCallbackId))
                    {
                        Logger.Log.Exception(new Exception("UploadDataResponse received but there's no callback to call!"));
                        return;
                    }

                    _callbacks[_uploadDataCallbackId](value);
                    break;

                default:
                    Logger.Log.Exception(new InvalidOperationException($"Unknown command {command}"));
                    break;
            }
        }


        protected static T Deserialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data, SerializerSettings);
        }

        internal static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, SerializerSettings);
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
        }

        void LogAndReconnectWithDelay(Exception exception)
        {
            const int delay = 1000;
            Logger.Log.Info($"Unable to connect, will attempt to reconnect in {delay / 1000} seconds!!!");
            Task.Delay(delay).ContinueWith((x) => Reconnect());
        }

        void ConnectionClosed()
        {
            Logger.Log.Info("Connection closed, will start reconnecting...");
            ClearInvocationCallbacks("close");
            Reconnect();
        }

        public void Stop()
        {
            Logger.Log.Info("Connection Stopped.");

            _hubUnsubscribe();
            _hubUnsubscribe = () => { };
        }

        public Task<UploadDataResponse> UploadData(UploadDataRequest request)
        {
            try
            {
                if (_connection?.State != ConnectionState.Connected)
                    return Task.FromResult(new UploadDataResponse());

                var task = SendAndReceive<UploadDataResponse>(MethodConstants.Server_UploadData, request);

                return task;
            }
            catch (Exception e)
            {
                Logger.Log.Exception(e);
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
                var exception = result as Exception;
                if (exception != null)
                {
                    tcs.TrySetException(exception);
                }
                else
                {
                    tcs.TrySetResult((TResult)result);
                }
            });
            if (command == MethodConstants.Server_UploadData)
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

        #region copied from SignalR

        string RegisterCallback(Action<object> callback)
        {
            lock (_callbacks)
            {
                string id = _callbackId.ToString();
                _callbacks[id] = callback;
                _callbackId++;
                return id;
            }
        }

        void RemoveCallback(string callbackId)
        {
            lock (_callbacks)
            {
                _callbacks.Remove(callbackId);
            }
        }
        private void ClearInvocationCallbacks(string error)
        {
            // Copy the callbacks then clear the list so if any of them happen to do an Invoke again
            // they can safely register their own callback into the global list again.
            // Once the global list is clear, dispatch the callbacks on their own threads (BUG #3101)

            Action<object>[] callbacks;

            lock (_callbacks)
            {
                callbacks = _callbacks.Values.ToArray();
                _callbacks.Clear();
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