//////////////////////////////////////////////////////////////////////////////
////
//// Copyright 2017 Rubius
////
//// Licensed under the Apache License, Version 2.0 (the "License");
//// you may not use this file except in compliance with the License.
//// You may obtain a copy of the License at
////
//// http://www.apache.org/licenses/LICENSE-2.0
////
//// Unless required by applicable law or agreed to in writing, software
//// distributed under the License is distributed on an "AS IS" BASIS,
//// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//// See the License for the specific language governing permissions and
//// limitations under the License.
////
//////////////////////////////////////////////////////////////////////////////

//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNet.SignalR;
//using Newtonsoft.Json;
//using Realmius.Contracts;
//using Realmius.Contracts.Models;
//using Realmius.Server.Exchange;
//using Realmius.Server.Infrastructure;
//using Realmius.Server.Models;

//namespace Realmius.Server.QuickStart
//{

//    public abstract class RealmiusHub<TUser> : Hub
//    {
//        protected readonly RealmiusServerProcessor<TUser> _processor;
//        protected static RealmiusServerProcessor<TUser> _processorStatic;

//        static RealmiusHub()
//        {
//            ChangeTrackingDbContext.DataUpdated += (sender, data) =>
//            {
//                Task.Factory.StartNew(async () =>
//                {
//                    await Task.Delay(20);
//                    UpdatedDataHandler.HandleDataChanges(sender, data);
//                });
//            };
//        }

//        protected RealmiusHub(RealmiusServerProcessor<TUser> processor)
//        {
//            _processor = processor;
//            if (_processorStatic == null)
//                _processorStatic = processor;
//        }

//        public UploadDataResponse UploadData(UploadDataRequest request)
//        {
//            var connectionId = Context.ConnectionId;
//            if (!Connections.ContainsKey(connectionId))
//            {
//                Logger.Log.Info($"User with ConnectionId {connectionId} not found in the connections pool (not authorized?)");
//                return new UploadDataResponse();
//            }

//            var result = _processor.Upload(request, Connections[connectionId]);
//            return result;
//        }

//        private static readonly ConcurrentDictionary<string, TUser> Connections = new ConcurrentDictionary<string, TUser>();
//        public override Task OnConnected()
//        {
//            UserConnected(Context.Request, Context.ConnectionId);

//            return base.OnConnected();
//        }

//        public static void AddUserGroup<THub>(Func<TUser, bool> userPredicate, string group)
//            where THub : RealmiusHub<TUser>
//        {
//            var connectionIds = Connections.Where(x => userPredicate(x.Value));
//            var hub = GlobalHost.ConnectionManager.GetHubContext<THub>();

//            foreach (KeyValuePair<string, TUser> connectionId in connectionIds)
//            {
//                hub.Groups.Add(connectionId.Key, group);

//                var tags = _processorStatic.GetTagsForUser(connectionId.Value);
//                if (tags.Contains(group))
//                    continue;

//                tags.Add(group);
//                //include data for the tag
//                var changes = _processorStatic.Download(new DownloadDataRequest()
//                {
//                    LastChangeTime = new Dictionary<string, DateTimeOffset>() { { group, DateTimeOffset.MinValue } },
//                    Types = _processorStatic.Configuration.TypesToSync.Select(x => x.Name),
//                    OnlyDownloadSpecifiedTags = true,
//                }, connectionId.Value);

//                hub.Clients.Client(connectionId.Key).DataDownloaded(new DownloadDataResponse()
//                {
//                    ChangedObjects = changes.ChangedObjects,
//                    LastChange = new Dictionary<string, DateTimeOffset>() { { group, DateTimeOffset.UtcNow } },
//                    LastChangeContainsNewTags = true,
//                });
//            }

//        }

//        protected virtual void UserConnected(IRequest contextRequest, string connectionId)
//        {
//            var user = CreateUserInfo(contextRequest);
//            if (user == null)
//            {
//                CallUnauthorize(new UnauthorizedResponse()
//                {
//                    Error = "User not authorized"
//                });
//                return;
//            }
//            Connections[connectionId] = user;

//            var lastDownloadString = contextRequest.QueryString[Constants.LastDownloadParameterName];
//            Dictionary<string, DateTimeOffset> lastDownload;
//            var userTags = _processor.GetTagsForUser(user);
//            if (string.IsNullOrEmpty(lastDownloadString))
//            {
//                var lastDownloadOld = contextRequest.QueryString[Constants.LastDownloadParameterNameOld];
//                if (string.IsNullOrEmpty(lastDownloadOld))
//                {
//                    lastDownload = new Dictionary<string, DateTimeOffset>();
//                }
//                else
//                {
//                    var date = DateTimeOffset.Parse(lastDownloadOld);
//                    lastDownload = userTags.ToDictionary(x => x, x => date);
//                }
//            }
//            else
//            {
//                lastDownload = JsonConvert.DeserializeObject<Dictionary<string, DateTimeOffset>>(lastDownloadString);
//            }
//            var types = contextRequest.QueryString[Constants.SyncTypesParameterName];
//            var data = _processor.Download(new DownloadDataRequest()
//            {
//                LastChangeTime = lastDownload,
//                Types = types.Split(','),
//            }, user);

//            data.LastChangeContainsNewTags = true;
//            CallDataDownloaded(data);

//            foreach (var userTag in userTags)
//            {
//                this.Groups.Add(connectionId, userTag);
//            }
//        }

//        private void CallUnauthorize(UnauthorizedResponse unauthorizedResponse)
//        {
//            Clients.Caller.Unauthorized(unauthorizedResponse);
//        }

//        private void CallDataDownloaded(DownloadDataResponse data)
//        {
//            Clients.Caller.DataDownloaded(data);
//        }

//        public override Task OnDisconnected(bool stopCalled)
//        {
//            var connectionId = Context.ConnectionId;
//            UserDisconnected(connectionId);

//            return base.OnDisconnected(stopCalled);
//        }

//        private void UserDisconnected(string connectionId)
//        {
//            TUser user;
//            if (Connections.ContainsKey(connectionId))
//                Connections.TryRemove(connectionId, out user);
//        }

//        public override async Task OnReconnected()
//        {
//            UserConnected(Context.Request, Context.ConnectionId);

//            await base.OnReconnected();
//        }

//        protected abstract TUser CreateUserInfo(IRequest context);
//    }
//}