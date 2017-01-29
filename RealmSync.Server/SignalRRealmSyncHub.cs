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

        private static readonly Dictionary<string, TUser> _connections = new Dictionary<string, TUser>();
        public override Task OnConnected()
        {
            UserConnected();

            return base.OnConnected();
        }

        protected virtual void UserConnected()
        {
            var user = CreateUserInfo(Context);
            _connections[Context.ConnectionId] = user;

            var lastDownload = Context.QueryString[Constants.LastDownloadParameterName];
            var types = Context.QueryString[Constants.SyncTypesParameterName];
            var data = _processor.Download(new DownloadDataRequest()
            {
                LastChangeTime = DateTimeOffset.Parse(lastDownload),
                Types = types.Split(','),
            }, user);
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

        protected override void Dispose(bool disposing)
        {
            ChangeTrackingDbContext.DataUpdated -= HandleDataChanges;

            base.Dispose(disposing);
        }

        protected abstract TUser CreateUserInfo(HubCallerContext context);
    }
}