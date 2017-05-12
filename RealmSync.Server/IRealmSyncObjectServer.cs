using System;

namespace RealmSync.SyncService
{
    public interface IRealmSyncObjectServer
    {
        string MobilePrimaryKey { get; }
    }
}