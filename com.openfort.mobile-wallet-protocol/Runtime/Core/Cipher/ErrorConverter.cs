using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MobileWalletProtocol
{
    public class ErrorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Exception).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var message = jo["message"]?.Value<string>();
            var code = jo["code"]?.Value<int>();

            var error = new Exception(message);
            if (code.HasValue)
            {
                error.Data["code"] = code.Value;
            }
            return error;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var error = value as Exception;
            writer.WriteStartObject();
            
            if (error.Data.Contains("code"))
            {
                writer.WritePropertyName("code");
                writer.WriteValue(error.Data["code"]);
            }

            writer.WritePropertyName("message");
            writer.WriteValue(error.Message);
            
            writer.WriteEndObject();
        }
    }
}