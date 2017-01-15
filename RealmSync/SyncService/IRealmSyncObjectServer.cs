using System;

namespace RealmSync.SyncService
{
    public interface IRealmSyncObjectServer
    {
        DateTime LastChangeServer { get; set; }
        string MobilePrimaryKey { get; }
    }
}