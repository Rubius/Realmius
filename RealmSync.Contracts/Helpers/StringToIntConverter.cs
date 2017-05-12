using System;
using Newtonsoft.Json;

namespace RealmSync.Contracts.Helpers
{

    public class StringToIntConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(int))
            {
                return int.Parse(reader.Value.ToString());
            }

            return reader.Value;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(int);
        }
    }
}