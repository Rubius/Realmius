using System;
using System.Collections.Generic;

namespace RealmSync.SyncService
{
    public class DownloadDataResponse
    {
        public List<DownloadRequestItem> ChangedObjects { get; set; }
		public DateTimeOffset LastChange { get; set; }

        public DownloadDataResponse()
        {
            ChangedObjects = new List<DownloadRequestItem>();
        }
    }
}