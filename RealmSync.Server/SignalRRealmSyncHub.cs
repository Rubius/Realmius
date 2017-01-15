using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace RealmSync.Server
{
    [HubName(Constants.SignalRHubName)]
    public class SignalRRealmSyncHub : Hub
    {

    }
}