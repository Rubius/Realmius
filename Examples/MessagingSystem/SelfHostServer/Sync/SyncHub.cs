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
using Microsoft.AspNet.SignalR.Hubs;
using Realmius.Contracts;
using Realmius.Server;
using Realmius.Server.QuickStart;
using Server.Entities;

namespace Server.Sync
{
    [HubName(Constants.SignalRHubName)]
    public class SyncHub : SignalRRealmiusHub<Client>
    {
        public SyncHub()
            : base(new RealmiusServerProcessor<Client>(() => new MessagingContext(new ShareEverythingRealmiusServerConfiguration(typeof(Client), typeof(Message))), SyncConfiguration.Instance))
        {
            Console.WriteLine("SyncHub created.");
        }

        protected override Client CreateUserInfo(HubCallerContext context)
        {
            try
            {
                var clientId = context.QueryString["clientId"];

                Console.WriteLine($"Connect client with id '{clientId}'");

                return new Client { Id = clientId};
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in connection with client");
                return null;
            }
        }
    }
}