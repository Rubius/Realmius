using Microsoft.AspNet.SignalR.Hubs;
using RealmSync;
using RealmSync.Model;
using RealmSync.Server;

namespace RealmTst.Controllers
{
    [HubName(Constants.SignalRHubName)]
    public class SyncHub : SignalRRealmSyncShareEverythingHub
    {
        public SyncHub()
            : base(() => new SyncDbContext(), typeof(ChatMessage))
        {

        }
    }
}