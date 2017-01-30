using Realms;

namespace RealmSync.SyncService
{
    public class UploadFileInfo : RealmObject
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string PathToFile { get; set; }
        public string Url { get; set; }
        public string QueryParams { get; set; }
    }
}