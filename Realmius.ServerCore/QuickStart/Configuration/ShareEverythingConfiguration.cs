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
using Realmius.Server.Configurations;
using Realmius.Server.Infrastructure;
using Realmius.Server.Models;
using Realmius.Contracts.Logger;

namespace Realmius.Server.QuickStart
{
    public class ShareEverythingConfiguration : RealmiusConfigurationBase
    {
        public override IList<Type> TypesToSync { get; }

        public override ILogger Logger { get; set; } = new Logger();

        public override IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj)
        {
            return new[] { "all" };
        }

        public override IList<string> GetTagsForUser(object user, ChangeTrackingDbContext db)
        {
            return new[] { "all" };
        }

        public ShareEverythingConfiguration(Func<ChangeTrackingDbContext> contextFactoryFunc, IList<Type> typesToSync)
            : base(contextFactoryFunc)
        {
            TypesToSync = typesToSync;
        }

        public ShareEverythingConfiguration(Func<ChangeTrackingDbContext> contextFactoryFunc, Type typeToSync, params Type[] typesToSync)
            : this(contextFactoryFunc, typesToSync.Union(new[] { typeToSync }).ToList())
        {
        }

        public override bool CheckAndProcess(CheckAndProcessArgs<object> args)
        {
            return true;
        }

        public override object AuthenticateUser(IRequest request)
        {
            return new { };
        }
    }
}