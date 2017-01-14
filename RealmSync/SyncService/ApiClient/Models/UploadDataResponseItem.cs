namespace RealmSync.SyncService
{
    public class UploadDataResponseItem
    {
        public string MobilePrimaryKey { get; set; }
        public string Type { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(Error);

        public string Error { get; set; }

        public UploadDataResponseItem(string mobilePrimaryKey, string type, string error = null)
        {
            MobilePrimaryKey = mobilePrimaryKey;
            Type = type;
            Error = error;
        }
    }
}