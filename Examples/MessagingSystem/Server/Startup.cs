using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

namespace Server
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR("/signalr", new HubConfiguration
            {
                EnableDetailedErrors = true,
            });
        }
    }
}