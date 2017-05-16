// /****************************** MessagingSystem ******************************\
// Project:            Server
// Filename:           ShareEverythingConfiguration.cs
// Created:            16.05.2017
// 
// <summary>
// 
// </summary>
// \***************************************************************************/

using System;
using System.Collections.Generic;
using Realmius.Server;
using Realmius.Server.Models;
using Realmius.Server.ServerConfiguration;

namespace Server.Sync
{
    public class ShareEverythingRealmSyncServerConfiguration : ShareEverythingRealmSyncServerConfiguration<ISyncUser>
    {
        public ShareEverythingRealmSyncServerConfiguration(IList<Type> typesToSync) : base(typesToSync)
        {
        }

        public ShareEverythingRealmSyncServerConfiguration(Type typeToSync, params Type[] typesToSync) : base(typeToSync, typesToSync)
        {
        }
    }

    public class ShareEverythingRealmSyncServerConfiguration<T> : SyncConfigurationBase<T>
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