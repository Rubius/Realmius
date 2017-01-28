using System;
using System.ComponentModel.DataAnnotations;

namespace RealmSync.Server
{
    public class SyncStatusServerObject
    {
        [Key]
        public string MobilePrimaryKey { get; set; }
        public string Type { get; set; }

        public DateTimeOffset LastChange { get; set; }
        public string SerializedObject { get; set; }
    }
}