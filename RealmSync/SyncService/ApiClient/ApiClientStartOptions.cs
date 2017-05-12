using System;
using System.Collections.Generic;

namespace Realmius.SyncService.ApiClient
{
    public class ApiClientStartOptions
    {
        public ApiClientStartOptions(Dictionary<string, DateTimeOffset> lastDownloaded, IEnumerable<string> types)
        {
            LastDownloaded = lastDownloaded;
            Types = types;
        }

        public Dictionary<string, DateTimeOffset> LastDownloaded { get; set; }
        public IEnumerable<string> Types { get; set; }
    }
}