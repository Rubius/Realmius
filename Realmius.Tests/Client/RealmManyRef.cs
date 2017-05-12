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
using Realmius.Infrastructure;
using Realmius.SyncService;
using Realms;

namespace Realmius.Tests.Client
{
    public class RealmManyRef : RealmObject, IRealmiusObjectClient
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Text { get; set; }

        [JsonConverter(typeof(RealmReferencesSerializer))]
        public IList<RealmRef> Children { get; }

        public string MobilePrimaryKey => Id;
    }
}