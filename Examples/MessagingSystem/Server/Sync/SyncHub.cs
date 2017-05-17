using Microsoft.AspNet.SignalR.Hubs;
using Realmius.Contracts;
using Realmius.Server;
using Realmius.Server.QuickStart;
using Server.Entities;

namespace Server.Sync
{
    [HubName(Constants.SignalRHubName)]
    public class SyncHub : SignalRRealmiusHub<User>
    {
        public SyncHub()
            : base(new RealmiusServerProcessor<User>(() => new MessagingContext(new ShareEverythingRealmiusServerConfiguration(typeof(Message))), SyncConfiguration.Instance))
        {
        }

        protected override User CreateUserInfo(HubCallerContext context)
        {
            return null;
        }
    }
}