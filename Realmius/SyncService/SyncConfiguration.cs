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
using Newtonsoft.Json;
using Realms;

namespace Realmius.SyncService
{
    internal class SyncConfiguration : RealmObject
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string LastDownloadedTagsSerialized { get; set; }

        private Dictionary<string, DateTimeOffset> _lastDownloadedTags;
        public Dictionary<string, DateTimeOffset> LastDownloadedTags
        {
            get
            {
                if (string.IsNullOrEmpty(LastDownloadedTagsSerialized))
                    _lastDownloadedTags = new Dictionary<string, DateTimeOffset>();

                return _lastDownloadedTags ?? (_lastDownloadedTags = JsonConvert.DeserializeObject<Dictionary<string, DateTimeOffset>>(LastDownloadedTagsSerialized));
            }
            set
            {
                _lastDownloadedTags = value;
                SaveLastDownloadedTags();
            }
        }

        public void SaveLastDownloadedTags()
        {
            LastDownloadedTagsSerialized = JsonConvert.SerializeObject(_lastDownloadedTags);
        }
    }
}