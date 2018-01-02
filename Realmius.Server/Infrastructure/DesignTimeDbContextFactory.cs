using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Realmius.Server.Models;

namespace Realmius.Server.Infrastructure
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SyncStatusDbContext>
    {
        public SyncStatusDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<SyncStatusDbContext>();
            builder.UseSqlServer(
                "Server=localhost\\mssqllocaldb;Database=Realmius_Migrations;Integrated Security=true");

            return new SyncStatusDbContext(builder.Options);
        }
    }
}