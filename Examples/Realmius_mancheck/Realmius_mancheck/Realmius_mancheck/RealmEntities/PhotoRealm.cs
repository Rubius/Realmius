using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Realmius.SyncService;
using Realms;

namespace Realmius_mancheck.RealmEntities
{
    public class PhotoRealm : RealmObject, IRealmiusObjectClient
    {
        public string PhotoUri { get; set; }

        public string MobilePrimaryKey => Id;

        [PrimaryKey]
        public string Id { get; set; }

        public string Title { get; set; }

        public DateTimeOffset PostTime { get; set; }
    }
}
