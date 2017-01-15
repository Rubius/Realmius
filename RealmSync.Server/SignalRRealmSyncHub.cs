using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using RealmSync.SyncService;

namespace RealmSync.Server
{
    public class UserInfo
    {

    }

    public abstract class SignalRRealmSyncHub : Hub
    {
        private readonly RealmSyncServerProcessor _processor;

        protected SignalRRealmSyncHub(RealmSyncServerProcessor processor)
        {
            _processor = processor;
            _processor.DataUpdated += (sender, request) =>
            {
                foreach (var connectionId in _connections.Keys)
                {
                    var download = new DownloadDataResponse()
                    {
                    };

                    var userConnection = this.Clients.User(connectionId);

                    foreach (var item in request.Items)
                    {
                        if (_processor.UserHasAccessToObject(item.DeserializedObject))
                            download.ChangedObjects.Add(item.Change);
                    }
                    download.LastChange = DateTimeOffset.UtcNow;

                    userConnection.DataDownloaded(download);
                }

            };
        }
        public UploadDataResponse UploadData(UploadDataRequest request)
        {
            return _processor.Upload(request);
        }

        public void HandleDataChanges()
        {

        }

        private readonly static Dictionary<string, UserInfo> _connections = new Dictionary<string, UserInfo>();
        public override Task OnConnected()
        {
            string name = Context.User.Identity.Name;

            _connections[Context.ConnectionId] = new UserInfo();

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string name = Context.User.Identity.Name;

            var connectionId = Context.ConnectionId;
            if (_connections.ContainsKey(connectionId))
                _connections.Remove(connectionId);

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            _connections[Context.ConnectionId] = new UserInfo();

            return base.OnReconnected();
        }
    }
}