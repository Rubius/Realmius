using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using RealmSync.SyncService;

namespace RealmSync.Server
{
    public class SignalRRealmSyncShareEverythingHub : SignalRRealmSyncHub<SyncUser>
    {
        public SignalRRealmSyncShareEverythingHub(Func<DbContext> dbContextFactoryFunc, params Type[] syncedTypes) :
            base(new RealmSyncServerProcessor<SyncUser>(dbContextFactoryFunc, syncedTypes))
        {
        }

        protected override SyncUser CreateUserInfo(HubCallerContext context)
        {
            return new SyncUser();
        }
    }

    public abstract class SignalRRealmSyncHub<TUser> : Hub
        where TUser : ISyncUser
    {
        private readonly RealmSyncServerProcessor<TUser> _processor;

        protected SignalRRealmSyncHub(RealmSyncServerProcessor<TUser> processor)
        {
            _processor = processor;
            _processor.DataUpdated += (sender, request) =>
            {
                foreach (var connectionInfo in _connections)
                {
                    var download = new DownloadDataResponse()
                    {
                    };

                    var userConnection = this.Clients.User(connectionInfo.Key);
                    var userData = connectionInfo.Value;

                    foreach (var item in request.Items)
                    {
                        if (_processor.UserHasAccessToObject(item.DeserializedObject, userData))
                            download.ChangedObjects.Add(item.Change);
                    }
                    download.LastChange = DateTimeOffset.UtcNow;

                    userConnection.DataDownloaded(download);
                }

            };
        }
        public UploadDataResponse UploadData(UploadDataRequest request)
        {
            return _processor.Upload(request, _connections[Context.ConnectionId]);
        }

        public void HandleDataChanges()
        {

        }

        private readonly static Dictionary<string, TUser> _connections = new Dictionary<string, TUser>();
        public override Task OnConnected()
        {
            _connections[Context.ConnectionId] = CreateUserInfo(Context);

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var connectionId = Context.ConnectionId;
            if (_connections.ContainsKey(connectionId))
                _connections.Remove(connectionId);

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            _connections[Context.ConnectionId] = CreateUserInfo(Context);

            return base.OnReconnected();
        }

        protected abstract TUser CreateUserInfo(HubCallerContext context);
    }
}