using Microsoft.AspNet.SignalR.Hubs;
using RealmSync;
using RealmSync.Model;
using RealmSync.Server;

namespace RealmTst.Controllers
{
    [HubName(Constants.SignalRHubName)]
    public class SyncHub : SignalRRealmSyncHub
    {
        public SyncHub()
            : base(new RealmSyncServerProcessor(() => new SyncDbContext(), typeof(ChatMessage)))
        {

        }
    }
}