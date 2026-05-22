using System;
using System.Linq;
using FluentAssertions;
using RimMind.Application.Common.Interfaces.Agent.Planning;
using RimMind.Application.Common.Interfaces.Agent.Reflection;
using RimMind.Domain.Agent.Planning;
using RimMind.Domain.Agent.Reflection;
using RimMind.Domain.Events;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseI
{
    /// <summary>
    /// ArchTest rules for Phase I+ (Reflection / Inner Voice / Daily Planning).
    /// Rules R-I+1 through R-I+6 as defined in spec.
    /// Only tests public types (internal types not accessible across net48/net10 boundary).
    /// </summary>
    public class R_Iplus1_ReflectionStrategyNamespaceTests
    {
        [Fact]
        [Trait("Phase", "I+")]
        public void IReflectionStrategy_Should_Be_In_Agent_Reflection_Namespace()
        {
            typeof(IReflectionStrategy).Namespace.Should().Be(
                "RimMind.Application.Common.Interfaces.Agent.Reflection",
                "IReflectionStrategy must be in Application.Common.Interfaces.Agent.Reflection namespace (R-I+6)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void IReflectionStrategy_Should_Have_ShouldReflect_Method()
        {
            var method = typeof(IReflectionStrategy).GetMethod("ShouldReflect");
            method.Should().NotBeNull("IReflectionStrategy must have ShouldReflect method (R-I+1)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void IReflectionStrategy_Should_Have_ReflectAsync_Method()
        {
            var method = typeof(IReflectionStrategy).GetMethod("ReflectAsync");
            method.Should().NotBeNull("IReflectionStrategy must have ReflectAsync method (R-I+4)");
        }
    }

    public class R_Iplus2_DailyPlannerNamespaceTests
    {
        [Fact]
        [Trait("Phase", "I+")]
        public void IDailyPlanner_Should_Be_In_Agent_Planning_Namespace()
        {
            typeof(IDailyPlanner).Namespace.Should().Be(
                "RimMind.Application.Common.Interfaces.Agent.Planning",
                "IDailyPlanner must be in Application.Common.Interfaces.Agent.Planning namespace (R-I+6)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void IDailyPlanner_Should_Have_ShouldPlan_Method()
        {
            var method = typeof(IDailyPlanner).GetMethod("ShouldPlan");
            method.Should().NotBeNull("IDailyPlanner must have ShouldPlan method (R-I+2)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void IDailyPlanner_Should_Have_PlanAsync_Method()
        {
            var method = typeof(IDailyPlanner).GetMethod("PlanAsync");
            method.Should().NotBeNull("IDailyPlanner must have PlanAsync method (R-I+4)");
        }
    }

    public class R_Iplus3_InnerVoiceEventTests
    {
        [Fact]
        [Trait("Phase", "I+")]
        public void InnerVoiceEvent_Should_Inherit_AgentBusEvent()
        {
            typeof(AgentBusEvent).IsAssignableFrom(typeof(InnerVoiceEvent)).Should().BeTrue(
                "InnerVoiceEvent must inherit AgentBusEvent (R-I+3)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void InnerVoiceEvent_Should_Have_VoiceText_Field()
        {
            var field = typeof(InnerVoiceEvent).GetField("VoiceText");
            field.Should().NotBeNull("InnerVoiceEvent must have VoiceText field (R-I+3)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void InnerVoiceEvent_Should_Have_ExpiryTick_Field()
        {
            var field = typeof(InnerVoiceEvent).GetField("ExpiryTick");
            field.Should().NotBeNull("InnerVoiceEvent must have ExpiryTick field (R-I+3)");
        }
    }

    public class R_Iplus4_ReflectionEntryTests
    {
        [Fact]
        [Trait("Phase", "I+")]
        public void ReflectionEntry_Should_Be_Sealed_Record()
        {
            typeof(ReflectionEntry).IsSealed.Should().BeTrue("ReflectionEntry must be sealed record (R-I+5)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void ReflectionEntry_Should_Have_Question_Property()
        {
            var prop = typeof(ReflectionEntry).GetProperty("Question");
            prop.Should().NotBeNull("ReflectionEntry must have Question property (R-I+4)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void ReflectionEntry_Should_Have_Insight_Property()
        {
            var prop = typeof(ReflectionEntry).GetProperty("Insight");
            prop.Should().NotBeNull("ReflectionEntry must have Insight property (R-I+4)");
        }
    }

    public class R_Iplus5_DomainValueObjectNamespaceTests
    {
        [Fact]
        [Trait("Phase", "I+")]
        public void ReflectionEntry_Should_Be_In_Domain_Agent_Reflection_Namespace()
        {
            typeof(ReflectionEntry).Namespace.Should().Be(
                "RimMind.Domain.Agent.Reflection",
                "ReflectionEntry must be in Domain.Agent.Reflection namespace (R-I+5)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void ScheduleBlock_Should_Be_In_Domain_Agent_Planning_Namespace()
        {
            typeof(ScheduleBlock).Namespace.Should().Be(
                "RimMind.Domain.Agent.Planning",
                "ScheduleBlock must be in Domain.Agent.Planning namespace (R-I+5)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void ScheduleBlock_Should_Be_Sealed_Record()
        {
            typeof(ScheduleBlock).IsSealed.Should().BeTrue("ScheduleBlock must be sealed record (R-I+5)");
        }
    }

    public class R_Iplus6_AgentBusEventTypeExtensionTests
    {
        [Fact]
        [Trait("Phase", "I+")]
        public void AgentBusEventType_Should_Contain_InnerVoice()
        {
            Enum.IsDefined(typeof(AgentBusEventType), AgentBusEventType.InnerVoice).Should().BeTrue(
                "AgentBusEventType must contain InnerVoice value (R-I+6)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void AgentBusEventType_Should_Contain_Reflection()
        {
            Enum.IsDefined(typeof(AgentBusEventType), AgentBusEventType.Reflection).Should().BeTrue(
                "AgentBusEventType must contain Reflection value (R-I+6)");
        }

        [Fact]
        [Trait("Phase", "I+")]
        public void AgentBusEventType_Should_Contain_ScheduleUpdate()
        {
            Enum.IsDefined(typeof(AgentBusEventType), AgentBusEventType.ScheduleUpdate).Should().BeTrue(
                "AgentBusEventType must contain ScheduleUpdate value (R-I+6)");
        }
    }
}
