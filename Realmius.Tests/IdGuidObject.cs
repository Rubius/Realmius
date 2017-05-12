using System;
using System.ComponentModel.DataAnnotations;
using Realmius.Server;

namespace Realmius.Tests
{
    public class IdGuidObject : IRealmiusObjectServer
    {
        public string Text { get; set; }
        public string Tags { get; set; }

        #region IRealmiusObject

        [Key]
        public Guid Id { get; set; }

        public string MobilePrimaryKey => Id.ToString();

        #endregion
    }
}