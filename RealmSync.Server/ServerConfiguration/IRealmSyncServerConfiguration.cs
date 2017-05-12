using System;
using System.Collections.Generic;
using Realmius.Server.Models;

namespace Realmius.Server.ServerConfiguration
{
    /// <summary>
    /// Do not implement this! Implement IRealmSyncServerConfiguration<TUser> instead!
    /// </summary>
    public interface IRealmSyncServerDbConfiguration
    {
        IList<Type> TypesToSync { get; }
        IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmSyncObjectServer obj);
    }

    public interface IRealmSyncServerConfiguration<TUser> : IRealmSyncServerDbConfiguration
        where TUser : ISyncUser
    {
        bool CheckAndProcess(CheckAndProcessArgs<TUser> args);
        object[] KeyForType(Type type, string itemPrimaryKey);
    }
}