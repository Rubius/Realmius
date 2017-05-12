using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Realmius.Server;

namespace Realmius.Tests
{
    public class IdIntObject : IRealmiusObjectServer
    {
        public string Text { get; set; }
        public string Tags { get; set; }

        #region IRealmiusObject

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string MobilePrimaryKey => Id.ToString();

        #endregion
    }
}