using System;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Domain.Agent.Social;
using RimMind.Domain.Events;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseI
{
    /// <summary>
    /// ArchTest rules for Phase I+++ (Social & Emergence system).
    /// Rules R-I+++1 through R-I+++8 as defined in spec.
    /// Types in Domain/Application projects are tested via typeof() directly.
    /// Types in RimMindCore (net48) are tested via source file analysis
    /// because ArchTest project targets net10.0 and cannot reference Verse-dependent types.
    /// </summary>

    // R-I+++1: Social interfaces in Application.Common.Interfaces.Agent.Social namespace
    public class R_Iplusplusplus1_SocialInterfaceNamespaceTests
    {
        [Fact]
        [Trait("Phase", "I+++")]
        public void IInformationDiffuser_Should_Be_In_Agent_Social_Interface_Namespace()
        {
            typeof(IInformationDiffuser).Namespace.Should().Be(
                "RimMind.Application.Common.Interfaces.Agent.Social",
                "IInformationDiffuser must be in Application.Common.Interfaces.Agent.Social namespace (R-I+++1)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void ISocialEventOrganizer_Should_Be_In_Agent_Social_Interface_Namespace()
        {
            typeof(ISocialEventOrganizer).Namespace.Should().Be(
                "RimMind.Application.Common.Interfaces.Agent.Social",
                "ISocialEventOrganizer must be in Application.Common.Interfaces.Agent.Social namespace (R-I+++1)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void ITraitEvolutionEngine_Should_Be_In_Agent_Social_Interface_Namespace()
        {
            typeof(ITraitEvolutionEngine).Namespace.Should().Be(
                "RimMind.Application.Common.Interfaces.Agent.Social",
                "ITraitEvolutionEngine must be in Application.Common.Interfaces.Agent.Social namespace (R-I+++1)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void IDreamGenerator_Should_Be_In_Agent_Social_Interface_Namespace()
        {
            typeof(IDreamGenerator).Namespace.Should().Be(
                "RimMind.Application.Common.Interfaces.Agent.Social",
                "IDreamGenerator must be in Application.Common.Interfaces.Agent.Social namespace (R-I+++1)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void ISleepDetector_Should_Be_In_Agent_Social_Interface_Namespace()
        {
            typeof(ISleepDetector).Namespace.Should().Be(
                "RimMind.Application.Common.Interfaces.Agent.Social",
                "ISleepDetector must be in Application.Common.Interfaces.Agent.Social namespace (R-I+++1)");
        }
    }

    // R-I+++2: Default implementations in Application.Features.Agent.Social namespace
    public class R_Iplusplusplus2_DefaultSocialNamespaceTests
    {
        private static string ReadSourceFile(string subPath)
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");
            var filePath = Path.Combine(sourceDir, subPath);
            File.Exists(filePath).Should().BeTrue($"{Path.GetFileName(filePath)} must exist (R-I+++2)");
            return File.ReadAllText(filePath);
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void DefaultInformationDiffuser_Should_Be_In_Features_Agent_Social_Namespace()
        {
            var source = ReadSourceFile(Path.Combine("Application", "Features", "Agent", "Social", "DefaultInformationDiffuser.cs"));
            source.Should().Contain("namespace RimMind.Application.Features.Agent.Social",
                "DefaultInformationDiffuser must be in RimMind.Application.Features.Agent.Social namespace (R-I+++2)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void DefaultSocialEventOrganizer_Should_Be_In_Features_Agent_Social_Namespace()
        {
            var source = ReadSourceFile(Path.Combine("Application", "Features", "Agent", "Social", "DefaultSocialEventOrganizer.cs"));
            source.Should().Contain("namespace RimMind.Application.Features.Agent.Social",
                "DefaultSocialEventOrganizer must be in RimMind.Application.Features.Agent.Social namespace (R-I+++2)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void DefaultTraitEvolutionEngine_Should_Be_In_Features_Agent_Social_Namespace()
        {
            var source = ReadSourceFile(Path.Combine("Application", "Features", "Agent", "Social", "DefaultTraitEvolutionEngine.cs"));
            source.Should().Contain("namespace RimMind.Application.Features.Agent.Social",
                "DefaultTraitEvolutionEngine must be in RimMind.Application.Features.Agent.Social namespace (R-I+++2)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void DefaultDreamGenerator_Should_Be_In_Features_Agent_Social_Namespace()
        {
            var source = ReadSourceFile(Path.Combine("Application", "Features", "Agent", "Social", "DefaultDreamGenerator.cs"));
            source.Should().Contain("namespace RimMind.Application.Features.Agent.Social",
                "DefaultDreamGenerator must be in RimMind.Application.Features.Agent.Social namespace (R-I+++2)");
        }
    }

    // R-I+++3: Infrastructure types in Infrastructure.Social namespace
    public class R_Iplusplusplus3_InfrastructureSocialNamespaceTests
    {
        private static string ReadSourceFile(string subPath)
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");
            var filePath = Path.Combine(sourceDir, subPath);
            File.Exists(filePath).Should().BeTrue($"{Path.GetFileName(filePath)} must exist (R-I+++3)");
            return File.ReadAllText(filePath);
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void VersePawnSleepDetector_Should_Be_In_Infrastructure_Social_Namespace()
        {
            var source = ReadSourceFile(Path.Combine("Infrastructure", "Social", "VersePawnSleepDetector.cs"));
            source.Should().Contain("RimMind.Infrastructure.Social",
                "VersePawnSleepDetector must be in RimMind.Infrastructure.Social namespace (R-I+++3)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void VerseTraitEvolver_Should_Be_In_Infrastructure_Social_Namespace()
        {
            var source = ReadSourceFile(Path.Combine("Infrastructure", "Social", "VerseTraitEvolver.cs"));
            source.Should().Contain("RimMind.Infrastructure.Social",
                "VerseTraitEvolver must be in RimMind.Infrastructure.Social namespace (R-I+++3)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void VerseDreamThoughtInjector_Should_Be_In_Infrastructure_Social_Namespace()
        {
            var source = ReadSourceFile(Path.Combine("Infrastructure", "Social", "VerseDreamThoughtInjector.cs"));
            source.Should().Contain("RimMind.Infrastructure.Social",
                "VerseDreamThoughtInjector must be in RimMind.Infrastructure.Social namespace (R-I+++3)");
        }
    }

    // R-I+++4: Domain value objects in Domain.Agent.Social namespace
    public class R_Iplusplusplus4_DomainSocialNamespaceTests
    {
        [Fact]
        [Trait("Phase", "I+++")]
        public void RumorEntry_Should_Be_In_Domain_Agent_Social_Namespace()
        {
            typeof(RumorEntry).Namespace.Should().Be(
                "RimMind.Domain.Agent.Social",
                "RumorEntry must be in Domain.Agent.Social namespace (R-I+++4)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void SocialEventPlan_Should_Be_In_Domain_Agent_Social_Namespace()
        {
            typeof(SocialEventPlan).Namespace.Should().Be(
                "RimMind.Domain.Agent.Social",
                "SocialEventPlan must be in Domain.Agent.Social namespace (R-I+++4)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void TraitEvolutionRecord_Should_Be_In_Domain_Agent_Social_Namespace()
        {
            typeof(TraitEvolutionRecord).Namespace.Should().Be(
                "RimMind.Domain.Agent.Social",
                "TraitEvolutionRecord must be in Domain.Agent.Social namespace (R-I+++4)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void DreamEntry_Should_Be_In_Domain_Agent_Social_Namespace()
        {
            typeof(DreamEntry).Namespace.Should().Be(
                "RimMind.Domain.Agent.Social",
                "DreamEntry must be in Domain.Agent.Social namespace (R-I+++4)");
        }
    }

    // R-I+++5: Events inherit AgentBusEvent
    public class R_Iplusplusplus5_SocialEventInheritanceTests
    {
        [Fact]
        [Trait("Phase", "I+++")]
        public void InformationDiffusionEvent_Should_Inherit_AgentBusEvent()
        {
            typeof(AgentBusEvent).IsAssignableFrom(typeof(InformationDiffusionEvent)).Should().BeTrue(
                "InformationDiffusionEvent must inherit AgentBusEvent (R-I+++5)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void SocialEventProposedEvent_Should_Inherit_AgentBusEvent()
        {
            typeof(AgentBusEvent).IsAssignableFrom(typeof(SocialEventProposedEvent)).Should().BeTrue(
                "SocialEventProposedEvent must inherit AgentBusEvent (R-I+++5)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void TraitEvolutionEvent_Should_Inherit_AgentBusEvent()
        {
            typeof(AgentBusEvent).IsAssignableFrom(typeof(TraitEvolutionEvent)).Should().BeTrue(
                "TraitEvolutionEvent must inherit AgentBusEvent (R-I+++5)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void DreamEvent_Should_Inherit_AgentBusEvent()
        {
            typeof(AgentBusEvent).IsAssignableFrom(typeof(DreamEvent)).Should().BeTrue(
                "DreamEvent must inherit AgentBusEvent (R-I+++5)");
        }
    }

    // R-I+++6: DefaultInformationDiffuser must not reference Verse namespace
    public class R_Iplusplusplus6_DefaultInformationDiffuserNoVerseTests
    {
        [Fact]
        [Trait("Phase", "I+++")]
        public void DefaultInformationDiffuser_Source_Should_Not_Reference_Verse_Namespace()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var filePath = Path.Combine(sourceDir, "Application", "Features", "Agent", "Social", "DefaultInformationDiffuser.cs");
            File.Exists(filePath).Should().BeTrue(
                "DefaultInformationDiffuser.cs must exist at Application/Features/Agent/Social/ (R-I+++6)");

            var source = File.ReadAllText(filePath);

            Regex.IsMatch(source, @"using\s+Verse\s*;").Should().BeFalse(
                "DefaultInformationDiffuser must not reference any Verse namespace (R-I+++6)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void DefaultInformationDiffuser_Source_Should_Use_System_Random()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var filePath = Path.Combine(sourceDir, "Application", "Features", "Agent", "Social", "DefaultInformationDiffuser.cs");
            File.Exists(filePath).Should().BeTrue(
                "DefaultInformationDiffuser.cs must exist at Application/Features/Agent/Social/ (R-I+++6)");

            var source = File.ReadAllText(filePath);

            source.Should().MatchRegex(@"Random\s+_random\s*=\s*new",
                "DefaultInformationDiffuser must use System.Random (via 'Random _random = new'), not Verse.Rand (R-I+++6)");
            Regex.IsMatch(source, @"Rand\.Value").Should().BeFalse(
                "DefaultInformationDiffuser must not use Verse.Rand.Value (R-I+++6)");
        }
    }

    // R-I+++7: PawnThinker must not directly instantiate VerseTraitEvolver
    // (service locator pattern is acceptable: using + field + Get<> call = 3 refs max)
    public class R_Iplusplusplus7_PawnThinkerNoDirectVerseTraitEvolverTests
    {
        [Fact]
        [Trait("Phase", "I+++")]
        public void PawnThinker_Source_Should_Not_Directly_Instantiate_VerseTraitEvolver()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var filePath = Path.Combine(sourceDir, "Presentation", "Agent", "PawnThinker.cs");
            File.Exists(filePath).Should().BeTrue(
                "PawnThinker.cs must exist at Presentation/Agent/ (R-I+++7)");

            var source = File.ReadAllText(filePath);

            // PawnThinker should not create new VerseTraitEvolver() directly
            Regex.IsMatch(source, @"new\s+VerseTraitEvolver\s*\(").Should().BeFalse(
                "PawnThinker must not directly instantiate VerseTraitEvolver — " +
                "it should use service locator (R-I+++7)");
        }

        [Fact]
        [Trait("Phase", "I+++")]
        public void PawnThinker_Or_ProactiveExecutor_Source_Should_Use_ServiceLocator_For_VerseTraitEvolver()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            // After refactoring, VerseTraitEvolver usage moved from PawnThinker to ProactiveBehaviorExecutor
            var pawnThinkerPath = Path.Combine(sourceDir, "Presentation", "Agent", "PawnThinker.cs");
            var executorPath = Path.Combine(sourceDir, "Presentation", "Agent", "ProactiveBehaviorExecutor.cs");

            var pawnThinkerSource = File.Exists(pawnThinkerPath) ? File.ReadAllText(pawnThinkerPath) : "";
            var executorSource = File.Exists(executorPath) ? File.ReadAllText(executorPath) : "";

            var combined = pawnThinkerSource + executorSource;
            combined.Should().Contain("RimMindServiceLocator.Get<VerseTraitEvolver>",
                "PawnThinker or ProactiveBehaviorExecutor should obtain VerseTraitEvolver via service locator (R-I+++7)");
        }
    }

    // R-I+++8: ProactiveAgentMode new dependencies are optional
    public class R_Iplusplusplus8_ProactiveAgentModeOptionalDepsTests
    {
        [Fact]
        [Trait("Phase", "I+++")]
        public void ProactiveAgentMode_Source_Should_Have_Optional_Social_Dependencies()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var filePath = Path.Combine(sourceDir, "Application", "Features", "Agent", "Modes", "ProactiveAgentMode.cs");
            File.Exists(filePath).Should().BeTrue(
                "ProactiveAgentMode.cs must exist at Application/Features/Agent/Modes/ (R-I+++8)");

            var source = File.ReadAllText(filePath);

            // ISocialEventOrganizer? and ITraitEvolutionEngine? should be optional (nullable)
            source.Should().MatchRegex(@"ISocialEventOrganizer\?",
                "ProactiveAgentMode must have ISocialEventOrganizer? as optional dependency (R-I+++8)");
            source.Should().MatchRegex(@"ITraitEvolutionEngine\?",
                "ProactiveAgentMode must have ITraitEvolutionEngine? as optional dependency (R-I+++8)");
        }
    }
}
