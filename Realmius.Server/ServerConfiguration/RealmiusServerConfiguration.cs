using System;
using System.Collections.Generic;
using Realmius.Server.Models;

namespace Realmius.Server.ServerConfiguration
{
    public class RealmiusServerConfiguration : SyncConfigurationBase<SyncUser>
    {
        private readonly Func<IRealmiusObjectServer, IList<string>> _getTagsFunc;
        public override IList<Type> TypesToSync { get; }

        public RealmiusServerConfiguration(IList<Type> typesToSync, Func<IRealmiusObjectServer, IList<string>> getTagsFunc)
        {
            _getTagsFunc = getTagsFunc;
            TypesToSync = typesToSync;
        }

        public override bool CheckAndProcess(CheckAndProcessArgs<SyncUser> args)
        {
            return true;
        }

        public override IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj)
        {
            return _getTagsFunc(obj);
        }
    }
}