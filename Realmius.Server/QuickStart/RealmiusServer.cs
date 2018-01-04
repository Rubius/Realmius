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
using Microsoft.AspNetCore.Builder;
using Realmius.Server.Configurations;
using Realmius.Server.Exchange;
using Realmius.Server.Infrastructure;
using Realmius.Server.Models;

namespace Realmius.Server.QuickStart
{
    public class RealmiusServer
    {
        //public static void SetupSignalRServer<T>(
        //    string url,
        //    IApplicationBuilder app,
        //    IRealmiusServerConfiguration<T> configuration)
        //{
        //    app.UseSignalR(builder => builder.MapHub<RealmiusPersistentConnection<T>>(url));
        //    ServiceProvider = app.ApplicationServices;
        //}

        //public static void SetupShareEverythingSignalRServer(
        //    string url,
        //    IApplicationBuilder app,
        //    Func<ChangeTrackingDbContext> contextFunc,
        //    params Type[] types)
        //{
        //    var configuration = new ShareEverythingConfiguration(contextFunc, types);
        //    SetupSignalRServer(url, app, configuration);
        //}

        internal static IServiceProvider ServiceProvider { get; private set; }
        internal static readonly Dictionary<Type, IRealmiusServerDbConfiguration> Configurations = new Dictionary<Type, IRealmiusServerDbConfiguration>();
        internal static IRealmiusServerConfiguration<T> GetConfiguration<T>()
        {
            return (IRealmiusServerConfiguration<T>) Configurations[typeof(T)];
        }
    }
}