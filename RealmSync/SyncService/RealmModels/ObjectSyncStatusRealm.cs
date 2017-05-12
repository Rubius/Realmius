using System;
using Realms;

namespace Realmius.SyncService.RealmModels
{
    public class ObjectSyncStatusRealm : RealmObject
    {
        public const string SplitSymbols = "_$_";
        [Realms.PrimaryKey]
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