using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace RealmSync.Server.Models
{
    public class EfEntityInfo
    {
        public Dictionary<string, bool> ModifiedProperties { get; set; }
        public object Entity { get; set; }
        public DbPropertyValues CurrentValues { get; set; }
        public DbPropertyValues OriginalValues { get; set; }
        public EntityState State { get; set; }
    }
}