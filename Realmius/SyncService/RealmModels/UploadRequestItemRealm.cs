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
using Realms;

namespace Realmius.SyncService.RealmModels
{
    public class UploadRequestItemRealm : RealmObject
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; }
        public string PrimaryKey { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public string SerializedObject { get; set; }
        public bool IsDeleted { get; set; }


        /// <summary>
        /// how many attempts there were made to upload this item without success
        /// </summary>
        public int UploadAttempts { get; set; }

        [Indexed]
        public DateTimeOffset NextUploadAttemptDate { get; set; } = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}