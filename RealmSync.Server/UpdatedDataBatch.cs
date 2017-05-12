using System.Collections.Generic;
using RealmSync.Server.Models;

namespace RealmSync.Server
{
    public class UpdatedDataBatch
    {
        public IList<DownloadResponseItemInfo> Items { get; set; }

        public UpdatedDataBatch()
        {
            Items = new List<DownloadResponseItemInfo>();
        }
    }
}