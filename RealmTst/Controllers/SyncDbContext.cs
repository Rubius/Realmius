using System.Data.Entity;
using RealmSync.Model;
using RealmSync.Server.Models;
using RealmWeb.Migrations;

//using RealmTst.Migrations;

namespace RealmTst.Controllers
{
    public class SyncDbContext : ChangeTrackingDbContext
    {
        static SyncDbContext()
        {
            //System.Data.Entity.Database.SetInitializer<SyncDbContext>(new MigrateDatabaseToLatestVersion<SyncDbContext, Configuration>());
            System.Data.Entity.Database.SetInitializer<SyncDbContext>(new DropCreateDatabaseAlways<SyncDbContext>());
        }

        public SyncDbContext() : base("DefaultConnection", typeof(ChatMessage))
        {

        }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        //public DbSet<ToDoItem> ToDoItems { get; set; }
        //public DbSet<Project> Projects { get; set; }
    }
}