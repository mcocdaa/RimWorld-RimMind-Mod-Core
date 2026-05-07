using System;
using System.Collections.Generic;

namespace RimMind.Kernel.Flywheel
{
    public class AnalysisReportRecord
    {
        public string AnalysisWindow = "";
        public int TotalRecords;
        public Dictionary<string, float> ComputedMetrics = new Dictionary<string, float>();
        public List<AnalysisRecommendationEntry> Recommendations = new List<AnalysisRecommendationEntry>();
        public long GeneratedAtTicks;
    }

    public class AnalysisRecommendationEntry : ParameterRecommendation
    {
        public string TriggerRule = "";
        public string ComputationDetail = "";
        public bool Applied;
        public long ApplyTimestampTicks;
    }
}
