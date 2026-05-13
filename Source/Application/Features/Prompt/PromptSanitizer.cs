using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RimMind.Application.Features.Prompt
{
    internal static class PromptSanitizer
    {
        private static readonly Regex ControlCharRegex = new Regex(
            @"[\x00-\x08\x0B\x0C\x0E-\x1F]",
            RegexOptions.Compiled);

        public static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var result = ControlCharRegex.Replace(input, "");
            result = result.Trim();
            return result;
        }

        public static string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength) return input;
            return input.Substring(0, maxLength) + "...";
        }

        public static string RemoveDuplicateLines(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var seen = new HashSet<string>();
            var sb = new StringBuilder();
            foreach (var line in input.Split('\n'))
            {
                if (seen.Add(line))
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }
    }
}
