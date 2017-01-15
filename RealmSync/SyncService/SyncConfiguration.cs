using System;
using Realms;

namespace RealmSync.SyncService
{
    internal class SyncConfiguration : RealmObject
    {
        [PrimaryKey]
        public int Id { get; set; }
        public DateTimeOffset LastDownloaded
        {
            get;
            set;
        } = new DateTimeOffset(new DateTime(1970, 1, 1));
    }
}