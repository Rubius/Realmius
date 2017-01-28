using System;
using System.ComponentModel.DataAnnotations;

namespace RealmSync.Server.Models
{
    public class SyncStatusServerObject
    {
        [Key]
        public int Id { get; set; }

        public string MobilePrimaryKey { get; set; }
        public string Type { get; set; }
        public int Version { get; set; }

        public DateTimeOffset LastChange { get; set; }
        public string SerializedObject { get; set; }
    }
}