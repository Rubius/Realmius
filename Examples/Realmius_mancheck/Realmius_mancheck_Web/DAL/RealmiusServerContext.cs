using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;
using Realmius.Server.Models;
using Realmius_mancheck_Web.Models;

namespace Realmius_mancheck_Web.DAL
{
    public class RealmiusServerContext : ChangeTrackingDbContext
    {
        public RealmiusServerContext() : base("RealmiusServerContext")
        {

        }

        public DbSet<NoteRealm> Notes { get; set; }

        public DbSet<PhotoRealm> Photos { get; set; }

        public DbSet<ChatMessageRealm> ChatMessages { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}