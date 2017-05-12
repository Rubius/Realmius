using System.Data.Entity;
using Realmius.Server.Migrations;

//using Realmius.Server.Migrations;

namespace Realmius.Server.Models
{
    public class SyncStatusDbContext : DbContext
    {
        public IDbSet<SyncStatusServerObject> SyncStatusServerObjects { get; set; }

        static SyncStatusDbContext()
        {
            Database.SetInitializer<SyncStatusDbContext>(new MigrateDatabaseToLatestVersion<SyncStatusDbContext, Configuration>(true));
            //Database.SetInitializer<SyncStatusDbContext>(new DropCreateDatabaseIfModelChanges<SyncStatusDbContext>());
        }

        public SyncStatusDbContext()
        {
        }

        public SyncStatusDbContext(string connectionString) : base(connectionString)
        {
        }
    }
}