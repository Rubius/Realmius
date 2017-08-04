using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Realmius.Server;

namespace Realmius_mancheck_Web.Models
{
    public class PhotoRealm : IRealmiusObjectServer
    {
        public string PhotoUri { get; set; }

        public string MobilePrimaryKey => Id.ToString();

        public string Id { get; set; }

        public string Title { get; set; }

        public DateTimeOffset PostTime { get; set; }
    }
}