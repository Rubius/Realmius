namespace RealmSync.SyncService
{
    public class DownloadRequestItem
    {
        public string Type { get; set; }
        public string MobilePrimaryKey { get; set; }
        public string SerializedObject { get; set; }
    }
}