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

namespace Realmius.Contracts.Models
{
    public class DownloadDataRequest
    {
        public IEnumerable<string> Types { get; set; }
        public Dictionary<string, DateTimeOffset> LastChangeTime { get; set; }
        public bool OnlyDownloadSpecifiedTags { get; set; } = false;

        public DownloadDataRequest()
        {
            LastChangeTime = new Dictionary<string, DateTimeOffset>();
        }
    }
}