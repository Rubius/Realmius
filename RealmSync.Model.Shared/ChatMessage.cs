using System;
using Newtonsoft.Json;
using RealmSync.SyncService;

namespace RealmSync.Model
{

    public partial class ChatMessage
    {
        public string Author { get; set; }
        public string Text { get; set; }
        public string Text2 { get; set; }
        public DateTimeOffset DateTime { get; set; } = new DateTimeOffset(new DateTime(1970, 1, 1));
    }
}