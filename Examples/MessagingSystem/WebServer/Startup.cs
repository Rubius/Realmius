using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Realmius.Server.Exchange;
using Realmius.Server.QuickStart;

namespace WebServer
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<MessagingContext>(options => options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RealmiusMessagingSystem;Trusted_Connection=True;Integrated Security=True"));
            services.AddCors();

            var serviceProvider = services.BuildServiceProvider();
            services.AddRealmiusShareEverything(() => serviceProvider.GetService<MessagingContext>(), typeof(Message));

            var context = serviceProvider.GetService<MessagingContext>();
            context.Database.Migrate();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder.AllowAnyOrigin());
            app.UseRealmius();

            app.Run(async (context) =>
            {
                Console.WriteLine("Create new random message");
                using (var db = context.RequestServices.GetService<MessagingContext>())
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
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
