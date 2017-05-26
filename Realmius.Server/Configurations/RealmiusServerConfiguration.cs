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
using Realmius.Server.QuickStart;

namespace Realmius.Server.Configurations
{
    public class RealmiusServerConfiguration : RealmiusConfigurationBase<RootUser>
    {
        private readonly Func<IRealmiusObjectServer, IList<string>> _getTagsFunc;
        public override IList<Type> TypesToSync { get; }

        public RealmiusServerConfiguration(IList<Type> typesToSync, Func<IRealmiusObjectServer, IList<string>> getTagsFunc)
        {
            _getTagsFunc = getTagsFunc;
            TypesToSync = typesToSync;
        }

        public override bool CheckAndProcess(CheckAndProcessArgs<RootUser> args)
        {
            return true;
        }

        public override IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj)
        {
            return _getTagsFunc(obj);
        }
    }
}