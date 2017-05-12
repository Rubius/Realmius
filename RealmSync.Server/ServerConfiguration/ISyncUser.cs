using System.Collections.Generic;

namespace RealmSync.Server
{
    public interface ISyncUser
    {
        /// <summary>
        /// tags the user has access to
        /// </summary>
        IList<string> Tags { get; }
    }
}