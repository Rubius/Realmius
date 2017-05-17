using System;
using Microsoft.AspNet.SignalR.Hubs;
using Realmius.Contracts;
using Realmius.Server;
using Server.Entities;

namespace Server.Sync
{
    [HubName(Constants.SignalRHubName)]
    public class SyncHub : SignalRRealmiusHub<User>
    {
        public SyncHub()
            : base(new RealmiusServerProcessor<User>(() => new MessagingContext(new ShareEverythingRealmSyncServerConfiguration(typeof(User), typeof(Message))), SyncConfiguration.Instance))
        {
            Console.WriteLine("SyncHub created.");
        }

        protected override User CreateUserInfo(HubCallerContext context)
        {
            try
            {
                var email = context.QueryString["email"];
                var key = context.QueryString["authKey"];
                var deviceId = context.QueryString["deviceId"];

                Console.WriteLine($"Connect user with id '{deviceId}'");

                return new User();
            }
            catch (Exception e)
            {
                Console.WriteLine($"User not found");
                return null;
            }
        }
    }
}