using System.ComponentModel.DataAnnotations;
using Realmius.Server;
using Realms;

namespace Realmius.Tests.Server.Models
{
    public class DbSyncObject : IRealmiusObjectServer
    {
        public string Text { get; set; }
        public string Tags { get; set; }

        #region IRealmiusObject

        [PrimaryKey]
        [Key]
        public string Id { get; set; }

        public string MobilePrimaryKey => Id;

        #endregion
    }
}