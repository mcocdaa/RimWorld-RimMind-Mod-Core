using System.Text;
using System.Text.RegularExpressions;

namespace RimMind.Core.Prompt
{
    public static class PromptSanitizer
    {
        private static readonly Regex ZeroWidthChars = new Regex(
            @"[\u200B\u200C\u200D\uFEFF\u2060\u180E\u200E\u200F]",
            RegexOptions.Compiled);

        private static readonly Regex[] InjectionPatterns = new[]
        {
            new Regex(@"ignore\s+(all\s+)?previous\s+(instructions?|prompts?|rules?)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"forget\s+(all\s+)?(previous\s+)?(instructions?|context|rules?)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"you\s+are\s+now\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"new\s+instruction\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"system\s*:\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };

        public static string Sanitize(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return prompt;
            prompt = prompt.Replace("{{", "{").Replace("}}", "}");
            return prompt;
        }

        public static string SanitizeUserInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            input = input.Normalize(NormalizationForm.FormKC);
            input = ZeroWidthChars.Replace(input, "");
            foreach (var pattern in InjectionPatterns)
                input = pattern.Replace(input, "[filtered]");
            return input;
        }
    }
}
