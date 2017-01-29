using System.Collections.Generic;
using RealmSync.Server.Models;

namespace RealmSync.Server
{
    public class UpdatedDataBatch
    {
        public IList<SyncStatusServerObject> Items { get; set; }

        public UpdatedDataBatch()
        {
            Items = new List<SyncStatusServerObject>();
        }
    }
}