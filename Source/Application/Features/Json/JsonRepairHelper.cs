using System.Text;
using System.Text.RegularExpressions;

namespace RimMind.Application.Features.Json
{
    internal static class JsonRepairHelper
    {
        private static readonly Regex TrailingCommaRegex = new Regex(
            @",\s*([}\]])",
            RegexOptions.Compiled);

        private static readonly Regex UnquotedKeyRegex = new Regex(
            @"(?<=[{,]\s*)([a-zA-Z_][a-zA-Z0-9_]*)\s*:",
            RegexOptions.Compiled);

        public static string? TryRepair(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            try
            {
                Newtonsoft.Json.Linq.JToken.Parse(input);
                return input;
            }
            catch
            {
                return Repair(input);
            }
        }

        public static string Repair(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";

            var result = new StringBuilder(input);

            result = new StringBuilder(TrailingCommaRegex.Replace(result.ToString(), "$1"));

            var balanced = BalanceBrackets(result.ToString());
            return balanced;
        }

        private static string BalanceBrackets(string input)
        {
            int openCurly = 0, openSquare = 0;
            foreach (char c in input)
            {
                if (c == '{') openCurly++;
                else if (c == '}') openCurly--;
                else if (c == '[') openSquare++;
                else if (c == ']') openSquare--;
            }

            var sb = new StringBuilder(input);
            while (openSquare > 0) { sb.Append(']'); openSquare--; }
            while (openCurly > 0) { sb.Append('}'); openCurly--; }
            return sb.ToString();
        }
    }
}
