﻿﻿﻿using System.Linq;
using FluentAssertions;
using RimMind.Core.Agent;
using RimMind.Kernel.Bus;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseA
{
    public class PawnAgentConstructorTests
    {
        [Fact]
        [Trait("Phase", "A")]
        public void R_A2_PawnAgent_Constructor_ShouldInject_IEventBus()
        {
            var ctorParams = typeof(PawnAgent).GetConstructors()
                .SelectMany(c => c.GetParameters())
                .ToList();

            ctorParams.Should().ContainSingle(
                p => p.ParameterType == typeof(IEventBus),
                "PawnAgent constructor must accept IEventBus for dependency injection");
        }
    }
}
