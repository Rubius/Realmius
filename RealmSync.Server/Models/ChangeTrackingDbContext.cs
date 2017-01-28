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
        private Dictionary<Type, string> _syncedTypes;
        public ChangeTrackingDbContext(Type[] syncedTypes)
        {
            _syncedTypes = syncedTypes.ToDictionary(x => x, x => x.Name);
        }

        public override int SaveChanges()
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

                    var typeName = entity.Entity.GetType().Name;
                    var obj = (IRealmSyncObjectServer)entity.Entity;
                    syncStatusContext.SyncStatusServerObjects.Add(new SyncStatusServerObject()
                    {
                        LastChange = DateTime.UtcNow,
                        Type = typeName,
                        MobilePrimaryKey = obj.MobilePrimaryKey,
                        SerializedObject = JsonConvert.SerializeObject(diff),
                    });
                }
            }

            foreach (var entity in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
            {
                if (_syncedTypes.ContainsKey(entity.Entity.GetType()))
                {
                    var typeName = entity.Entity.GetType().Name;

                    var obj = (IRealmSyncObjectServer)entity.Entity;
                    syncStatusContext.SyncStatusServerObjects.Add(new SyncStatusServerObject()
                    {
                        LastChange = DateTime.UtcNow,
                        Type = typeName,
                        MobilePrimaryKey = obj.MobilePrimaryKey,
                        SerializedObject = JsonConvert.SerializeObject(obj),
                    });
                }
            }
            syncStatusContext.SaveChanges();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}