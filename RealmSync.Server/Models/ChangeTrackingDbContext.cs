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
        public static event EventHandler<UpdatedDataBatch> DataUpdated;
        protected virtual void OnDataUpdated(UpdatedDataBatch e)
        {
            DataUpdated?.Invoke(this, e);
        }

        private IRealmSyncServerDbConfiguration _syncConfiguration;
        private Dictionary<Type, string> _syncedTypes;

        public ChangeTrackingDbContext(string nameOrConnectionString, IRealmSyncServerDbConfiguration syncConfiguration)
            : base(nameOrConnectionString)
        {
            Initialize(syncConfiguration);
        }
        public ChangeTrackingDbContext(IRealmSyncServerDbConfiguration syncConfiguration)
        {
            Initialize(syncConfiguration);
        }

        /// <summary>
        /// this will share everything!
        /// </summary>
        public ChangeTrackingDbContext(string nameOrConnectionString, Type typeToSync, params Type[] typesToSync)
            : base(nameOrConnectionString)
        {
            Initialize(typeToSync, typesToSync);
        }
        /// <summary>
        /// this will share everything!
        /// </summary>
        public ChangeTrackingDbContext(Type typeToSync, params Type[] typesToSync)
        {
            Initialize(typeToSync, typesToSync);
        }

        private void Initialize(Type typeToSync, Type[] typesToSync)
        {
            var syncConfiguration = new ShareEverythingRealmSyncServerConfiguration(typeToSync, typesToSync);
            Initialize(syncConfiguration);
        }

        private void Initialize(IRealmSyncServerDbConfiguration syncConfiguration)
        {
            _syncConfiguration = syncConfiguration;
            _syncedTypes = _syncConfiguration.TypesToSync.ToDictionary(x => x, x => x.Name);
        }


        protected virtual IList<string> GetTagsForObject(IRealmSyncObjectServer obj)
        {
            return _syncConfiguration.GetTagsForObject(obj);
        }
        public override int SaveChanges()
        {
            ProcessChanges();
            return base.SaveChanges();
        }

        protected virtual void ProcessChanges()
        {
            var connectionString = this.Database.Connection.ConnectionString;
            var syncStatusContext = new SyncStatusDbContext(connectionString);

            var updatedResult = new UpdatedDataBatch();

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


                    updatedResult.Items.Add(syncObj);
                }
            }

            foreach (var entity in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
            {
                if (_syncedTypes.ContainsKey(entity.Entity.GetType()))
                {
                    var obj = (IRealmSyncObjectServer)entity.Entity;
                    var syncObj = new SyncStatusServerObject();
                    Process(syncObj, obj);
                    syncStatusContext.SyncStatusServerObjects.Add(syncObj);

                    updatedResult.Items.Add(syncObj);
                }
            }
            syncStatusContext.SaveChanges();

            if (updatedResult.Items.Count > 0)
                OnDataUpdated(updatedResult);
        }

        protected virtual void Process(SyncStatusServerObject syncObj, IRealmSyncObjectServer obj)
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