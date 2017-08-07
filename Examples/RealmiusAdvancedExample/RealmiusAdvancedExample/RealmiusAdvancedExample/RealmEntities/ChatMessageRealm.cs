using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Realmius.SyncService;
using Realms;

namespace RealmiusAdvancedExample.RealmEntities
{
    public class ChatMessageRealm : RealmObject, IRealmiusObjectClient
    {
        public string MobilePrimaryKey => Id;

        [PrimaryKey]
        public string Id { get; set; }

        public string Text { get; set; }

        public string AuthorName { get; set; }

        public DateTimeOffset CreatingDateTime { get; set;  }

        public ChatMessageRealm(string text)
        {
            Id = Guid.NewGuid().ToString();
            Text = text;
            AuthorName = App.CurrentUser.Name;
            CreatingDateTime = DateTimeOffset.Now;
        }

        public ChatMessageRealm()
        {
        }
    }
}
