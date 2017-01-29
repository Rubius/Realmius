using System;
using System.Collections.Generic;
using RealmSync.SyncService;

namespace RealmSync.Server
{
    public class ShareEverythingRealmSyncServerConfiguration : IRealmSyncServerConfiguration<ISyncUser>
    {
        public bool CheckAndProcess(IRealmSyncObjectServer deserialized, ISyncUser user)
        {
            return true;
        }

        public IList<Type> TypesToSync { get; }
        public IList<string> GetTagsForObject(IRealmSyncObjectServer obj)
        {
            return new[] { "all" };
        }

        public ShareEverythingRealmSyncServerConfiguration(IList<Type> typesToSync)
        {
            TypesToSync = typesToSync;
        }
        public ShareEverythingRealmSyncServerConfiguration(Type typeToSync, params Type[] typesToSync)
        {
            var types = new List<Type> { typeToSync };
            types.AddRange(typesToSync);
            TypesToSync = types;
        }
    }
}