using Realms;

namespace RealmSync.SyncService
{
    public class UploadFileInfo : RealmObject
    {
        public string PathToFile { get; set; }
        public string Url { get; set; }
        public string QueryParams { get; set; }
    }
}