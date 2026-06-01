using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP6
{
    public class P6_CoreWindowIndependenceTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static readonly string[] SubmodNamespaces = new[]
        {
            "RimMind.Personality",
            "RimMind.Dialogue",
            "RimMind.Storyteller",
            "RimMind.Advisor",
            "RimMind.Memory",
            "RimMind.BridgeRimTalk",
            "RimMind.BridgeRimChat",
        };

        private static readonly string[] SubmodClassNames = new[]
        {
            "RimMindPersonalityMod",
            "RimMindDialogueMod",
            "RimMindStorytellerMod",
            "RimMindAdvisorMod",
            "RimMindMemoryMod",
            "PersonalityProfile",
            "DialogueState",
            "StorytellerContext",
            "AdvisorHistory",
            "WorkingMemoryProvider",
            "MemoryContextProvider",
        };

        private static readonly string[] CoreWindowFiles = new[]
        {
            "Infrastructure/UI/Window_RequestLog.cs",
            "Infrastructure/UI/Window_ToolCallDebug.cs",
            "Infrastructure/UI/Window_MechanismStatus.cs",
            "Infrastructure/UI/Window_ContextKeyDebug.cs",
            "Infrastructure/UI/Window_AgentStateDebug.cs",
            "Infrastructure/UI/Window_AgentModeDebug.cs",
            "Infrastructure/UI/Window_AgentFlowLab.cs",
            "Infrastructure/UI/Window_AgentProgressFloat.cs",
        };

        [Fact]
        public void Core_Windows_Do_Not_Import_Submod_Namespaces()
        {
            foreach (var windowFile in CoreWindowFiles)
            {
                var content = ReadSourceFile(windowFile);
                foreach (var ns in SubmodNamespaces)
                {
                    Assert.DoesNotContain($"using {ns}", content);
                }
            }
        }

        [Fact]
        public void Core_Windows_Do_Not_Reference_Submod_Class_Names()
        {
            foreach (var windowFile in CoreWindowFiles)
            {
                var content = ReadSourceFile(windowFile);
                foreach (var className in SubmodClassNames)
                {
                    Assert.DoesNotContain(className, content);
                }
            }
        }

        [Fact]
        public void Window_AgentDialogue_Uses_Only_Core_API_For_Submod_Interaction()
        {
            var content = ReadSourceFile("Infrastructure/UI/Window_AgentDialogue.cs");
            Assert.Contains("WithModId", content);
            Assert.Contains("ScenarioIds", content);
            foreach (var ns in SubmodNamespaces)
            {
                Assert.DoesNotContain($"using {ns}", content);
            }
        }

        [Fact]
        public void CoreContextProviders_Uses_ScenarioIds_Not_Submod_Namespaces()
        {
            var content = ReadSourceFile("Presentation/Context/CoreContextProviders.cs");
            Assert.Contains("ScenarioIds", content);
            foreach (var ns in SubmodNamespaces)
            {
                Assert.DoesNotContain($"using {ns}", content);
            }
        }

        [Fact]
        public void Core_Windows_Use_RimMindAPI_Not_Submod_Direct_Access()
        {
            foreach (var windowFile in CoreWindowFiles)
            {
                var content = ReadSourceFile(windowFile);
                Assert.DoesNotContain("RimMindAPI.Tools.GetAllDefinitions", content);
                Assert.DoesNotContain("RimMindAPI.ShouldSkipAction", content);
                Assert.DoesNotContain("RimMindAPI.RegisterAgentIdentityProvider", content);
            }
        }

        [Fact]
        public void Core_Windows_Use_CompPawnAgent_Not_Submod_Types()
        {
            foreach (var windowFile in CoreWindowFiles)
            {
                var content = ReadSourceFile(windowFile);
                if (content.Contains("CompPawnAgent"))
                {
                    Assert.Contains("GetComp", content);
                }
            }
        }

        [Fact]
        public void RimMindPlaySettingsPatch_Does_Not_Import_Submod_Namespaces()
        {
            var content = ReadSourceFile("Infrastructure/Patches/RimMindPlaySettingsPatch.cs");
            foreach (var ns in SubmodNamespaces)
            {
                Assert.DoesNotContain($"using {ns}", content);
            }
        }

        [Fact]
        public void AICoreDebugActions_Does_Not_Import_Submod_Namespaces()
        {
            var content = ReadSourceFile("Infrastructure/UI/AICoreDebugActions.cs");
            foreach (var ns in SubmodNamespaces)
            {
                Assert.DoesNotContain($"using {ns}", content);
            }
        }

        [Fact]
        public void Core_CompositionRoot_Does_Not_Import_Submod_Namespaces()
        {
            var content = ReadSourceFile("Presentation/Runtime/RimMindCompositionRoot.cs");
            foreach (var ns in SubmodNamespaces)
            {
                Assert.DoesNotContain($"using {ns}", content);
            }
        }

        [Fact]
        public void Core_Windows_Handle_Missing_Submod_Data_Gracefully()
        {
            var contextKeyDebug = ReadSourceFile("Infrastructure/UI/Window_ContextKeyDebug.cs");
            Assert.Contains("OwnerMod", contextKeyDebug);
            Assert.Contains("Layer", contextKeyDebug);

            var mechanismStatus = ReadSourceFile("Infrastructure/UI/Window_MechanismStatus.cs");
            Assert.Contains("OwnerModId", mechanismStatus);
        }

        [Fact]
        public void Core_Source_Does_Not_Contain_Submod_Specific_Using_Directives()
        {
            var coreSourceDir = Path.Combine(SourceDir, "Infrastructure", "UI");
            if (!Directory.Exists(coreSourceDir)) return;
            foreach (var f in Directory.GetFiles(coreSourceDir, "*.cs"))
            {
                var content = File.ReadAllText(f);
                foreach (var ns in SubmodNamespaces)
                {
                    Assert.DoesNotContain($"using {ns}", content);
                }
            }
        }
    }
}
