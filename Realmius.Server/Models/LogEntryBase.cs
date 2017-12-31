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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Realmius.Server.Models
{
    public class LogEntryBase
    {
        public int Id { get; set; }

        [MaxLength(40)]
        public string RecordIdString { get; set; }
        public int RecordIdInt { get; set; }

        public DateTimeOffset Time { get; set; }
        public string EntityType { get; set; }
        public string BeforeJson { get; set; }
        public string AfterJson { get; set; }
        public string ChangesJson { get; set; }

        public LogEntryBase()
        {

        }
    }
}