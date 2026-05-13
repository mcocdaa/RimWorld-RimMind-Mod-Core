using System;

namespace RimMind.Application.Common.Models.Prompt
{
    public class PromptSection
    {
        public string Name { get; set; } = "";
        public string Content { get; set; } = "";
        public int Priority { get; set; }
        public int EstimatedTokens { get; set; }
        public bool IsCompressible { get; set; }
        public Func<string, string>? Compress { get; set; }

        public PromptSection Clone()
        {
            return new PromptSection
            {
                Name = Name,
                Content = Content,
                Priority = Priority,
                EstimatedTokens = EstimatedTokens,
                IsCompressible = IsCompressible,
                Compress = Compress
            };
        }

        public static int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (int)(text.Length / 3.5);
        }
    }
}
