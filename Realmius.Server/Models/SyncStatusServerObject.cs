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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Realmius.Server.Models
{
    public class ObjectTag
    {
        public string Tag { get; set; }
    }

    [Table("_RealmSyncStatus")]
    public class SyncStatusServerObject
    {
        [MaxLength(40)]
        public string MobilePrimaryKey { get; set; }

        [MaxLength(40)]
        public string Type { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset LastChange { get; set; }
        public string FullObjectAsJson { get; set; }

        [MaxLength(40)]
        public string Tag0 { get; set; }
        [MaxLength(40)]
        public string Tag1 { get; set; }
        [MaxLength(40)]
        public string Tag2 { get; set; }
        [MaxLength(40)]
        public string Tag3 { get; set; }

        private string _columnChangeDatesLastDeserialized;
        private Dictionary<string, DateTimeOffset> _columnChangeDates = new Dictionary<string, DateTimeOffset>();

        public string ColumnChangeDatesSerialized { get; set; }

        public void UpdateColumnChangeDatesSerialized()
        {
            ColumnChangeDatesSerialized = JsonConvert.SerializeObject(ColumnChangeDates);
        }

        [NotMapped]
        public Dictionary<string, DateTimeOffset> ColumnChangeDates
        {
            get
            {
                if (_columnChangeDatesLastDeserialized == ColumnChangeDatesSerialized)
                    return _columnChangeDates;

                if (!string.IsNullOrEmpty(ColumnChangeDatesSerialized))
                    _columnChangeDates = JsonConvert.DeserializeObject<Dictionary<string, DateTimeOffset>>(ColumnChangeDatesSerialized) ?? new Dictionary<string, DateTimeOffset>();
                else
                    _columnChangeDates = new Dictionary<string, DateTimeOffset>();

                _columnChangeDatesLastDeserialized = ColumnChangeDatesSerialized;
                return _columnChangeDates;
            }
            set => _columnChangeDates = value;
        }

        public SyncStatusServerObject()
        {
        }

        public SyncStatusServerObject(string type, string mobilePrimaryKey)
        {
            Type = type;
            MobilePrimaryKey = mobilePrimaryKey;
        }
    }
}