using System;
using System.Collections.Generic;

namespace Realmius.Contracts.Models
{
    public class DownloadDataRequest
    {
        public IEnumerable<string> Types { get; set; }
        public Dictionary<string, DateTimeOffset> LastChangeTime { get; set; }
        public bool OnlyDownloadSpecifiedTags { get; set; } = false;

        public DownloadDataRequest()
        {
            LastChangeTime = new Dictionary<string, DateTimeOffset>();
        }
    }
}