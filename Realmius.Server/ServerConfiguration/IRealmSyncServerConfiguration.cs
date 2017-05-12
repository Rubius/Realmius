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
    /// <summary>
    /// Do not implement this! Implement IRealmiusServerConfiguration<TUser> instead!
    /// </summary>
    public interface IRealmiusServerDbConfiguration
    {
        IList<Type> TypesToSync { get; }
        IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj);
    }

    public interface IRealmiusServerConfiguration<TUser> : IRealmiusServerDbConfiguration
        where TUser : ISyncUser
    {
        bool CheckAndProcess(CheckAndProcessArgs<TUser> args);
        object[] KeyForType(Type type, string itemPrimaryKey);
    }
}