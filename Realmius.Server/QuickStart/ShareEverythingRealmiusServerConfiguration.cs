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
using Realmius.Server.Models;
using Realmius.Server.ServerConfiguration;

namespace Realmius.Server.QuickStart
{
    public class ShareEverythingRealmiusServerConfiguration : ShareEverythingRealmiusServerConfiguration<ISyncUser>
    {
        public ShareEverythingRealmiusServerConfiguration(IList<Type> typesToSync) : base(typesToSync)
        {
        }

        public ShareEverythingRealmiusServerConfiguration(Type typeToSync, params Type[] typesToSync) : base(typeToSync, typesToSync)
        {
        }
    }

    public class ShareEverythingRealmiusServerConfiguration<T> : SyncConfigurationBase<T>
        where T : ISyncUser
    {
        public override bool CheckAndProcess(CheckAndProcessArgs<T> args)
        {
            return true;
        }

        public override IList<Type> TypesToSync { get; }
        public override IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj)
        {
            return new[] { "all" };
        }

        public ShareEverythingRealmiusServerConfiguration(IList<Type> typesToSync)
        {
            TypesToSync = typesToSync;
        }
        public ShareEverythingRealmiusServerConfiguration(Type typeToSync, params Type[] typesToSync)
        {
            var types = new List<Type> { typeToSync };
            types.AddRange(typesToSync);
            TypesToSync = types;
        }
    }
}