namespace RealmSync.SyncService
{
    public class UploadRequestItem
    {
        public string Type { get; set; }
        public string PrimaryKey { get; set; }
        public string SerializedObject { get; set; }
        public bool IsDeleted { get; set; }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(PrimaryKey)}: {PrimaryKey}, {(IsDeleted ? "Deleted" : $"{nameof(SerializedObject)}: {SerializedObject.Replace("\r", "").Replace("\n", "").Replace("  ", " ")}")}";
        }
    }
}