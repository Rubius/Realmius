using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Realmius.Server.QuickStart
{

    public class RealmiusOptions
    {
    }
    public static class RealmiusExtensions
    {
        public static IServiceCollection AddRealmius(this IServiceCollection serviceCollection, Action<RealmiusOptions> handleOptions)
        {
            var options = new RealmiusOptions();
            handleOptions(options);


            return serviceCollection;
        }

        public static IApplicationBuilder UseRealmius(this IApplicationBuilder applicationBuilder)
        {
            var serviceProvider = applicationBuilder.ApplicationServices;


            return applicationBuilder;
        }
    }
}