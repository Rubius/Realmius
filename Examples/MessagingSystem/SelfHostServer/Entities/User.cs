using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Realmius.Server;
using Realmius.Server.ServerConfiguration;

namespace Server.Entities
{
    [Table("Users")]
    public class User : IRealmiusObjectServer, ISyncUser
    {
        public string Id { get; set; }

        public string Nickname { get; set; }

        public string MobilePrimaryKey => Id;

        public IList<string> Tags { get; } = new List<string> {"all"};
    }
}