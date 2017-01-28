using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RealmSync.SyncService;

namespace RealmSync.Server.Models
{
    public class ChangeTrackingDbContext : DbContext
    {
        private readonly Dictionary<Type, string> _syncedTypes;
        public ChangeTrackingDbContext(Type[] syncedTypes)
        {
            _syncedTypes = syncedTypes.ToDictionary(x => x, x => x.Name);
        }

        protected virtual IList<string> GetTagsForObject(IRealmSyncObjectServer obj)
        {
            return new[] { "all" };
        }
        public override int SaveChanges()
        {
            ProcessChanges();
            return base.SaveChanges();
        }

        private void ProcessChanges()
        {
            var syncStatusContext = new SyncStatusDbContext();

            foreach (var entity in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
            {
                if (_syncedTypes.ContainsKey(entity.Entity.GetType()))
                {
                    var diff = new Dictionary<string, object>();
                    foreach (var propName in entity.CurrentValues.PropertyNames)
                    {
                        var current = entity.CurrentValues[propName];
                        var original = entity.OriginalValues[propName];
                        if ((original?.Equals(current) != true) && !(current == null && original == null))
                        {
                            diff[propName] = current;
                        }
                    }

                    var obj = (IRealmSyncObjectServer)entity.Entity;
                    var syncObj = new SyncStatusServerObject()
                    {
                        ChangesAsJson = JsonConvert.SerializeObject(diff),
                    };
                    Process(syncObj, obj);
                    syncStatusContext.SyncStatusServerObjects.Add(syncObj);
                }
            }

            foreach (var entity in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
            {
                if (_syncedTypes.ContainsKey(entity.Entity.GetType()))
                {


                    var obj = (IRealmSyncObjectServer)entity.Entity;
                    var syncObj = new SyncStatusServerObject()
                    {
                    };
                    Process(syncObj, obj);
                    syncStatusContext.SyncStatusServerObjects.Add(syncObj);
                }
            }
            syncStatusContext.SaveChanges();
        }

        private void Process(SyncStatusServerObject syncObj, IRealmSyncObjectServer obj)
        {
            var typeName = obj.GetType().Name;
            var tags = GetTagsForObject(obj);
            if (tags.Count > 0)
                syncObj.Tag0 = tags[0];
            if (tags.Count > 1)
                syncObj.Tag1 = tags[1];
            if (tags.Count > 2)
                syncObj.Tag2 = tags[2];
            if (tags.Count > 3)
                syncObj.Tag3 = tags[3];

            syncObj.FullObjectAsJson = JsonConvert.SerializeObject(obj);
            syncObj.LastChange = DateTime.UtcNow;
            syncObj.Type = typeName;
            syncObj.MobilePrimaryKey = obj.MobilePrimaryKey;
        }

        public override Task<int> SaveChangesAsync()
        {
            ProcessChanges();
            return base.SaveChangesAsync();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            ProcessChanges();
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}