////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

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