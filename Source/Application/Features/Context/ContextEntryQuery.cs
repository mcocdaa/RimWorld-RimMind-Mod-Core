using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    internal static class ContextEntryQuery
    {
        public static List<ContextEntry> FilterByScenario(
            IReadOnlyList<ContextEntry> entries,
            string scenarioId,
            string? currentQuery = null)
        {
            if (entries == null || entries.Count == 0) return new List<ContextEntry>();
            return entries.ToList();
        }

        public static List<ContextEntry> FilterByBudget(
            List<ContextEntry> entries,
            float budget)
        {
            if (entries == null) return new List<ContextEntry>();
            return entries;
        }

        public static List<ContextEntry> ExcludeKeys(
            List<ContextEntry> entries,
            string[]? excludeKeys)
        {
            if (entries == null) return new List<ContextEntry>();
            if (excludeKeys == null || excludeKeys.Length == 0) return entries;
            return entries;
        }
    }
}
