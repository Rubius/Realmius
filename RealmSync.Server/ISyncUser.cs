using System.Collections.Generic;

namespace RealmSync.Server
{
    public interface ISyncUser
    {
        IList<string> Tags { get; }
    }
}