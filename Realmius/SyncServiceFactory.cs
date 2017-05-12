using System;
using System.Collections.Generic;
using Realmius.SyncService;
using Realmius.SyncService.ApiClient;
using Realms;

namespace Realmius
{
    public class SyncServiceFactory
    {
        internal static Dictionary<string, IList<RealmiusService>> SyncServices { get; private set; } = new Dictionary<string, IList<RealmiusService>>();
        public static IRealmiusService CreateUsingPolling(Func<Realm> realmFactoryMethod, Uri uploadUri, Uri downloadUri, Type[] typesToSync, bool deleteDatabase = false)
        {
            var apiClient = new PollingSyncApiClient(uploadUri, downloadUri);
            var syncService = new RealmiusService(realmFactoryMethod, apiClient, deleteDatabase, typesToSync);

            return syncService;
        }

        public static IRealmiusService CreateUsingSignalR(Func<Realm> realmFactoryMethod, Uri uri, string hubName, Type[] typesToSync, bool deleteDatabase = false)
        {
            var apiClient = new SignalRSyncApiClient(uri, hubName);
            var syncService = new RealmiusService(realmFactoryMethod, apiClient, deleteDatabase, typesToSync);

            return syncService;
        }
    }
}