using System;
using Realms;

namespace Realmius.SyncService
{
    public class UploadFileParameters
    {
        
    }
    internal class UploadFileInfo : RealmObject
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string PathToFile { get; set; }
        public string Url { get; set; }
        public string QueryParams { get; set; }
        public string FileParameterName { get; set; }

        [Indexed]
        public DateTimeOffset Added { get; set; }
        [Indexed]
        public bool UploadFinished { get; set; }
    }
}