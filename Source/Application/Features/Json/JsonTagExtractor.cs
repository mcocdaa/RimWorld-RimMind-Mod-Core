using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace RimMind.Application.Features.Json
{
    internal static class JsonTagExtractor
    {
        private static readonly Regex XmlTagRegex = new Regex(
            @"<(\w+)[^>]*>(.*?)</\1>",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex SelfClosingTagRegex = new Regex(
            @"<(\w+)[^>]*/>",
            RegexOptions.Compiled);

        public static Dictionary<string, string> ExtractTags(string input)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(input)) return result;

            foreach (Match match in XmlTagRegex.Matches(input))
            {
                if (match.Groups.Count >= 3)
                {
                    string tagName = match.Groups[1].Value;
                    string content = match.Groups[2].Value.Trim();
                    result[tagName] = content;
                }
            }

            return result;
        }

        public static string? ExtractTag(string input, string tagName)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(tagName)) return null;
            var pattern = $@"<{tagName}[^>]*>(.*?)</{tagName}>";
            var match = Regex.Match(input, pattern, RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        public static List<string> ExtractSelfClosingTags(string input)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(input)) return result;

            foreach (Match match in SelfClosingTagRegex.Matches(input))
            {
                result.Add(match.Groups[1].Value);
            }
            return result;
        }

        public static string StripAllTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return Regex.Replace(input, @"<[^>]+>", "").Trim();
        }
    }
}
