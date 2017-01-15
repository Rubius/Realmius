using System;
using System.Collections.Generic;

namespace RealmSync.SyncService
{
    public class DownloadDataResponse
    {
        public List<DownloadResponseItem> ChangedObjects { get; set; }
		public DateTimeOffset LastChange { get; set; }

        public DownloadDataResponse()
        {
            ChangedObjects = new List<DownloadResponseItem>();
        }
    }
}