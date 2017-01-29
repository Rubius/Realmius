using System;
using System.Collections.Generic;
using RealmSync.SyncService;

namespace RealmSync.Server
{
    public interface IRealmSyncServerDbConfiguration
    {
        IList<Type> TypesToSync { get; }
        IList<string> GetTagsForObject(IRealmSyncObjectServer obj);
    }

    public interface IRealmSyncServerConfiguration<in TUser> : IRealmSyncServerDbConfiguration
        where TUser : ISyncUser
    {
        bool CheckAndProcess(IRealmSyncObjectServer deserialized, TUser user);
    }
}