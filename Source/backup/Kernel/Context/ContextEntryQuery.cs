using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Context;

namespace RimMind.Kernel.Context
{
    public static class ContextEntryQuery
    {
        private const int DefaultHour = 12;
        private const int DefaultColonistCount = 0;

        public static int ExtractHour(IReadOnlyList<ContextEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.Metadata != null &&
                    entry.Metadata.TryGetValue("key", out var key) && key == "time")
                {
                    if (entry.Metadata.TryGetValue("hour", out var hourStr) &&
                        int.TryParse(hourStr, out var hour) && hour >= 0 && hour <= 23)
                    {
                        return hour;
                    }
                    return ParseHourFromContent(entry.Content);
                }
            }
            return DefaultHour;
        }

        public static int ExtractColonistCount(IReadOnlyList<ContextEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.Metadata != null &&
                    entry.Metadata.TryGetValue("key", out var key) && key == "colonistCount")
                {
                    if (entry.Metadata.TryGetValue("count", out var countStr) &&
                        int.TryParse(countStr, out var count) && count >= 0)
                    {
                        return count;
                    }
                    return ParseCountFromContent(entry.Content);
                }
            }
            return DefaultColonistCount;
        }

        private static int ParseHourFromContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return DefaultHour;
            var digitStart = -1;
            var digitEnd = -1;
            for (int i = 0; i < content.Length; i++)
            {
                if (char.IsDigit(content[i]))
                {
                    if (digitStart < 0) digitStart = i;
                    digitEnd = i;
                }
                else if (digitStart >= 0)
                {
                    break;
                }
            }
            if (digitStart >= 0 && digitEnd >= digitStart &&
                int.TryParse(content.Substring(digitStart, digitEnd - digitStart + 1), out var hour))
            {
                return hour;
            }
            return DefaultHour;
        }

        private static int ParseCountFromContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return DefaultColonistCount;
            var digits = new string(content.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var count) ? count : DefaultColonistCount;
        }
    }
}
