using System;
using Realmius.Server;

namespace Server.Entities
{
    public class Message : IRealmiusObjectServer
    {
        public string MobilePrimaryKey { get; }
        
        public long Id { get; set; }

        public DateTime DateTime { get; set; }

        public long ClientId { get; set; }

        public string Text { get; set; }
    }
}