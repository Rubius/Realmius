using System.Data.Entity;
using RealmSync.Server;
using RealmSync.Server.Models;

namespace UnitTestProject
{
    public class LocalDbContext : ChangeTrackingDbContext
    {
        static LocalDbContext()
        {
            Database.SetInitializer<LocalDbContext>(new DropCreateDatabaseAlways<LocalDbContext>());
        }

        public LocalDbContext(IRealmSyncServerDbConfiguration config) : base(config)
        {
        }

        public DbSet<DbSyncObject> DbSyncObjects { get; set; }
    }
}