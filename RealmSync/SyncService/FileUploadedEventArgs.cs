namespace Realmius.SyncService
{
    public class FileUploadedEventArgs
    {
        public string AdditionalInfo { get; set; }
        public string QueryParams { get; set; }
        public string PathToFile { get; set; }

        public FileUploadedEventArgs(string additionalInfo, string queryParams, string pathToFile)
        {
            AdditionalInfo = additionalInfo;
            QueryParams = queryParams;
            PathToFile = pathToFile;
        }
    }
}