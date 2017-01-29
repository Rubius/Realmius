using System;
using System.Collections.Generic;
using RealmSync.SyncService;

namespace RealmSync.Server
{
    public class RealmSyncServerConfiguration : IRealmSyncServerConfiguration<SyncUser>
    {
        private readonly Func<IRealmSyncObjectServer, IList<string>> _getTagsFunc;
        public IList<Type> TypesToSync { get; }

        public RealmSyncServerConfiguration(IList<Type> typesToSync, Func<IRealmSyncObjectServer, IList<string>> getTagsFunc)
        {
            _getTagsFunc = getTagsFunc;
            TypesToSync = typesToSync;
        }

        public bool CheckAndProcess(IRealmSyncObjectServer deserialized, SyncUser user)
        {
            return true;
        }

        public IList<string> GetTagsForObject(IRealmSyncObjectServer obj)
        {
            return _getTagsFunc(obj);
        }
    }
}