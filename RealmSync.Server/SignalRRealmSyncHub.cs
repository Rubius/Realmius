using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using RealmSync.SyncService;

namespace RealmSync.Server
{
    [HubName(Constants.SignalRHubName)]
    public class SignalRRealmSyncHub : Hub
    {
        private readonly RealmSyncServerProcessor _processor;

        public SignalRRealmSyncHub(RealmSyncServerProcessor processor)
        {
            _processor = processor;
        }
        public UploadDataResponse UploadData(UploadDataRequest request)
        {
            return _processor.Upload(request);
        }

        public void HandleDataChanges()
        {
            
        }

        public override Task OnConnected()
        {
            //Clients.
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            return base.OnReconnected();
        }
    }
}