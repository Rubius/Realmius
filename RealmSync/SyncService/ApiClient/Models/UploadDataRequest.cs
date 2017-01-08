using System.Collections.Generic;

namespace RealmSync.SyncService
{
    public class UploadDataRequest
    {
        public List<UploadRequestItem> ChangeNotifications { get; set; }

        public UploadDataRequest()
        {
            ChangeNotifications = new List<UploadRequestItem>();
        }
    }
}