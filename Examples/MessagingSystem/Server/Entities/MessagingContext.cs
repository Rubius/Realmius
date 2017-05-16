using System;
using System.Data.Entity;
using Realmius.Server.Models;
using Realmius.Server.ServerConfiguration;

namespace Server.Entities
{
    public class MessagingContext : ChangeTrackingDbContext
    {
        public static IRealmiusServerConfiguration<User> SyncConfiguration { get; set; }

        public IDbSet<Message> Messages { get; set; }
        public IDbSet<User> Users { get; set; }

        static MessagingContext()
        {
            Database.SetInitializer<MessagingContext>(new DropCreateDatabaseAlways<MessagingContext>());
        }

        public MessagingContext(IRealmiusServerDbConfiguration syncConfiguration) 
            : base(SyncConfiguration)
        {
        }
    }
}