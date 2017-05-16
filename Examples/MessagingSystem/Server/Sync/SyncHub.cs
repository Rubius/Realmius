using System;
using System.Web.ApplicationServices;
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

        }

        protected override User CreateUserInfo(HubCallerContext context)
        {
            return null;
            //try
            //{
            //    var email = context.QueryString["email"];
            //    var key = context.QueryString["authKey"];
            //    var deviceId = context.QueryString["deviceId"];

            //    var db = new HouseManagementDbContext();
            //    var authenticationService = new AuthenticationService(db);
            //    var user = authenticationService.CheckAccess(email, deviceId, key);

            //    if (user == null)
            //        return null;

            //    return user.GetUserInfo(deviceId);
            //}
            //catch (Exception e)
            //{
            //    WebLogger.Log.Error(e, "User not found");
            //    return null;
            //}
        }
    }
}