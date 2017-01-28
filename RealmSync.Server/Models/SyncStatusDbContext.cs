using System.Data.Entity;

namespace RealmSync.Server.Models
{
    public class SyncStatusDbContext : DbContext
    {
        public IDbSet<SyncStatusServerObject> SyncStatusServerObjects { get; set; }

        static SyncStatusDbContext()
        {
            Database.SetInitializer<SyncStatusDbContext>(new DropCreateDatabaseIfModelChanges<SyncStatusDbContext>());
        }
    }
}