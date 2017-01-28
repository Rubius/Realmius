namespace RealmSync.Server.Models
{
    /// <summary>
    /// should be applied to types to have an ability to track changes made to DB directly (not via EF)
    /// </summary>
    public interface IDbEntityWithVersionSupport
    {
        /// <summary>
        /// should be incremented on each save
        /// </summary>
        int Version { get; set; }
    }
}