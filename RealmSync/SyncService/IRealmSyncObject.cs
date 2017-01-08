using System;

namespace RealmSync.SyncService
{
    public interface IRealmSyncObject
    {
        /// <summary>
        /// values are from SyncState enum. enums are not supported by Realm yet
        /// </summary>
        int SyncState { get; set; }
        //DateTimeOffset LastChangeClient { get; set; }
        //DateTimeOffset LastChangeServer { get; set; }
        string MobilePrimaryKey { get; }
        string LastSynchronizedVersion { get; set; }
    }

    public class RealmSyncObjectInfo
    {
        public DateTimeOffset LastChangeServer { get; set; }
        public string MobilePrimaryKey { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(Error);

        public string Error { get; set; }
    }
}