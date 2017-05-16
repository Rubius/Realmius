using System.Collections.Generic;
using Realmius.Server;
using Realmius.Server.ServerConfiguration;

namespace Server.Entities
{
    public class User : IRealmiusObjectServer, ISyncUser
    {
        public string MobilePrimaryKey { get; }

        public IList<string> Tags { get; } = new List<string>();
    }
}