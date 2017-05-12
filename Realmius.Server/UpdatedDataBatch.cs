using System.Collections.Generic;
using Realmius.Server.Models;

namespace Realmius.Server
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