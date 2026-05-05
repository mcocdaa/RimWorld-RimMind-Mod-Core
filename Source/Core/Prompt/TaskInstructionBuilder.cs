using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Verse;

namespace RimMind.Core.Prompt
{
    public static class TaskInstructionBuilder
    {
        public static string Build(string keyPrefix, params string[] subKeys)
        {
            var parts = new List<string>();
            foreach (var subKey in subKeys)
            {
                string fullKey = $"{keyPrefix}.{subKey}";
                string translated = fullKey.Translate();
                if (string.IsNullOrEmpty(translated)) continue;
                if (IsMissingTranslation(translated, fullKey))
                {
                    Log.Warning($"[RimMind-Core] TaskInstructionBuilder: missing translation for '{fullKey}'");
                    continue;
                }
                parts.Add(translated);
            }
            return parts.Count > 0 ? string.Join("\n\n", parts) : "";
        }

        private static bool IsMissingTranslation(string translated, string originalKey)
        {
            if (translated == originalKey) return true;
            string stripped = StripDiacritics(translated);
            return string.Equals(stripped, originalKey, StringComparison.OrdinalIgnoreCase);
        }

        private static string StripDiacritics(string text)
        {
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize();
        }
    }
}
