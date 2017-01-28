namespace RealmSync.SyncService
{
    public class UploadRequestItem
    {
        public string Type { get; set; }
        public string PrimaryKey { get; set; }
        public string SerializedObject { get; set; }
    }
}