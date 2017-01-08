using System.Collections.Generic;

namespace RealmSync.SyncService
{
    public class UploadDataResponse
    {
        public List<RealmSyncObjectInfo> Results { get; set; }

        public UploadDataResponse()
        {
            Results = new List<RealmSyncObjectInfo>();
        }
    }
}