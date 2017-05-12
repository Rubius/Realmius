using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using RealmSync.SyncService;

namespace RealmSync.Tests
{
    public class IdIntObject :
        IRealmSyncObjectServer
    {
        public string Text { get; set; }
        public string Tags { get; set; }

        #region IRealmSyncObject

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string MobilePrimaryKey => Id.ToString();

        #endregion
    }
}