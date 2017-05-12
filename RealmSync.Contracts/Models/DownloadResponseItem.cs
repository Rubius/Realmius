namespace RealmSync.SyncService
{
    public class DownloadResponseItem
    {
        public string Type { get; set; }
        public bool IsDeleted { get; set; }
        public string MobilePrimaryKey { get; set; }
        public string SerializedObject { get; set; }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, Key: {MobilePrimaryKey}, {(IsDeleted ? "Deleted" : $"{nameof(SerializedObject)}: {SerializedObject.Replace("\r", "").Replace("\n", "").Replace("  ", " ")}")}";
        }
    }
}