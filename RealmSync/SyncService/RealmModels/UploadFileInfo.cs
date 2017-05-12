using System;
using Realms;

namespace Realmius.SyncService.RealmModels
{
    public class UploadFileInfo : RealmObject
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string PathToFile { get; set; }
        public string Url { get; set; }
        public string QueryParams { get; set; }
        public string FileParameterName { get; set; }

        /// <summary>
        /// AdditionalInfo is passed back with FileUploaded event, so the client could store any specific information identifying the file here
        /// </summary>
        public string AdditionalInfo { get; set; }

        [Indexed]
        public DateTimeOffset Added { get; set; }
        [Indexed]
        public bool UploadFinished { get; set; }
    }
}