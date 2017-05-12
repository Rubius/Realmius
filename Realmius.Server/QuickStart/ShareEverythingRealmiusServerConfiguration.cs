using System;
using System.Collections.Generic;
using Realmius.Server.Models;
using Realmius.Server.ServerConfiguration;

namespace Realmius.Server.QuickStart
{
    public class ShareEverythingRealmiusServerConfiguration : ShareEverythingRealmiusServerConfiguration<ISyncUser>
    {
        public ShareEverythingRealmiusServerConfiguration(IList<Type> typesToSync) : base(typesToSync)
        {
        }

        public ShareEverythingRealmiusServerConfiguration(Type typeToSync, params Type[] typesToSync) : base(typeToSync, typesToSync)
        {
        }
    }

    public class ShareEverythingRealmiusServerConfiguration<T> : SyncConfigurationBase<T>
        where T : ISyncUser
    {
        public override bool CheckAndProcess(CheckAndProcessArgs<T> args)
        {
            return true;
        }

        public override IList<Type> TypesToSync { get; }
        public override IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj)
        {
            return new[] { "all" };
        }

        public ShareEverythingRealmiusServerConfiguration(IList<Type> typesToSync)
        {
            TypesToSync = typesToSync;
        }
        public ShareEverythingRealmiusServerConfiguration(Type typeToSync, params Type[] typesToSync)
        {
            var types = new List<Type> { typeToSync };
            types.AddRange(typesToSync);
            TypesToSync = types;
        }
    }
}