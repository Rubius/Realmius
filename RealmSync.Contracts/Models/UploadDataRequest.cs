using System.Collections.Generic;

namespace Realmius.Contracts.Models
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