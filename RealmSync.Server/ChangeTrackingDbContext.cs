using System.Data.Entity;

namespace RealmSync.Server
{
    public class ChangeTrackingDbContext : DbContext
    {
        public IDbSet<SyncStatusServerObject> SyncStatusServerObjects { get; set; }
    }
}