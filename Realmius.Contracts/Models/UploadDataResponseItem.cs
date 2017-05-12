namespace Realmius.Contracts.Models
{
    public class UploadDataResponseItem
    {
        public string MobilePrimaryKey { get; set; }
        public string Type { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(Error);

        public string Error { get; set; }

        private UploadDataResponseItem()
        {
        }

        public UploadDataResponseItem(string mobilePrimaryKey, string type, string error = null)
        {
            MobilePrimaryKey = mobilePrimaryKey;
            Type = type;
            Error = error;
        }


        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, Key: {MobilePrimaryKey} {Error}".Trim();
        }
    }
}