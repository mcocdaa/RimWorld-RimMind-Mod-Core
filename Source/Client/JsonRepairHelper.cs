using System;
using Newtonsoft.Json.Linq;

namespace RimMind.Core.Client
{
    public static class JsonRepairHelper
    {
        public static string Repair(string json) => json;

        public static string? TryRepairTruncatedJson(string? input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            var trimmed = input.TrimEnd();
            if (string.IsNullOrEmpty(trimmed)) return null;

            try
            {
                JToken.Parse(trimmed);
                return null;
            }
            catch { }

            string repaired = trimmed;

            if (repaired.EndsWith(",")) repaired = repaired.Substring(0, repaired.Length - 1);

            int openBraces = 0, openBrackets = 0;
            bool inString = false;
            char prev = '\0';
            foreach (char c in repaired)
            {
                if (prev != '\\' && c == '"') inString = !inString;
                if (!inString)
                {
                    if (c == '{') openBraces++;
                    else if (c == '}') openBraces--;
                    else if (c == '[') openBrackets++;
                    else if (c == ']') openBrackets--;
                }
                prev = c;
            }

            if (inString) repaired += "\"";

            for (int i = 0; i < openBrackets; i++) repaired += "]";
            for (int i = 0; i < openBraces; i++) repaired += "}";

            return repaired;
        }
    }
}
