using System.Collections.Generic;
using Realmius.Contracts.Models;

namespace Realmius.Server
{
    internal class UpdatedDataItem
    {
        public IRealmiusObjectServer DeserializedObject { get; set; }
        public DownloadResponseItem Change { get; set; }
        public IList<string> Tags { get; set; }

    }
}