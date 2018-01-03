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

using Realmius.Server;
using Realms;

namespace Realmius.Tests
{
    public class UnknownSyncObjectServer : IRealmiusObjectServer
    {
        #region IRealmiusObject

        public string Id { get; set; }

        public string MobilePrimaryKey => Id;

        #endregion //IRealmiusObject
    }
    public class UnknownSyncObject : RealmObject, IRealmiusObjectServer//, Realmius.SyncService.IRealmiusObjectClient
    {
        #region IRealmiusObject

        [PrimaryKey]
        public string Id { get; set; }

        public string MobilePrimaryKey => Id;

        #endregion //IRealmiusObject
    }
}