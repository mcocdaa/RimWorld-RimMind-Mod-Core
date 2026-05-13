using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Models.Prompt;

namespace RimMind.Application.Common.Models.Context
{
    public class PromptBudget
    {
        public int TotalTokens { get; set; }
        public int ReserveForOutput { get; set; }
        public int RemainingTokens => TotalTokens;

        public PromptBudget(int totalTokens, int reserveForOutput = 0)
        {
            TotalTokens = totalTokens;
            ReserveForOutput = reserveForOutput;
        }

        public List<PromptSection>? Compose(List<PromptSection> sections)
        {
            if (sections == null) return null;
            var sorted = sections.OrderByDescending(s => s.Priority).ToList();
            var result = new List<PromptSection>();
            int used = 0;
            foreach (var sec in sorted)
            {
                if (used + sec.EstimatedTokens > TotalTokens - ReserveForOutput)
                {
                    if (sec.IsCompressible && sec.Compress != null)
                    {
                        var compressed = sec.Clone();
                        compressed.Content = sec.Compress(sec.Content);
                        compressed.EstimatedTokens = PromptSection.EstimateTokens(compressed.Content);
                        if (used + compressed.EstimatedTokens <= TotalTokens - ReserveForOutput)
                        {
                            result.Add(compressed);
                            used += compressed.EstimatedTokens;
                            continue;
                        }
                    }
                    continue;
                }
                result.Add(sec);
                used += sec.EstimatedTokens;
            }
            return result.OrderBy(s => s.Priority).ToList();
        }
    }
}
