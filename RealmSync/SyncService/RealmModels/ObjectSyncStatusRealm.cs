using System;
using Realms;

namespace RealmSync.SyncService
{
    public class ObjectSyncStatusRealm : RealmObject
    {
        [Realms.PrimaryKey]
        public string MobilePrimaryKey { get; set; }
        public string Type { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public string SerializedObject { get; set; }
        /// <summary>
        /// values are from SyncState enum. enums are not supported by Realm yet
        /// </summary>
        public int SyncState { get; set; }
    }
}