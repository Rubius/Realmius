using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using Server.Entities;
using Server.Sync;

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
                while (Console.ReadKey().Key != ConsoleKey.Q)
                {
                    Console.WriteLine("Create new random message");
                    using (var db = new MessagingContext(
                        new ShareEverythingRealmSyncServerConfiguration(typeof(User), typeof(Message))))
                    {
                        var user = new User { Nickname = "SystemUser", Id = Guid.NewGuid().ToString() };
                        db.Users.Add(user);
                        db.SaveChanges();

                        var message = new Message
                        {
                            Id = Guid.NewGuid().ToString(),
                            Text = "Random generated " + new Random().Next(1000)
                        };

                        db.Messages.Add(message);
                        db.SaveChanges();
                        Console.WriteLine(
                            $"User '{user.Nickname}' created message '{message.Text}'");
                    }
                }
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
