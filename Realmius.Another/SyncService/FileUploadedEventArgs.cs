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

namespace Realmius.SyncService
{
    public class FileUploadedEventArgs
    {
        public string AdditionalInfo { get; set; }
        public string QueryParams { get; set; }
        public string PathToFile { get; set; }

        public FileUploadedEventArgs(string additionalInfo, string queryParams, string pathToFile)
        {
            AdditionalInfo = additionalInfo;
            QueryParams = queryParams;
            PathToFile = pathToFile;
        }
    }
}