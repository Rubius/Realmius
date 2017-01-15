using System;
using System.Collections.Generic;

namespace RealmSync.SyncService
{
    public class DownloadDataRequest
    {
        public IEnumerable<string> Types { get; set; }
        public DateTimeOffset LastChangeTime { get; set; }
    }
}