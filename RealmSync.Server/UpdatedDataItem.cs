using System.Collections.Generic;
using RealmSync.SyncService;

namespace RealmSync.Server
{
    internal class UpdatedDataItem
    {
        public IRealmSyncObjectServer DeserializedObject { get; set; }
        public DownloadResponseItem Change { get; set; }
        public IList<string> Tags { get; set; }

    }
}