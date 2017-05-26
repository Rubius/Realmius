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
    public class UploadFileInfo : RealmObject
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string PathToFile { get; set; }
        public string Url { get; set; }
        public string QueryParams { get; set; }
        public string FileParameterName { get; set; }

        /// <summary>
        /// AdditionalInfo is passed back with FileUploaded event, so the client could store any specific information identifying the file here
        /// </summary>
        public string AdditionalInfo { get; set; }

        [Indexed]
        public DateTimeOffset Added { get; set; }
        [Indexed]
        public bool UploadFinished { get; set; }
    }
}