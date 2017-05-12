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

namespace Realmius.Contracts.Models
{
    public class UploadDataResponseItem
    {
        public string MobilePrimaryKey { get; set; }
        public string Type { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(Error);

        public string Error { get; set; }

        private UploadDataResponseItem()
        {
        }

        public UploadDataResponseItem(string mobilePrimaryKey, string type, string error = null)
        {
            MobilePrimaryKey = mobilePrimaryKey;
            Type = type;
            Error = error;
        }


        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, Key: {MobilePrimaryKey} {Error}".Trim();
        }
    }
}