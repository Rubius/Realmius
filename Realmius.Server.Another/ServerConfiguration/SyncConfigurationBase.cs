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

namespace Realmius.Server.ServerConfiguration
{
    public abstract class SyncConfigurationBase<TUser> : IRealmiusServerConfiguration<TUser>
        where TUser : ISyncUser
    {
        public abstract IList<Type> TypesToSync { get; }
        public abstract IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj);
        public abstract bool CheckAndProcess(CheckAndProcessArgs<TUser> args);

        public bool CheckAndProcess(ChangeTrackingDbContext ef, IRealmiusObjectServer deserialized, TUser user)
        {
            return CheckAndProcess(new CheckAndProcessArgs<TUser>()
            {
                Entity = deserialized,
                Database = ef,
                User = user,
                OriginalDbEntity = ef.CloneWithOriginalValues(deserialized),
            });
        }

        public virtual object[] KeyForType(Type type, string itemPrimaryKey)
        {
            return new object[] { itemPrimaryKey };
        }
    }
}