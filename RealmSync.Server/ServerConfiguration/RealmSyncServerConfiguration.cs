using System;
using System.Collections.Generic;
using Realmius.Server.Models;

namespace Realmius.Server.ServerConfiguration
{
    public class RealmSyncServerConfiguration : SyncConfigurationBase<SyncUser>
    {
        private readonly Func<IRealmSyncObjectServer, IList<string>> _getTagsFunc;
        public override IList<Type> TypesToSync { get; }

        public RealmSyncServerConfiguration(IList<Type> typesToSync, Func<IRealmSyncObjectServer, IList<string>> getTagsFunc)
        {
            _getTagsFunc = getTagsFunc;
            TypesToSync = typesToSync;
        }

        public override bool CheckAndProcess(CheckAndProcessArgs<SyncUser> args)
        {
            return true;
        }

        public override IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmSyncObjectServer obj)
        {
            return _getTagsFunc(obj);
        }
    }
}