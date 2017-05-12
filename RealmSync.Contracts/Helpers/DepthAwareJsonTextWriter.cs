using System.IO;
using Newtonsoft.Json;

namespace RealmSync.Contracts.Helpers
{
    internal class DepthAwareJsonTextWriter : JsonTextWriter
    {
        public DepthAwareJsonTextWriter(TextWriter textWriter) : base(textWriter) { }

        public int CurrentDepth { get; private set; }

        public override void WriteStartObject()
        {
            CurrentDepth++;
            base.WriteStartObject();
        }

        public override void WriteEndObject()
        {
            CurrentDepth--;
            base.WriteEndObject();
        }
    }
}