using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealmSync.Server.Models
{
    public class LogEntryBase
    {
        public int Id { get; set; }

        [Index]
        [MaxLength(40)]
        public string RecordIdString { get; set; }
        [Index]
        public int RecordIdInt { get; set; }

        [Index("IX_Time")]
        public DateTimeOffset Time { get; set; }
        public string EntityType { get; set; }
        public string BeforeJson { get; set; }
        public string AfterJson { get; set; }
        public string ChangesJson { get; set; }

        public LogEntryBase()
        {

        }
    }
}