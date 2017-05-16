using System;
using System.ComponentModel.DataAnnotations.Schema;
using Realmius.Server;

namespace Server.Entities
{
    [Table("Messages")]
    public class Message : IRealmiusObjectServer
    {
        public string Id { get; set; }

        public string MobilePrimaryKey => Id;
        
        public DateTime DateTime { get; set; }

        public long UserId { get; set; }

        public string Text { get; set; }
    }
}