using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RimMind.Contracts.Internal;
using RimMind.Kernel.Abstractions;
using RimMind.Kernel.Logging;

namespace RimMind.Kernel.Prompt
{
    public static class TaskInstructionBuilder
    {
        public static string Build(string keyPrefix, params string[] subKeys)
        {
            var parts = new List<string>();
            var translationService = RimMindServiceLocator.Get<ITranslationService>();
            foreach (var subKey in subKeys)
            {
                string fullKey = $"{keyPrefix}.{subKey}";
                string translated = translationService?.Translate(fullKey) ?? fullKey;
                if (string.IsNullOrEmpty(translated)) continue;
                if (IsMissingTranslation(translated, fullKey))
                {
                    RimMindLogger.Warning($"TaskInstructionBuilder: missing translation for '{fullKey}'");
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
