using System.Data.Entity;
using RealmSync.Server.Models;

namespace UnitTestProject
{
    public class LocalDbContext : ChangeTrackingDbContext
    {
        static LocalDbContext()
        {
            Database.SetInitializer<LocalDbContext>(new DropCreateDatabaseAlways<LocalDbContext>());
        }

        public LocalDbContext() : base(new[] { typeof(DbSyncObject) })
        {

        }
        public DbSet<DbSyncObject> DbSyncObjects { get; set; }
    }
}