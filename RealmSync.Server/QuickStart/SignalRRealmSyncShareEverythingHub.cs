using System;
using System.Data.Entity;
using Microsoft.AspNet.SignalR.Hubs;
using RealmSync.Server.Models;

namespace RealmSync.Server
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