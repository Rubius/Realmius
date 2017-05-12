using System;
using Realms;

namespace Realmius.SyncService.RealmModels
{
    public class UploadRequestItemRealm : RealmObject
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; }
        public string PrimaryKey { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public string SerializedObject { get; set; }
        public bool IsDeleted { get; set; }
    }
}