using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Realmius.Server.Configurations;
using Realmius.Server.Exchange;
using Realmius.Server.Infrastructure;
using Realmius.Server.Models;

namespace Realmius.Server.QuickStart
{
    public class RealmiusServer
    {
        public static void SetupSignalRServer<T>(
            string url,
            IApplicationBuilder app,
            IRealmiusServerConfiguration<T> configuration)
        {
            app.UseSignalR(builder => builder.MapHub<RealmiusPersistentConnection<T>>(url));
            //app.MapSignalR<RealmiusPersistentConnection<T>>(url);
        }

        public static void SetupShareEverythingSignalRServer(
            string url,
            IApplicationBuilder app,
            Func<ChangeTrackingDbContext> contextFunc,
            params Type[] types)
        {
            var configuration = new ShareEverythingConfiguration(contextFunc, types);
            SetupSignalRServer(url, app, configuration);
        }

        internal static readonly Dictionary<Type, IRealmiusServerDbConfiguration> Configurations = new Dictionary<Type, IRealmiusServerDbConfiguration>();
        internal static IRealmiusServerConfiguration<T> GetConfiguration<T>()
        {
            return (IRealmiusServerConfiguration<T>) Configurations[typeof(T)];
        }
    }
}