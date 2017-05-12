using System.Collections.Generic;

namespace RealmSync.SyncService
{
    public class UploadDataResponse
    {
        public List<UploadDataResponseItem> Results { get; set; }

        public UploadDataResponse()
        {
            Results = new List<UploadDataResponseItem>();
        }
    }
}