using System.Collections.Generic;

namespace Realmius.Server.ServerConfiguration
{
    public interface ISyncUser
    {
        /// <summary>
        /// tags the user has access to
        /// </summary>
        IList<string> Tags { get; }
    }
}