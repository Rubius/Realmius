using System.Collections.Generic;

namespace Realmius.Server.ServerConfiguration
{
    public class SyncUserTagged : ISyncUser
    {
        public IList<string> Tags { get; set; }

        public SyncUserTagged(IList<string> tags)
        {
            Tags = tags;
        }
    }
}