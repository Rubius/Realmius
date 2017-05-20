////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using Realmius.Server.QuickStart;
using Server.Entities;

namespace Server
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
                    using (var db = new MessagingContext())
                    {
                        var message = new Message
                        {
                            Id = Guid.NewGuid().ToString(),
                            ClientId = "SystemID",
                            DateTime = DateTimeOffset.Now,
                            Text = "Random generated value: " + new Random().Next(1000)
                        };

                        db.Messages.Add(message);
                        db.SaveChanges();
                        Console.WriteLine(
                            $"Client '{message.ClientId}' created message '{message.Text}' at {message.DateTime}");
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
