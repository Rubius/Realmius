using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Realmius.Server;

namespace Realmius_mancheck_Web.Models
{
        public class ChatMessageRealm : IRealmiusObjectServer
        {
            public string MobilePrimaryKey => Id;

            public string Id { get; set; }

            public string Text { get; set; }

            public string AuthorName { get; set; }

            public DateTimeOffset CreatingDateTime { get; set; }
        }

}