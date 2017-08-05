using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using Realmius.Server.Configurations;
using Realmius.Server.QuickStart;
using RealmiusAdvancedExample_Web.DAL;
using RealmiusAdvancedExample_Web.Models;

[assembly: OwinStartup(typeof(RealmiusAdvancedExample_Web.Startup))]
namespace RealmiusAdvancedExample_Web
{
    public class Startup
    {
        private readonly bool needAuthorisation = true;

        public static Type[] TypesForSync => new Type[] {typeof(NoteRealm), typeof(PhotoRealm), typeof(ChatMessageRealm)};

        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);

            app.MapSignalR("/signalr", new HubConfiguration()
            {
                EnableDetailedErrors = true,

            });

            if (needAuthorisation)
            {
                RealmiusServer.SetupSignalRServer(
                    "/Realmius",
                    app,
                    new RealmiusServerAuthConfiguration()
                    {
                        TypesToSyncList = TypesForSync
                    });
            }
            else
            {
                RealmiusServer.SetupShareEverythingSignalRServer(
                "/Realmius",
                app,
                () => new RealmiusServerContext(),
                TypesForSync);
            }
        }
    }
}
