using System;
using System.Collections.Generic;
using System.Linq;
using NetArchTest.Rules;

namespace RimMind.Core.ArchTests
{
    public static class ArchTestExtensions
    {
        public static string FormatFailingTypes(this TestResult result)
        {
            if (result.IsSuccessful)
                return string.Empty;

            var names = result.FailingTypes?
                .Select(t => t.FullName ?? t.Name)
                .OrderBy(n => n)
                ?? Enumerable.Empty<string>();

            return string.Join("\n  ", names);
        }

        public static string FormatFailingTypes(this IEnumerable<Type> types)
        {
            var names = types
                .Select(t => t.FullName ?? t.Name)
                .OrderBy(n => n);

            return string.Join("\n  ", names);
        }
    }
}
