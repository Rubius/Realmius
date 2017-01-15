using RealmSync.SyncService;

namespace RealmSync.Server
{
    public class UpdatedDataItem
    {
        public IRealmSyncObjectServer DeserializedObject { get; set; }
        public DownloadResponseItem Change { get; set; }
    }
}