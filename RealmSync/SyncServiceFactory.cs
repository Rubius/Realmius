using System;
using Realms;
using RealmSync.SyncService;

namespace RealmSync
{
    public class SyncServiceFactory
    {
        public static RealmSyncService CreateUsingPolling(Func<Realm> realmFactoryMethod, Uri uploadUri, Uri downloadUri, params Type[] typesToSync)
        {
            var apiClient = new PollingSyncApiClient(uploadUri, downloadUri);
            var syncService = new RealmSyncService(realmFactoryMethod, apiClient, typesToSync);

            return syncService;
        }

        public static RealmSyncService CreateUsingSignalR(Func<Realm> realmFactoryMethod, Uri uri, params Type[] typesToSync)
        {
            var apiClient = new SignalRSyncApiClient(uri);
            var syncService = new RealmSyncService(realmFactoryMethod, apiClient, typesToSync);

            return syncService;
        }
    }
}