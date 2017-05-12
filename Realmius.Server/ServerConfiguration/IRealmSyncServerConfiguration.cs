using System;
using System.Collections.Generic;
using Realmius.Server.Models;

namespace Realmius.Server.ServerConfiguration
{
    /// <summary>
    /// Do not implement this! Implement IRealmiusServerConfiguration<TUser> instead!
    /// </summary>
    public interface IRealmiusServerDbConfiguration
    {
        IList<Type> TypesToSync { get; }
        IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj);
    }

    public interface IRealmiusServerConfiguration<TUser> : IRealmiusServerDbConfiguration
        where TUser : ISyncUser
    {
        bool CheckAndProcess(CheckAndProcessArgs<TUser> args);
        object[] KeyForType(Type type, string itemPrimaryKey);
    }
}