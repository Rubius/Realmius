using System;
using System.Data.Entity;
using Microsoft.AspNet.SignalR.Hubs;

namespace RealmSync.Server
{
    public class SignalRRealmSyncShareEverythingHub : SignalRRealmSyncHub<SyncUser>
    {
        public SignalRRealmSyncShareEverythingHub(Func<DbContext> dbContextFactoryFunc, params Type[] syncedTypes) :
            base(new RealmSyncServerProcessor<SyncUser>(dbContextFactoryFunc, new ShareEverythingRealmSyncServerConfiguration(syncedTypes)))
        {
        }

        protected override SyncUser CreateUserInfo(HubCallerContext context)
        {
            return new SyncUser();
        }
    }
}