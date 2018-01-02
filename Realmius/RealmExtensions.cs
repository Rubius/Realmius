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
using System.Linq;
using System.Runtime.CompilerServices;
using Realmius.SyncService;
using Realms;
[assembly:InternalsVisibleTo("Realmius.Tests")]
namespace Realmius
{
    public static class RealmExtensions
    {
        public static void AddSkipUpload<T>(this Realm realm, T obj, bool update = false)
            where T : RealmObject, IRealmiusObjectClient
        {
            SkipUpload(realm, obj);

            realm.Add(obj, update);
        }

        public static void SkipUpload<T>(this Realm realm, T obj)
            where T : RealmObject, IRealmiusObjectClient
        {
            ForAllServices(realm, syncService =>
            {
                syncService?.SkipUpload(obj);
            });
        }

        internal static void AddSkipUpload(this Realm realm, IRealmiusObjectClient obj, bool update = false)
        {
            SkipUpload(realm, obj);

            realm.Add((RealmObject)obj, update);
        }

        internal static void SkipUpload(this Realm realm, IRealmiusObjectClient obj)
        {
            ForAllServices(realm, syncService =>
            {
                syncService?.SkipUpload(obj);
            });
        }

        private static void ForAllServices(Realm realm, Action<RealmiusSyncService> action)
        {
            var databasePath = realm.Config.DatabasePath;
            IList<RealmiusSyncService> syncServices;
            lock (SyncServiceFactory.SyncServices)
            {
                if (SyncServiceFactory.SyncServices.TryGetValue(databasePath, out syncServices))
                {
                    foreach (RealmiusSyncService syncService in syncServices.ToList())
                    {
                        action(syncService);
                    }
                }
                else
                {
                    action(null);
                }
            }
        }

        /// <summary>
        /// Removes item from Realm and sync the changes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="realm"></param>
        /// <param name="obj"></param>
        public static void RemoveAndSync<T>(this Realm realm, T obj)
            where T : RealmObject, IRealmiusObjectClient
        {
            ForAllServices(realm, syncService =>
            {
                syncService?.RemoveObject(obj);
                realm.Remove(obj);
            });
        }
    }
}