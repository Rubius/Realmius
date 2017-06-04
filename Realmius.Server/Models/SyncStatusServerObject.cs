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
        [Key]
        [Column(Order = 1)]
        [MaxLength(40)]
        public string MobilePrimaryKey { get; set; }

        [Index("IX_Download0", 2)]
        [Key]
        [Column(Order = 0)]
        [MaxLength(40)]
        public string Type { get; set; }

        public bool IsDeleted { get; set; }

        [Index("IX_Download0", 1)]
        public DateTimeOffset LastChange { get; set; }
        public string FullObjectAsJson { get; set; }

        [Index("IX_Download0", 3)]
        [MaxLength(40)]
        public string Tag0 { get; set; }
        [MaxLength(40)]
        public string Tag1 { get; set; }
        [MaxLength(40)]
        public string Tag2 { get; set; }
        [MaxLength(40)]
        public string Tag3 { get; set; }

        private string _columnChangeDatesSerialized;
        public string ColumnChangeDatesSerialized
        {
            get => _columnChangeDatesSerialized;
            set
            {
                _columnChangeDatesSerialized = value;
                ColumnChangeDates = value == null
                    ? null
                    : JsonConvert.DeserializeObject<Dictionary<string, DateTimeOffset>>(value);
            }
        }

        public void UpdateColumnChangeDatesSerialized()
        {
            _columnChangeDatesSerialized = JsonConvert.SerializeObject(ColumnChangeDates);
        }

        [NotMapped]
        public Dictionary<string, DateTimeOffset> ColumnChangeDates { get; set; } = new Dictionary<string, DateTimeOffset>();

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