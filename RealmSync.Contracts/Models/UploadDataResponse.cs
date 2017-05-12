using System.Collections.Generic;

namespace Realmius.Contracts.Models
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