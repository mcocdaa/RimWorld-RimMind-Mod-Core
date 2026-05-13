using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace RimMind.Application.Features.Json
{
    public static class JsonTagExtractor
    {
        public static Action<string>? OnWarning;

        private static void Warn(string message)
        {
            OnWarning?.Invoke(message);
        }

        public static T? Extract<T>(string text, string tagName) where T : class
        {
            string? raw = ExtractRaw(text, tagName);
            if (raw == null) return null;

            try
            {
                return JsonConvert.DeserializeObject<T>(raw);
            }
            catch (Exception ex)
            {
                Warn($"[RimMind-Core] JsonTagExtractor.Extract deserialization failed: {ex.Message}");
                return null;
            }
        }

        public static List<T> ExtractAll<T>(string text, string tagName) where T : class
        {
            var result = new List<T>();
            foreach (var raw in ExtractAllRaw(text, tagName))
            {
                try
                {
                    var item = JsonConvert.DeserializeObject<T>(raw);
                    if (item != null) result.Add(item);
                }
                catch (Exception ex) { Warn($"[RimMind-Core] JsonTagExtractor.ExtractAll deserialization failed: {ex.Message}"); }
            }
            return result;
        }

        public static string? ExtractRaw(string text, string tagName)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(tagName))
                return null;

            var pattern = $@"<{Regex.Escape(tagName)}>([\s\S]*?)</{Regex.Escape(tagName)}>";
            var match = Regex.Match(text, pattern, RegexOptions.Singleline);
            if (!match.Success) return null;

            string content = match.Groups[1].Value.Trim();
            return string.IsNullOrEmpty(content) ? null : content;
        }

        public static List<string> ExtractAllRaw(string text, string tagName)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(tagName))
                return result;

            var pattern = $@"<{Regex.Escape(tagName)}>([\s\S]*?)</{Regex.Escape(tagName)}>";
            foreach (Match match in Regex.Matches(text, pattern, RegexOptions.Singleline))
            {
                string content = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(content))
                    result.Add(content);
            }
            return result;
        }
    }
}
