using System;
using Microsoft.AspNet.SignalR.Hubs;
using Realmius.Server.Models;
using Realmius.Server.ServerConfiguration;

namespace Realmius.Server.QuickStart
{
    public class SignalRRealmSyncShareEverythingHub : SignalRRealmSyncHub<SyncUser>
    {
        public SignalRRealmSyncShareEverythingHub(Func<ChangeTrackingDbContext> dbContextFactoryFunc, params Type[] syncedTypes) :
            base(new RealmSyncServerProcessor<SyncUser>(dbContextFactoryFunc, new ShareEverythingRealmSyncServerConfiguration<SyncUser>(syncedTypes)))
        {
        }

        protected override SyncUser CreateUserInfo(HubCallerContext context)
        {
            return new SyncUser();
        }
    }
}