using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using RealmSync.Server.Models;
using RealmSync.SyncService;

namespace RealmSync.Server
{
    public abstract class SignalRRealmSyncHub<TUser> : Hub
        where TUser : ISyncUser
    {
        private readonly RealmSyncServerProcessor<TUser> _processor;

        protected SignalRRealmSyncHub(RealmSyncServerProcessor<TUser> processor)
        {
            _processor = processor;
            ChangeTrackingDbContext.DataUpdated += HandleDataChanges;
        }
        public UploadDataResponse UploadData(UploadDataRequest request)
        {
            return _processor.Upload(request, _connections[Context.ConnectionId]);
        }

        private void HandleDataChanges(object sender, UpdatedDataBatch updatedDataBatch)
        {
            foreach (var item in updatedDataBatch.Items)
            {
                var download = new DownloadDataResponse()
                {
                };
                download.ChangedObjects.Add(new DownloadResponseItem()
                {
                    MobilePrimaryKey = item.MobilePrimaryKey,
                    Type = item.Type,
                    SerializedObject = item.ChangesAsJson,
                });
                download.LastChange = DateTimeOffset.UtcNow;

                var tags = new List<string>()
                {
                    item.Tag0,item.Tag1,item.Tag2,item.Tag3
                };
                this.Clients.Groups(tags).DataDownloaded(download);
            }
        }

        private readonly static Dictionary<string, TUser> _connections = new Dictionary<string, TUser>();
        public override Task OnConnected()
        {
            var user = CreateUserInfo(Context);
            _connections[Context.ConnectionId] = user;
            foreach (var userTag in user.Tags)
            {
                this.Groups.Add(Context.ConnectionId, userTag);
            }

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

        protected override void Dispose(bool disposing)
        {
            ChangeTrackingDbContext.DataUpdated -= HandleDataChanges;

            base.Dispose(disposing);
        }

        protected abstract TUser CreateUserInfo(HubCallerContext context);
    }
}