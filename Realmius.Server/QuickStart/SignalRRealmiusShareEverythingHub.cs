using System;
using Microsoft.AspNet.SignalR.Hubs;
using Realmius.Server.Models;
using Realmius.Server.ServerConfiguration;

namespace Realmius.Server.QuickStart
{
    public class SignalRRealmiusShareEverythingHub : SignalRRealmiusHub<SyncUser>
    {
        public SignalRRealmiusShareEverythingHub(Func<ChangeTrackingDbContext> dbContextFactoryFunc, params Type[] syncedTypes) :
            base(new RealmiusServerProcessor<SyncUser>(dbContextFactoryFunc, new ShareEverythingRealmiusServerConfiguration<SyncUser>(syncedTypes)))
        {
        }

        protected override SyncUser CreateUserInfo(HubCallerContext context)
        {
            return new SyncUser();
        }
    }
}