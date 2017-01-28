using System;
using Realms;

namespace RealmSync.SyncService
{
    public class UploadRequestItemRealm : RealmObject
    {
        public string Type { get; set; }
        public string PrimaryKey { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public string SerializedObject { get; set; }
    }
}