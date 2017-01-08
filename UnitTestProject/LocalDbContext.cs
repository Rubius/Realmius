using System.Data.Entity;

namespace UnitTestProject
{
    public class LocalDbContext : DbContext
    {
        static LocalDbContext()
        {
            Database.SetInitializer<LocalDbContext>(new DropCreateDatabaseAlways<LocalDbContext>());
        }

        public DbSet<DbSyncObject> DbSyncObjects { get; set; }
    }
}