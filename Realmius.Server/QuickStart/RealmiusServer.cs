using System;
using System.Collections.Generic;
using Owin;
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
            IAppBuilder app,
            IRealmiusServerConfiguration<T> configuration,
            ILogger logger = null)
        {
            if (logger != null)
            {
                ChangeTrackingDbContext.Logger = logger;
            }

            app.MapSignalR<RealmiusPersistentConnection<T>>(url);
        }

        public static void SetupShareEverythingSignalRServer(
            string url,
            IAppBuilder app,
            Func<ChangeTrackingDbContext> contextFunc,
            ILogger logger = null,
            params Type[] types)
        {
            var configuration = new ShareEverythingConfiguration(contextFunc, types);
            SetupSignalRServer(url, app, configuration, logger);
        }

        internal static readonly Dictionary<Type, IRealmiusServerDbConfiguration> Configurations = new Dictionary<Type, IRealmiusServerDbConfiguration>();
        internal static IRealmiusServerConfiguration<T> GetConfiguration<T>()
        {
            return (IRealmiusServerConfiguration<T>) Configurations[typeof(T)];
        }
    }
}