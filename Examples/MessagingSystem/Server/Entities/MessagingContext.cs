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

        public MessagingContext(string nameOrConnectionString, IRealmiusServerDbConfiguration syncConfiguration)
            : base(nameOrConnectionString, syncConfiguration)
        {
        }

        public MessagingContext() 
            : base(SyncConfiguration)
        {
        }

        public MessagingContext(string nameOrConnectionString, Type typeToSync, params Type[] typesToSync)
            : base(nameOrConnectionString, typeToSync, typesToSync)
        {
        }

        public MessagingContext(Type typeToSync, params Type[] typesToSync) 
            : base(typeToSync, typesToSync)
        {
        }
    }
}