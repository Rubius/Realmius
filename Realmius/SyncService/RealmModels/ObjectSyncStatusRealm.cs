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
    public class ObjectSyncStatusRealm : RealmObject
    {
        public const string SplitSymbols = "_$_";
        [PrimaryKey]
        public string Key { get; set; }

        private string _mobilePrimaryKey;

        [Ignored]
        public string MobilePrimaryKey
        {
            get
            {
                CheckTypeAndKey();
                return _mobilePrimaryKey;
            }
            set
            {
                _mobilePrimaryKey = value;
                UpdateKey();
            }
        }


        private string _type;

        [Ignored]
        public string Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
                UpdateKey();
            }
        }

        public DateTimeOffset DateTime { get; set; }
        public string SerializedObject { get; set; }
        public bool IsDeleted { get; set; }
        /// <summary>
        /// values are from SyncState enum. enums are not supported by Realm yet
        /// </summary>
        public int SyncState { get; set; }

        private void UpdateKey()
        {
            Key = $"{_type}{SplitSymbols}{_mobilePrimaryKey}";
        }

        private void CheckTypeAndKey()
        {
            if (!string.IsNullOrEmpty(_mobilePrimaryKey) && !string.IsNullOrEmpty(_type))
                return;

            var split = Key.Split(new[] { SplitSymbols }, StringSplitOptions.None);
            _type = split[0];
            _mobilePrimaryKey = split[1];
        }
    }
}