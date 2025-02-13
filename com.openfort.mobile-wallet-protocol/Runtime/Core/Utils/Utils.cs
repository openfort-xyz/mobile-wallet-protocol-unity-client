using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MobileWalletProtocol
{
    static class Utils
    {
        const string k_MWPResponsePath = "mobile-wallet-protocol";

        public static string AppendMWPResponsePath(string urlString)
        {
            if (urlString.EndsWith("/"))
            {
                return $"{urlString}{k_MWPResponsePath}";
            }
            else
            {
                return $"{urlString}/{k_MWPResponsePath}";
            }
        }

        static readonly JsonSerializerSettings s_JsonSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new JsonConverter[]
            {
                new ErrorConverter()
            }
        };

        public static string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, s_JsonSettings);
        }

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, s_JsonSettings);
        }
    }
}