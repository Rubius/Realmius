using System;
using Realms;
using RealmSync.SyncService;

namespace RealmSync.Model
{

    //public class RealmObjectSyncBase : RealmObject, IRealmSyncObject
    //{

    //}

    public partial class ChatMessage :
#if __IOS__
        RealmObject, 
#endif
        IRealmSyncObject
    {
        public string Author { get; set; }
        public string Text { get; set; }
        public DateTimeOffset DateTime { get; set; }

        #region IRealmSyncObject
        [PrimaryKey]
        public string Id { get; set; }

        public int SyncState { get; set; }
        public string MobilePrimaryKey { get { return Id; } }

        [Ignored]
        public string LastSynchronizedVersion { get; set; }
        #endregion
    }

    //public class ToDoItem : RealmObjectSyncBase
    //{
    //    public bool Done { get; set; }
    //    public string Title { get; set; }
    //    public string Description { get; set; }
    //}

    //public class Project : RealmObjectSyncBase
    //{

    //}
}