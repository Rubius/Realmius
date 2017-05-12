using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Realmius.Server;
using Realmius.Server.Infrastructure;
using Realms;

namespace Realmius.Tests.Server.Models
{
    
    public class RefSyncObject : IRealmSyncObjectServer
    {
        public string Text { get; set; }

        [JsonConverter(typeof(RealmServerCollectionConverter))]
        public virtual IList<RefSyncObject> References { get; set; }

        #region IRealmSyncObject

        [PrimaryKey]
        [Key]
        public string Id { get; set; }

        public string MobilePrimaryKey => Id;

        #endregion
    }
}