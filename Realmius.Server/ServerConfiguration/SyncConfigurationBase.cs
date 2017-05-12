using System;
using System.Collections.Generic;
using Realmius.Server.Models;

namespace Realmius.Server.ServerConfiguration
{
    public abstract class SyncConfigurationBase<TUser> : IRealmiusServerConfiguration<TUser>
        where TUser : ISyncUser
    {
        public abstract IList<Type> TypesToSync { get; }
        public abstract IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj);
        public abstract bool CheckAndProcess(CheckAndProcessArgs<TUser> args);

        public bool CheckAndProcess(ChangeTrackingDbContext ef, IRealmiusObjectServer deserialized, TUser user)
        {
            return CheckAndProcess(new CheckAndProcessArgs<TUser>()
            {
                Entity = deserialized,
                Database = ef,
                User = user,
                OriginalDbEntity = ef.CloneWithOriginalValues(deserialized),
            });
        }

        public virtual object[] KeyForType(Type type, string itemPrimaryKey)
        {
            return new object[] { itemPrimaryKey };
        }
    }
}