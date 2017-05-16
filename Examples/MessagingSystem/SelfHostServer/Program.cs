using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;

namespace SelfHostServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "http://localhost:45000";
            using (WebApp.Start(url))
            {
                Console.WriteLine("Server running on {0}", url);
                Console.ReadLine();
            }
        }
    }

    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR("/signalr", new HubConfiguration
            {
                EnableDetailedErrors = true,
            });
        }
    }
}
