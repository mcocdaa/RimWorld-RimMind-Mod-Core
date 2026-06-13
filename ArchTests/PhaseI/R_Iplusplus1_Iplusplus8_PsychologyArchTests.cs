using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Domain.Agent.Psychology;
using RimMind.Domain.Events;
using RimMind.Domain.Enums;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseI
{
    /// <summary>
    /// ArchTest rules for Phase I++6 (Psychology system).
    /// Rules R-I++1 through R-I++8 as defined in spec.
    /// Types in Domain/Application projects are tested via typeof() directly.
    /// Types in RimMindCore (net48) are tested via source file analysis
    /// because ArchTest project targets net10.0 and cannot reference Verse-dependent types.
    /// </summary>
    public class R_Iplusplus1_PsychologyInterfaceNamespaceTests
    {
        [Fact]
        [Trait("Phase", "I++")]
        public void IPsychologyWatcher_Should_Be_In_Agent_Psychology_Interface_Namespace()
        {
            typeof(IPsychologyWatcher).Namespace.Should().Be(
                "RimMind.Application.Common.Interfaces.Agent.Psychology",
                "IPsychologyWatcher must be in Application.Common.Interfaces.Agent.Psychology namespace (R-I++1)");
        }

        [Fact]
        [Trait("Phase", "I++")]
        public void IThoughtInjector_Should_Be_In_Agent_Psychology_Interface_Namespace()
        {
            typeof(IThoughtInjector).Namespace.Should().Be(
                "RimMind.Application.Common.Interfaces.Agent.Psychology",
                "IThoughtInjector must be in Application.Common.Interfaces.Agent.Psychology namespace (R-I++1)");
        }

        [Fact]
        [Trait("Phase", "I++")]
        public void IPawnPsychologyDataProvider_Should_Be_In_Agent_Psychology_Interface_Namespace()
        {
            typeof(IPawnPsychologyDataProvider).Namespace.Should().Be(
                "RimMind.Application.Common.Interfaces.Agent.Psychology",
                "IPawnPsychologyDataProvider must be in Application.Common.Interfaces.Agent.Psychology namespace (R-I++1)");
        }
    }

    /// <summary>
    /// R-I++2: DefaultPsychologyWatcher should be in RimMind.Application.Features.Agent.Psychology.
    /// Tested via source file analysis because DefaultPsychologyWatcher is internal sealed
    /// and lives in the net48 project, inaccessible from ArchTest (net10.0).
    /// </summary>
    public class R_Iplusplus2_DefaultPsychologyWatcherNamespaceTests
    {
        [Fact]
        [Trait("Phase", "I++")]
        public void DefaultPsychologyWatcher_Should_Be_In_Features_Agent_Psychology_Namespace()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var filePath = Path.Combine(sourceDir, "Application", "Features", "Agent", "Psychology", "DefaultPsychologyWatcher.cs");
            File.Exists(filePath).Should().BeTrue(
                "DefaultPsychologyWatcher.cs must exist at Application/Features/Agent/Psychology/ (R-I++2)");

            var source = File.ReadAllText(filePath);
            source.Should().Contain("namespace RimMind.Application.Features.Agent.Psychology",
                "DefaultPsychologyWatcher must be in RimMind.Application.Features.Agent.Psychology namespace (R-I++2)");
        }
    }

    /// <summary>
    /// R-I++3: VersePawnPsychologyDataProvider and VerseThoughtInjector should be in
    /// RimMind.Infrastructure.Psychology namespace.
    /// Tested via source file analysis because these types live in the net48 project
    /// and are not accessible from ArchTest (net10.0).
    /// </summary>
    public class R_Iplusplus3_InfrastructurePsychologyNamespaceTests
    {
        [Fact]
        [Trait("Phase", "I++")]
        public void VersePawnPsychologyDataProvider_Should_Be_In_Infrastructure_Psychology_Namespace()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var filePath = Path.Combine(sourceDir, "Infrastructure", "Psychology", "VersePawnPsychologyDataProvider.cs");
            File.Exists(filePath).Should().BeTrue(
                "VersePawnPsychologyDataProvider.cs must exist at Infrastructure/Psychology/ (R-I++3)");

            var source = File.ReadAllText(filePath);
            source.Should().Contain("RimMind.Infrastructure.Psychology",
                "VersePawnPsychologyDataProvider must be in RimMind.Infrastructure.Psychology namespace (R-I++3)");
        }

        [Fact]
        [Trait("Phase", "I++")]
        public void VerseThoughtInjector_Should_Be_In_Infrastructure_Psychology_Namespace()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var filePath = Path.Combine(sourceDir, "Infrastructure", "Psychology", "VerseThoughtInjector.cs");
            File.Exists(filePath).Should().BeTrue(
                "VerseThoughtInjector.cs must exist at Infrastructure/Psychology/ (R-I++3)");

            var source = File.ReadAllText(filePath);
            source.Should().Contain("RimMind.Infrastructure.Psychology",
                "VerseThoughtInjector must be in RimMind.Infrastructure.Psychology namespace (R-I++3)");
        }
    }

    public class R_Iplusplus4_DomainPsychologyNamespaceTests
    {
        [Fact]
        [Trait("Phase", "I++")]
        public void RimMindDynamicThought_Should_Be_In_Domain_Agent_Psychology_Namespace()
        {
            typeof(RimMindDynamicThought).Namespace.Should().Be(
                "RimMind.Domain.Agent.Psychology",
                "RimMindDynamicThought must be in Domain.Agent.Psychology namespace (R-I++4)");
        }

        [Fact]
        [Trait("Phase", "I++")]
        public void NeedLevel_Should_Be_In_Domain_Agent_Psychology_Namespace()
        {
            typeof(NeedLevel).Namespace.Should().Be(
                "RimMind.Domain.Agent.Psychology",
                "NeedLevel must be in Domain.Agent.Psychology namespace (R-I++4)");
        }
    }

    public class R_Iplusplus5_PsychologyEventInheritanceTests
    {
        [Fact]
        [Trait("Phase", "I++")]
        public void MoodThresholdCrossedEvent_Should_Inherit_AgentBusEvent()
        {
            typeof(AgentBusEvent).IsAssignableFrom(typeof(MoodThresholdCrossedEvent)).Should().BeTrue(
                "MoodThresholdCrossedEvent must inherit AgentBusEvent (R-I++5)");
        }

        [Fact]
        [Trait("Phase", "I++")]
        public void NeedCriticalEvent_Should_Inherit_AgentBusEvent()
        {
            typeof(AgentBusEvent).IsAssignableFrom(typeof(NeedCriticalEvent)).Should().BeTrue(
                "NeedCriticalEvent must inherit AgentBusEvent (R-I++5)");
        }

        [Fact]
        [Trait("Phase", "I++")]
        public void MentalStateWarningEvent_Should_Inherit_AgentBusEvent()
        {
            typeof(AgentBusEvent).IsAssignableFrom(typeof(MentalStateWarningEvent)).Should().BeTrue(
                "MentalStateWarningEvent must inherit AgentBusEvent (R-I++5)");
        }
    }

    /// <summary>
    /// R-I++6: PawnThinker must not directly reference VerseThoughtInjector type.
    /// It should only use IThoughtInjector interface.
    /// Tested via source file analysis because PawnThinker lives in the net48 project.
    /// </summary>
    public class R_Iplusplus6_PawnThinkerNoDirectVerseThoughtInjectorTests
    {
        [Fact]
        [Trait("Phase", "I++")]
        public void PawnThinker_Source_Should_Not_Reference_VerseThoughtInjector()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var filePath = Path.Combine(sourceDir, "Presentation", "Agent", "PawnThinker.cs");
            File.Exists(filePath).Should().BeTrue(
                "PawnThinker.cs must exist at Presentation/Agent/ (R-I++6)");

            var source = File.ReadAllText(filePath);

            Regex.Matches(source, @"VerseThoughtInjector").Should().BeEmpty(
                "PawnThinker must not directly reference VerseThoughtInjector type — " +
                "it should only use IThoughtInjector interface (R-I++6)");
        }

        [Fact]
        [Trait("Phase", "I++")]
        public void ProactiveBehaviorExecutor_Source_Should_Use_IDreamThoughtInjector_Interface()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            // After refactoring, ProactiveBehaviorExecutor uses IDreamThoughtInjector interface via constructor injection
            var executorPath = Path.Combine(sourceDir, "Presentation", "Agent", "ProactiveBehaviorExecutor.cs");

            File.Exists(executorPath).Should().BeTrue(
                "ProactiveBehaviorExecutor.cs must exist at Presentation/Agent/ (R-I++6)");

            var source = File.ReadAllText(executorPath);

            source.Should().Contain("IDreamThoughtInjector",
                "ProactiveBehaviorExecutor must use IDreamThoughtInjector interface for dream thought injection (R-I++6)");
            source.Should().NotContain("VerseDreamThoughtInjector",
                "ProactiveBehaviorExecutor must not reference concrete VerseDreamThoughtInjector — use IDreamThoughtInjector interface instead (R-I++6)");
        }
    }

    /// <summary>
    /// R-I++7: DefaultPsychologyWatcher must not reference any Verse namespace.
    /// Tested via source file analysis because DefaultPsychologyWatcher is internal sealed
    /// and lives in the net48 project, inaccessible from ArchTest (net10.0).
    /// </summary>
    public class R_Iplusplus7_DefaultPsychologyWatcherNoVerseReferenceTests
    {
        [Fact]
        [Trait("Phase", "I++")]
        public void DefaultPsychologyWatcher_Source_Should_Not_Reference_Verse_Namespace()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var filePath = Path.Combine(sourceDir, "Application", "Features", "Agent", "Psychology", "DefaultPsychologyWatcher.cs");
            File.Exists(filePath).Should().BeTrue(
                "DefaultPsychologyWatcher.cs must exist at Application/Features/Agent/Psychology/ (R-I++7)");

            var source = File.ReadAllText(filePath);

            Regex.IsMatch(source, @"using\s+Verse\s*;").Should().BeFalse(
                "DefaultPsychologyWatcher must not reference any Verse namespace (R-I++7)");
        }
    }

    /// <summary>
    /// R-I++8: VersePawnPsychologyDataProvider must implement IPawnPsychologyDataProvider interface.
    /// Tested via source file analysis because VersePawnPsychologyDataProvider lives in the net48 project.
    /// </summary>
    public class R_Iplusplus8_VersePawnPsychologyDataProviderImplementsInterfaceTests
    {
        [Fact]
        [Trait("Phase", "I++")]
        public void VersePawnPsychologyDataProvider_Source_Should_Implement_IPawnPsychologyDataProvider()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var filePath = Path.Combine(sourceDir, "Infrastructure", "Psychology", "VersePawnPsychologyDataProvider.cs");
            File.Exists(filePath).Should().BeTrue(
                "VersePawnPsychologyDataProvider.cs must exist at Infrastructure/Psychology/ (R-I++8)");

            var source = File.ReadAllText(filePath);

            source.Should().Contain("IPawnPsychologyDataProvider",
                "VersePawnPsychologyDataProvider must implement IPawnPsychologyDataProvider interface (R-I++8)");
            source.Should().MatchRegex(@"class\s+VersePawnPsychologyDataProvider\s*:\s*IPawnPsychologyDataProvider",
                "VersePawnPsychologyDataProvider class declaration must implement IPawnPsychologyDataProvider (R-I++8)");
        }
    }
}
