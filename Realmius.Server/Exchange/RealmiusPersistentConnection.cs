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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Realmius.Contracts;
using Realmius.Contracts.Models;
using Realmius.Contracts.SignalR;
using Realmius.Server.Infrastructure;
using Realmius.Server.Models;
using Realmius.Server.QuickStart;

namespace Realmius.Server.Exchange
{
    public class RealmiusPersistentConnection<TUser> : PersistentConnection
    {
        protected readonly RealmiusServerProcessor<TUser> Processor;
        protected static RealmiusServerProcessor<TUser> ProcessorStatic;
        private static readonly ConcurrentDictionary<string, TUser> Connections = new ConcurrentDictionary<string, TUser>();
        private static JsonSerializerSettings SerializerSettings;
        private static bool _initialized;

        protected static T Deserialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data, SerializerSettings);
        }

        internal static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, SerializerSettings);
        }

        public RealmiusPersistentConnection() :
            this(new RealmiusServerProcessor<TUser>(RealmiusServer.GetConfiguration<TUser>()))
        {
        }
        protected RealmiusPersistentConnection(RealmiusServerProcessor<TUser> processor)
        {
            InitializeIfNeeded();
            Processor = processor;
            if (ProcessorStatic == null)
                ProcessorStatic = processor;
        }

        private void InitializeIfNeeded()
        {
            if (_initialized)
                return;
            _initialized = true;

            SerializerSettings = new JsonSerializerSettings()
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
            };

            ChangeTrackingDbContext.DataUpdated += (sender, data) =>
            {
                Task.Factory.StartNew(() =>
                {
                    //await Task.Delay(20);
                    PersistentConnectionUpdatedDataHandler.HandleDataChanges<TUser>(sender, data);
                });
            };
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            if (data.Length < MethodConstants.CommandNameLength)
                return Task.FromResult(true);

            var command = data.Substring(0, MethodConstants.CommandNameLength);
            var parameter = data.Substring(MethodConstants.CommandNameLength);

            switch (command)
            {
                case MethodConstants.ServerUploadData:
                    var result = UploadData(Deserialize<UploadDataRequest>(parameter), connectionId);
                    Send(connectionId, MethodConstants.ServerUploadData, result);
                    break;

                default:
                    Logger.Log.Exception(new InvalidOperationException($"Unknown command {command}"));
                    break;
            }

            return Task.FromResult(true);
        }

        public UploadDataResponse UploadData(UploadDataRequest request, string connectionId)
        {
            if (!Connections.ContainsKey(connectionId))
            {
                Logger.Log.Info($"User with ConnectionId {connectionId} not found in the connections pool (not authorized?)");
                return new UploadDataResponse();
            }

            var result = Processor.Upload(request, Connections[connectionId]);
            return result;
        }

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            UserConnected(request, connectionId);

            return base.OnConnected(request, connectionId);
        }

        public static void AddUserGroup(Func<TUser, bool> userPredicate, string group)
        {
            var connectionIds = Connections.Where(x => userPredicate(x.Value));
            var connection = GlobalHost.ConnectionManager.GetConnectionContext<RealmiusPersistentConnection<TUser>>();

            foreach (KeyValuePair<string, TUser> connectionId in connectionIds)
            {
                connection.Groups.Add(connectionId.Key, group);

                var tags = ProcessorStatic.GetTagsForUser(connectionId.Value);
                if (tags.Contains(group))
                    continue;

                tags.Add(group);
                //include data for the tag
                var changes = ProcessorStatic.Download(new DownloadDataRequest()
                {
                    LastChangeTime = new Dictionary<string, DateTimeOffset>() { { group, DateTimeOffset.MinValue } },
                    Types = ProcessorStatic.Configuration.TypesToSync.Select(x => x.Name),
                    OnlyDownloadSpecifiedTags = true,
                }, connectionId.Value);

                var downloadData = new DownloadDataResponse()
                {
                    ChangedObjects = changes.ChangedObjects,
                    LastChange = new Dictionary<string, DateTimeOffset>() { { group, DateTimeOffset.UtcNow } },
                    LastChangeContainsNewTags = true,
                };
                connection.Connection.Send(connectionId.Key, MethodConstants.ClientDataDownloaded + Serialize(downloadData));
            }

        }

        protected virtual void UserConnected(IRequest contextRequest, string connectionId)
        {
            var user = Processor.Configuration.AuthenticateUser(contextRequest);
            if (user == null)
            {
                CallUnauthorize(connectionId, new UnauthorizedResponse()
                {
                    Error = "User not authorized"
                });
                return;
            }
            Connections[connectionId] = user;

            var lastDownloadString = contextRequest.QueryString[Constants.LastDownloadParameterName];
            Dictionary<string, DateTimeOffset> lastDownload;
            var userTags = Processor.GetTagsForUser(user);
            if (string.IsNullOrEmpty(lastDownloadString))
            {
                var lastDownloadOld = contextRequest.QueryString[Constants.LastDownloadParameterNameOld];
                if (string.IsNullOrEmpty(lastDownloadOld))
                {
                    lastDownload = new Dictionary<string, DateTimeOffset>();
                }
                else
                {
                    var date = DateTimeOffset.Parse(lastDownloadOld);
                    lastDownload = userTags.ToDictionary(x => x, x => date);
                }
            }
            else
            {
                lastDownload = JsonConvert.DeserializeObject<Dictionary<string, DateTimeOffset>>(lastDownloadString);
            }
            var types = contextRequest.QueryString[Constants.SyncTypesParameterName];
            var data = Processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = lastDownload,
                Types = types.Split(','),
            }, user);

            data.LastChangeContainsNewTags = true;
            CallDataDownloaded(connectionId, data);

            foreach (var userTag in userTags)
            {
                Groups.Add(connectionId, userTag);
            }
        }

        private void CallUnauthorize(string connectionId, UnauthorizedResponse unauthorizedResponse)
        {
            Send(connectionId, MethodConstants.ClientUnauthorized, unauthorizedResponse);
        }
        private void CallDataDownloaded(string connectionId, DownloadDataResponse data)
        {
            Send(connectionId, MethodConstants.ClientDataDownloaded, data);
        }

        private void Send(string connectionId, string command, object data)
        {
            Connection.Send(connectionId, command + Serialize(data));
        }

        protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            UserDisconnected(connectionId);

            return base.OnDisconnected(request, connectionId, stopCalled);
        }

        private void UserDisconnected(string connectionId)
        {
            TUser user;
            if (Connections.ContainsKey(connectionId))
                Connections.TryRemove(connectionId, out user);
        }

        protected override Task OnReconnected(IRequest request, string connectionId)
        {
            UserConnected(request, connectionId);

            return base.OnReconnected(request, connectionId);
        }
    }
}