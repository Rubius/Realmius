using System;
using System.Reflection;

namespace RealmSync.SyncService
{
    internal class RealmObjectTypeInfo
    {
        public Type Type { get; private set; }
        public bool ImplementsSyncState { get; private set; }

        private static readonly Type syncObjectWithSyncStatusInterface = typeof(IRealmSyncObjectWithSyncStatusClient);
        public RealmObjectTypeInfo(Type type)
        {
            Type = type;
            ImplementsSyncState =
                syncObjectWithSyncStatusInterface.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }
    }
}