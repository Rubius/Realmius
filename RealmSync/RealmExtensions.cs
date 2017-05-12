using System;
using System.Collections.Generic;
using Realmius.SyncService;
using Realms;

namespace Realmius
{
    public static class RealmExtensions
    {
        public static void AddSkipUpload<T>(this Realm realm, T obj, bool update = false)
            where T : RealmObject, IRealmSyncObjectClient
        {
            SkipUpload(realm, obj);

            realm.Add(obj, update);
        }

        public static void SkipUpload<T>(this Realm realm, T obj)
            where T : RealmObject, IRealmSyncObjectClient
        {
            ForAllServices(
                realm,
                (syncService) =>
                {
                    syncService?.SkipUpload(obj);
                });

        }

        internal static void AddSkipUpload(this Realm realm, IRealmSyncObjectClient obj, bool update = false)
        {
            SkipUpload(realm, obj);

            realm.Add((RealmObject)obj, update);
        }

        internal static void SkipUpload(this Realm realm, IRealmSyncObjectClient obj)
        {
            ForAllServices(
                realm,
                (syncService) =>
                {
                    syncService?.SkipUpload(obj);
                });

        }

        private static void ForAllServices(Realm realm, Action<RealmSyncService> action)
        {
            var databasePath = realm.Config.DatabasePath;
            IList<RealmSyncService> syncServices;
            if (SyncServiceFactory.SyncServices.TryGetValue(databasePath, out syncServices))
            {
                foreach (RealmSyncService syncService in syncServices)
                {
                    action(syncService);
                }
            }
            else
            {
                action(null);
            }
        }

        /// <summary>
        /// Removes item rom Realm and sync the changes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="realm"></param>
        /// <param name="obj"></param>
        public static void RemoveAndSync<T>(this Realm realm, T obj)
            where T : RealmObject, IRealmSyncObjectClient
        {
            ForAllServices(realm,
                           (syncService) =>
                           {
                               syncService?.RemoveObject(obj);
                               realm.Remove(obj);
                           });
        }
    }
}