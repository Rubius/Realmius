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
using Newtonsoft.Json;
using Realmius.SyncService;
using Realms;

namespace Client.Models
{
    public class Message : RealmObject, IRealmiusObjectClient
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string MobilePrimaryKey => Id;

        public DateTimeOffset DateTime { get; set; }

        public string ClientId { get; set; }

        [JsonProperty("ReplyId")]
        public Message Reply { get; set; }

        public string Text { get; set; }
    }
}