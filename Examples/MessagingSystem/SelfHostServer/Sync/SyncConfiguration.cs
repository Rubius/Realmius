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
using Realmius.Server;
using Realmius.Server.Models;
using Realmius.Server.ServerConfiguration;
using Server.Entities;

namespace Server.Sync
{
    public class SyncConfiguration : SyncConfigurationBase<Client>
    {
        private static SyncConfiguration _instance;

        public static SyncConfiguration Instance => _instance ?? (_instance = new SyncConfiguration());

        private SyncConfiguration()
        {
        }

        public override IList<Type> TypesToSync { get; } = new List<Type>
        {
            typeof(Message)
        };

        public override IList<string> GetTagsForObject(ChangeTrackingDbContext changeTrackingContext, IRealmiusObjectServer obj)
        {
            var message = obj as Message;
            
            if (message != null)
            {
                return new[] { "all" };
            }
            
            return new List<string>();
        }

        public override bool CheckAndProcess(CheckAndProcessArgs<Client> args)
        {
            return true;
        }
    }
}