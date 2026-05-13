using System;
using System.Collections.Generic;

namespace RimMind.Application.Common.Interfaces.Flywheel
{
    public class ParameterRecommendation
    {
        public string Target = null!;
        public float CurrentValue;
        public float RecommendedValue;
        public float Confidence;
        public string Reason = null!;
    }

    public class AnalysisRecommendationEntry : ParameterRecommendation
    {
        public string TriggerRule = "";
        public string ComputationDetail = "";
        public bool Applied;
        public long ApplyTimestampTicks;
    }

    public class AnalysisReportRecord
    {
        public string AnalysisWindow = "";
        public int TotalRecords;
        public Dictionary<string, float> ComputedMetrics = new Dictionary<string, float>();
        public List<AnalysisRecommendationEntry> Recommendations = new List<AnalysisRecommendationEntry>();
        public long GeneratedAtTicks;

        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? RuleId { get; set; }
        public string? Reason { get; set; }
        public float OldValue { get; set; }
        public float NewValue { get; set; }
        public string? ParameterKey { get; set; }
        public string? Category { get; set; }
        public string? Detail { get; set; }
    }

    public interface IAnalysisReportWriter
    {
        void Write(AnalysisReportRecord report);
    }
}
