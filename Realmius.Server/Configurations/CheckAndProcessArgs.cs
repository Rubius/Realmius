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

using Realmius.Server.Models;

namespace Realmius.Server.Configurations
{
    public class CheckAndProcessArgs<TUser>
    {
        /// <summary>
        /// reference to EF to retrieve entities if needed
        /// </summary>
        public ChangeTrackingDbContext Database { get; set; }

        /// <summary>
        /// user that is uploading the changes
        /// </summary>
        public TUser User { get; set; }

        /// <summary>
        /// entity with user's changes applied
        /// </summary>
        public IRealmiusObjectServer Entity { get; set; }

        /// <summary>
        /// entity as it is in database before user's changes are applied
        /// </summary>
        public IRealmiusObjectServer OriginalDbEntity { get; set; }

    }
}