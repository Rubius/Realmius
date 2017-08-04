using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Realmius.Server;

namespace Realmius_mancheck_Web.Models
{
    public class NoteRealm : IRealmiusObjectServer
    {
        public string MobilePrimaryKey => Id.ToString();

        public string Description { get; set; }
        
        public string Id { get; set; }

        public string Title { get; set; }

        public DateTimeOffset PostTime { get; set; }

        public int UserRole { get; set; }
    }
}