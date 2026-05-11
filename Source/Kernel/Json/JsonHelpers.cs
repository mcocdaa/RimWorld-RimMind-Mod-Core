using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RimMind.Kernel.Json
{
    public static class JsonHelpers
    {
        private static readonly JsonSerializerSettings _settings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public static T? SafeDeserialize<T>(string json) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, _settings);
            }
            catch
            {
                return null;
            }
        }

        public static T? SafeDeserialize<T>(string json, JsonSerializerSettings settings) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch
            {
                return null;
            }
        }

        public static T[] SafeDeserializeArray<T>(string json) where T : class
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T[]>(json, _settings);
                return result ?? Array.Empty<T>();
            }
            catch
            {
                return Array.Empty<T>();
            }
        }

        public static string? ExtractString(string json, string propertyName)
        {
            try
            {
                var obj = JObject.Parse(json);
                var token = obj[propertyName];
                return token?.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static bool TryParseJson(string input, out JToken? token)
        {
            token = null;
            try
            {
                token = JToken.Parse(input);
                return token != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
