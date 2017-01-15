using System.Collections.Generic;

namespace RealmSync.Server
{
    public class UpdatedDataBatch
    {
        public List<UpdatedDataItem> Items { get; set; }

        public UpdatedDataBatch()
        {
            Items = new List<UpdatedDataItem>();
        }
    }
}