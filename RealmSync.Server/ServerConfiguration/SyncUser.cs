using System.Collections.Generic;

namespace RealmSync.Server
{
    public class SyncUser : ISyncUser
    {
        private static readonly IList<string> _tags = new[] { "all" };
        public IList<string> Tags => _tags;
    }
}