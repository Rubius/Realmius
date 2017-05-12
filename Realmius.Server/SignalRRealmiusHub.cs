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
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Realmius.Contracts;
using Realmius.Contracts.Models;
using Realmius.Server.Models;
using Realmius.Server.ServerConfiguration;

namespace Realmius.Server
{
    public abstract class SignalRRealmiusHub<TUser> : Hub
        where TUser : ISyncUser
    {
        protected readonly RealmiusServerProcessor<TUser> _processor;
        protected static RealmiusServerProcessor<TUser> _processorStatic;

        static SignalRRealmiusHub()
        {
            ChangeTrackingDbContext.DataUpdated += UpdatedDataHandler.HandleDataChanges;
        }

        protected SignalRRealmiusHub(RealmiusServerProcessor<TUser> processor)
        {
            _processor = processor;
            if (_processorStatic == null)
                _processorStatic = processor;
        }

        public UploadDataResponse UploadData(UploadDataRequest request)
        {
            if (!_connections.ContainsKey(Context.ConnectionId))
            {
                Logger.Log.Info($"User with ConnectionId {Context.ConnectionId} not found in the connections pool (not authorized?)");
                return new UploadDataResponse();
            }

            var result = _processor.Upload(request, _connections[Context.ConnectionId]);
            return result;
        }

        private static readonly Dictionary<string, TUser> _connections = new Dictionary<string, TUser>();
        public override Task OnConnected()
        {
            UserConnected();

            return base.OnConnected();
        }

        public static void AddUserGroup<THub>(Func<TUser, bool> userPredicate, string group)
            where THub : SignalRRealmiusHub<TUser>
        {
            var connectionIds = _connections.Where(x => userPredicate(x.Value));
            var hub = GlobalHost.ConnectionManager.GetHubContext<THub>();

            foreach (KeyValuePair<string, TUser> connectionId in connectionIds)
            {
                hub.Groups.Add(connectionId.Key, group);

                if (connectionId.Value.Tags.Contains(group))
                    continue;

                connectionId.Value.Tags.Add(group);
                //include data for the tag
                var changes = _processorStatic.Download(new DownloadDataRequest()
                {
                    LastChangeTime = new Dictionary<string, DateTimeOffset>() { { group, DateTimeOffset.MinValue } },
                    Types = _processorStatic.Configuration.TypesToSync.Select(x => x.Name),
                    OnlyDownloadSpecifiedTags = true,
                }, connectionId.Value);

                hub.Clients.Client(connectionId.Key).DataDownloaded(new DownloadDataResponse()
                {
                    ChangedObjects = changes.ChangedObjects,
                    LastChange = new Dictionary<string, DateTimeOffset>() { { group, DateTimeOffset.UtcNow } },
                    LastChangeContainsNewTags = true,
                });
            }

        }

        //public static void UpdateUserGroups<THub>(Func<TUser, bool> userPredicate, IList<string> groups)
        //    where THub : SignalRRealmiusHub<TUser>
        //{
        //    var connectionIds = _connections.Where(x => userPredicate(x.Value));
        //    var hub = GlobalHost.ConnectionManager.GetHubContext<THub>();

        //    foreach (KeyValuePair<string, TUser> connectionId in connectionIds)
        //    {
        //        var oldGroups = connectionId.Value.Tags.ToList();

        //        foreach (string oldGroup in oldGroups)
        //        {
        //            hub.Groups.Remove(connectionId.Key, oldGroup);
        //        }

        //        connectionId.Value.Tags.Clear();
        //        foreach (var item in groups)
        //        {
        //            connectionId.Value.Tags.Add(item);
        //            hub.Groups.Add(connectionId.Key, item);
        //        }
        //    }
        //}

        protected virtual void UserConnected()
        {
            var user = CreateUserInfo(Context);
            if (user == null)
            {
                Clients.Caller.Unauthorized(new UnauthorizedResponse()
                {
                    Error = "User not authorized"
                });
                return;
            }
            _connections[Context.ConnectionId] = user;


            var lastDownloadString = Context.QueryString[Constants.LastDownloadParameterName];
            Dictionary<string, DateTimeOffset> lastDownload;
            if (string.IsNullOrEmpty(lastDownloadString))
            {
                var lastDownloadOld = Context.QueryString[Constants.LastDownloadParameterNameOld];
                if (string.IsNullOrEmpty(lastDownloadOld))
                {
                    lastDownload = new Dictionary<string, DateTimeOffset>();
                }
                else
                {
                    var date = DateTimeOffset.Parse(lastDownloadOld);
                    lastDownload = user.Tags.ToDictionary(x => x, x => date);
                }

            }
            else
            {
                lastDownload = JsonConvert.DeserializeObject<Dictionary<string, DateTimeOffset>>(lastDownloadString);
            }
            var types = Context.QueryString[Constants.SyncTypesParameterName];
            var data = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = lastDownload,
                Types = types.Split(','),
            }, user);

            data.LastChangeContainsNewTags = true;
            Clients.Caller.DataDownloaded(data);

            foreach (var userTag in user.Tags)
            {
                this.Groups.Add(Context.ConnectionId, userTag);
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var connectionId = Context.ConnectionId;
            if (_connections.ContainsKey(connectionId))
                _connections.Remove(connectionId);

            return base.OnDisconnected(stopCalled);
        }

        public override async Task OnReconnected()
        {
            UserConnected();

            await base.OnReconnected();
        }

        protected abstract TUser CreateUserInfo(HubCallerContext context);
    }
}