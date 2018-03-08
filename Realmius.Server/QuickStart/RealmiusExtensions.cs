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
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Realmius.Server.Configurations;
using Realmius.Server.Exchange;
using Realmius.Server.Models;

namespace Realmius.Server.QuickStart
{

    public class RealmiusOptions<T> : RealmiusOptions
    {
        public IRealmiusServerConfiguration<T> Configuration { get; set; }
    }

    public class RealmiusOptions
    {
        public string Url { get; set; }
    }
    public static class RealmiusExtensions
    {
        private static Dictionary<Type, RealmiusOptions> _realmiusOptions = new Dictionary<Type, RealmiusOptions>();
        public static IServiceCollection AddRealmius<T>(this IServiceCollection serviceCollection, Action<RealmiusOptions<T>> handleOptions)
        {
            var options = new RealmiusOptions<T>();
            handleOptions?.Invoke(options);

            AddRealmius(serviceCollection, options);

            return serviceCollection;
        }

        private static void AddRealmius<T>(IServiceCollection serviceCollection, RealmiusOptions<T> options)
        {
            if (string.IsNullOrEmpty(options.Url))
                throw new ArgumentNullException(nameof(options.Url), "Url option must be set");

            serviceCollection.AddSingleton(options.Configuration);
            serviceCollection.AddSignalR();

            _realmiusOptions[typeof(T)] = options;
        }

        public static IServiceCollection AddRealmiusShareEverything(this IServiceCollection serviceCollection, Func<ChangeTrackingDbContext> contextFunc,
            params Type[] types)
        {
            var options = new RealmiusOptions<object>
            {
                Url = "/Realmius/ShareEverything",
                Configuration = new ShareEverythingConfiguration(contextFunc, types)
            };
            AddRealmius(serviceCollection, options);

            return serviceCollection;
        }

        public static IApplicationBuilder UseRealmius(this IApplicationBuilder applicationBuilder)
        {
            RealmiusServer.ServiceProvider = applicationBuilder.ApplicationServices;

            applicationBuilder.UseSignalR(builder =>
            {
                //builder.GetType().GetMethod("MapHub").MakeGenericMethod()
                foreach (var realmiusOption in _realmiusOptions)
                {
                    builder.MapHub<RealmiusPersistentConnection<object>>(realmiusOption.Value.Url);
                }
            });

            return applicationBuilder;
        }
    }
}