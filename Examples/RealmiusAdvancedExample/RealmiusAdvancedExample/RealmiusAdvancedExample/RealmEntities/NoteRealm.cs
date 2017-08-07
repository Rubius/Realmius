using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Realmius.SyncService;
using Realms;

namespace RealmiusAdvancedExample.RealmEntities
{
    public class NoteRealm : RealmObject, IRealmiusObjectClient
    {
        public string MobilePrimaryKey => Id;

        public string Description { get; set; }

        [PrimaryKey]
        public string Id { get; set; }

        public string Title { get; set; }

        public DateTimeOffset PostTime { get; set; }
    }
}
