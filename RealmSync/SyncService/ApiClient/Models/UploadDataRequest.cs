using System.Collections.Generic;

namespace RealmSync.SyncService
{
    public class UploadDataRequest
    {
        public IList<UploadRequestItem> ChangeNotifications { get; set; }

        public UploadDataRequest()
        {
            ChangeNotifications = new List<UploadRequestItem>();
        }
    }
}