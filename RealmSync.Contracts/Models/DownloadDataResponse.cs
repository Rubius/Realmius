using System;
using System.Collections.Generic;

namespace RealmSync.SyncService
{
    public class DownloadDataResponse
    {
        public List<DownloadResponseItem> ChangedObjects { get; set; }
		public Dictionary<string, DateTimeOffset> LastChange { get; set; }
        public bool LastChangeContainsNewTags { get; set; }
        public DownloadDataResponse()
        {
            ChangedObjects = new List<DownloadResponseItem>();
        }
    }
}