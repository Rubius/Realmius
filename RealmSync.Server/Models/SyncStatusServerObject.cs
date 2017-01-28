using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealmSync.Server.Models
{

    public class ObjectTag
    {
        public string Tag { get; set; }
    }
    public class SyncStatusServerObject
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(40)]
        public string MobilePrimaryKey { get; set; }

        [Index("IX_Download0", 2)]
        [MaxLength(40)]
        public string Type { get; set; }
        public int Version { get; set; }

        [Index("IX_Download0", 1)]
        public DateTimeOffset LastChange { get; set; }
        public string ChangesAsJson { get; set; }
        public string FullObjectAsJson { get; set; }

        [Index("IX_Download0", 3)]
        [MaxLength(40)]
        public string Tag0 { get; set; }
        [MaxLength(40)]
        public string Tag1 { get; set; }
        [MaxLength(40)]
        public string Tag2 { get; set; }
        [MaxLength(40)]
        public string Tag3 { get; set; }
    }
}