using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Owin;
using Realmius.Server.Configurations;
using Realmius.Server.Models;

namespace Realmius.Server.QuickStart
{
    public class Tst : PersistentConnection
    {
        protected override Task OnConnected(IRequest request, string connectionId)
        {
            return base.OnConnected(request, connectionId);
        }
    }

    public class RealmiusServer
    {
        public static void SetupSignalRServer<T>(
            string url,
            IAppBuilder app,
            IRealmiusServerConfiguration<T> configuration)
        {
            Configurations[typeof(T)] = configuration;
            ChangeTrackingDbContext.Configurations[configuration.ContextFactoryFunction().GetType()] = configuration;
            app.MapSignalR<RealmiusPersistentConnection<T>>(url);
        }

        public static void SetupShareEverythingSignalRServer(
            string url,
            IAppBuilder app,
            Func<ChangeTrackingDbContext> contextFunc,
            params Type[] types)
        {
            var configuration = new ShareEverythingConfiguration(contextFunc, types);
            SetupSignalRServer(url, app, configuration);
        }

        private static readonly Dictionary<Type, object> Configurations = new Dictionary<Type, object>();
        internal static IRealmiusServerConfiguration<T> GetConfiguration<T>()
        {
            return (IRealmiusServerConfiguration<T>)Configurations[typeof(T)];
        }
    }
}