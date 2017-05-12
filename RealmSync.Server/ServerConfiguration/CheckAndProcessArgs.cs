using RealmSync.Server.Models;
using RealmSync.SyncService;

namespace RealmSync.Server
{
    public class CheckAndProcessArgs<TUser>
    {
        /// <summary>
        /// reference to EF to retrieve entities if needed
        /// </summary>
        public ChangeTrackingDbContext Database { get; set; }

        /// <summary>
        /// user that is uploading the changes
        /// </summary>
        public TUser User { get; set; }

        /// <summary>
        /// entity with user's changes applied
        /// </summary>
        public IRealmSyncObjectServer Entity { get; set; }

        /// <summary>
        /// entity as it is in database before user's changes are applied
        /// </summary>
        public IRealmSyncObjectServer OriginalDbEntity { get; set; }

    }
}