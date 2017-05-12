using System.Collections.Generic;

namespace RealmSync.Server.Models
{
    public class SyncTypeInfo
    {
        public string TypeName { get; set; }

        /// <summary>
        /// properties that are out of this dictionary won't be saved in databases
        /// </summary>
        public Dictionary<string, bool> TrackedProperties { get; set; }
    }
}