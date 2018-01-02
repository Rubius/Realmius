////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;

namespace Realmius.Server.Models
{
    public class SyncStatusDbContext : DbContext
    {
        private readonly string _connectionString;
        public DbSet<SyncStatusServerObject> SyncStatusServerObjects { get; set; }

        static SyncStatusDbContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SyncStatusServerObject>()
                .HasKey(c => new { c.Type, c.MobilePrimaryKey })
                ;

            modelBuilder.Entity<SyncStatusServerObject>()
                .HasIndex(x => x.LastChange);

            modelBuilder.Entity<SyncStatusServerObject>()
                .HasIndex(x => x.Tag0);

            modelBuilder.Entity<LogEntryBase>()
                .HasIndex(x => x.Time);

            modelBuilder.Entity<LogEntryBase>()
                .HasIndex(x => x.RecordIdInt);

            modelBuilder.Entity<LogEntryBase>()
                .HasIndex(x => x.RecordIdString);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!string.IsNullOrEmpty(_connectionString))
            {
                optionsBuilder.UseSqlServer(_connectionString);
            }
        }

        public SyncStatusDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SyncStatusDbContext(DbContextOptions<SyncStatusDbContext> dbContextOptions)
            : base(dbContextOptions)
        {
        }
    }
}