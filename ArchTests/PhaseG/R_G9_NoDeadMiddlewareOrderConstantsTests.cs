using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using RimMind.Application.Common.Models;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    /// <summary>
    /// R-G9: MiddlewareOrder must not contain dead constants.
    /// Constants that have no Middleware references are dead code and must be removed
    /// to keep the centralized ordering table trustworthy.
    /// </summary>
    public class R_G9_NoDeadMiddlewareOrderConstantsTests
    {
        // These constants have been verified (grep across all 9 submodules) to have
        // zero MiddlewareOrder.XXX references in code. They are dead and must stay absent.
        private static readonly string[] DeadConstants =
        {
            "LayerBuild",     // was 300, duplicates CircuitBreaker=300 value, no Middleware uses it
            "Retry",          // was 800, superseded by UnifiedRetry=400, no Middleware uses it
            "NpcChatRetry",   // was 800, no Middleware uses it
            "CacheStore",     // was 900, no Middleware uses it
        };

        [Fact]
        [Trait("Phase", "G")]
        public void MiddlewareOrder_Constants_Should_Not_Contain_Dead_Constants()
        {
            var presentDead = DeadConstants
                .Where(name => typeof(RimMindDefaults.MiddlewareOrder)
                    .GetField(name, BindingFlags.Public | BindingFlags.Static) != null)
                .ToList();

            presentDead.Should().BeEmpty(
                "R-G9: MiddlewareOrder must not contain dead constants. " +
                "The following constants have no Middleware references and must be removed: " +
                $"{string.Join(", ", presentDead)}. " +
                "Dead ordering constants mislead maintainers who assume every entry maps to a live middleware.");
        }
    }
}
