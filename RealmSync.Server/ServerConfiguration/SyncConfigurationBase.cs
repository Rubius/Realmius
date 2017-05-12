using System;
using System.Collections.Generic;
using RealmSync.Server.Models;
using RealmSync.SyncService;

namespace RealmSync.Server
{
    public abstract class SyncConfigurationBase<TUser> : IRealmSyncServerConfiguration<TUser>
        where TUser : ISyncUser
    {
        public abstract IList<Type> TypesToSync { get; }
        public abstract IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmSyncObjectServer obj);
        public abstract bool CheckAndProcess(CheckAndProcessArgs<TUser> args);

        public bool CheckAndProcess(ChangeTrackingDbContext ef, IRealmSyncObjectServer deserialized, TUser user)
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