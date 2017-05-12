using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Realms;

namespace RealmSync.SyncService
{
    internal class SyncConfiguration : RealmObject
    {

        [PrimaryKey]
        public int Id { get; set; }

        public string LastDownloadedTagsSerialized { get; set; }

        private Dictionary<string, DateTimeOffset> _lastDownloadedTags;
        public Dictionary<string, DateTimeOffset> LastDownloadedTags
        {
            get
            {
                if (string.IsNullOrEmpty(LastDownloadedTagsSerialized))
                    _lastDownloadedTags = new Dictionary<string, DateTimeOffset>();

                return _lastDownloadedTags ?? (_lastDownloadedTags = JsonConvert.DeserializeObject<Dictionary<string, DateTimeOffset>>(LastDownloadedTagsSerialized));
            }
            set
            {
                _lastDownloadedTags = value;
                SaveLastDownloadedTags();
            }
        }

        public void SaveLastDownloadedTags()
        {
            LastDownloadedTagsSerialized = JsonConvert.SerializeObject(_lastDownloadedTags);
        }
    }
}