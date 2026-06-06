using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP6
{
    public class P6_SubmodOrthogonalityTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadCoreSource(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static readonly string[] SubmodDirs = new[]
        {
            "RimMind-Personality", "RimMind-Dialogue", "RimMind-Storyteller",
            "RimMind-Advisor", "RimMind-Memory", "RimMind-Bridge-RimTalk",
            "RimMind-Bridge-RimChat", "RimMind-Actions",
        };

        private static IEnumerable<string> GetSubmodSourceFiles(string modDir)
        {
            var dir = Path.Combine(RepoRoot, modDir, "Source");
            if (!Directory.Exists(dir)) return Enumerable.Empty<string>();
            return Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"));
        }

        [Fact]
        public void Submods_Register_Through_Core_Extension_APIs()
        {
            var extensionPatterns = new[]
            {
                "RimMindAPI.Extensions<ISettingsTab>",
                "RimMindAPI.Extensions<IToggleBehavior>",
                "RimMindAPI.Extensions<IModCooldown>",
                "RimMindAPI.Extensions<ISkipCheck>",
                "RimMindAPI.Extensions<IDialogueTrigger>",
                "RimMindAPI.Extensions<IIncidentExecutedListener>",
            };
            var foundAny = false;
            foreach (var modDir in SubmodDirs)
            {
                foreach (var file in GetSubmodSourceFiles(modDir))
                {
                    var content = File.ReadAllText(file);
                    foreach (var pattern in extensionPatterns)
                    {
                        if (content.Contains(pattern))
                        {
                            foundAny = true;
                            break;
                        }
                    }
                }
            }
            Assert.True(foundAny, "At least one submod should register through Core extension APIs");
        }

        [Fact]
        public void Submods_Do_Not_Directly_Own_Core_UI_Windows()
        {
            var coreWindowClasses = new[]
            {
                "Window_RequestLog",
                "Window_ToolCallDebug",
                "Window_MechanismStatus",
                "Window_ContextKeyDebug",
                "Window_AgentStateDebug",
                "Window_AgentModeDebug",
                "Window_AgentFlowLab",
                "Window_AgentProgressFloat",
            };
            foreach (var modDir in SubmodDirs)
            {
                foreach (var file in GetSubmodSourceFiles(modDir))
                {
                    var content = File.ReadAllText(file);
                    foreach (var windowClass in coreWindowClasses)
                    {
                        Assert.DoesNotContain($"class {windowClass}", content);
                    }
                }
            }
        }

        [Fact]
        public void Submods_Register_ContextKeys_Through_RimMindAPI()
        {
            foreach (var modDir in SubmodDirs)
            {
                foreach (var file in GetSubmodSourceFiles(modDir))
                {
                    var content = File.ReadAllText(file);
                    if (content.Contains("new ContextProviderDef"))
                    {
                        Assert.True(content.Contains("RimMindAPI") || content.Contains("IContextKeyRegistry"),
                            $"{file} registers ContextProviderDef but does not use RimMindAPI or IContextKeyRegistry");
                    }
                }
            }
        }

        [Fact]
        public void Storyteller_Reflection_Access_To_Memory_Is_Documented()
        {
            var storytellerDir = Path.Combine(RepoRoot, "RimMind-Storyteller", "Source");
            if (!Directory.Exists(storytellerDir)) return;
            foreach (var file in Directory.GetFiles(storytellerDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                var content = File.ReadAllText(file);
                if (content.Contains("NarratorMemoryStore") && content.Contains("AccessTools"))
                {
                    Assert.True(content.Contains("try") || content.Contains("catch"),
                        $"Storyteller reflection access in {file} must be wrapped in try/catch for graceful degradation");
                }
            }
        }

        [Fact]
        public void Advisor_Uses_Core_Tools_API_Not_Direct_Registry()
        {
            var advisorDir = Path.Combine(RepoRoot, "RimMind-Advisor", "Source");
            if (!Directory.Exists(advisorDir)) return;
            foreach (var file in Directory.GetFiles(advisorDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                var content = File.ReadAllText(file);
                if (content.Contains("GetAllDefinitions"))
                {
                    Assert.Contains("RimMindAPI", content);
                }
            }
        }

        [Fact]
        public void Bridge_Submods_Use_Conditional_Registration_Pattern()
        {
            var bridgeDirs = new[] { "RimMind-Bridge-RimTalk", "RimMind-Bridge-RimChat" };
            foreach (var modDir in bridgeDirs)
            {
                foreach (var file in GetSubmodSourceFiles(modDir))
                {
                    var content = File.ReadAllText(file);
                    if (content.Contains("new ContextProviderDef"))
                    {
                        Assert.True(content.Contains("IsRimTalkActive") || content.Contains("IsRimChatActive") || content.Contains("enableContextPull"),
                            $"{file} in {modDir} must guard ContextKey registration with activation check");
                    }
                    if (content.Contains("ContextKeys.Register"))
                    {
                        Assert.True(content.Contains("Unregister"),
                            $"{file} in {modDir} must provide Unregister when using conditional registration");
                    }
                }
            }
        }

        [Fact]
        public void No_Submod_References_Another_Submods_Internal_Namespace()
        {
            var submodInternalNamespaces = new Dictionary<string, string[]>
            {
                ["RimMind-Personality"] = new[] { "RimMind.Dialogue", "RimMind.Storyteller", "RimMind.Advisor", "RimMind.Memory" },
                ["RimMind-Dialogue"] = new[] { "RimMind.Personality", "RimMind.Storyteller", "RimMind.Advisor", "RimMind.Memory" },
                ["RimMind-Storyteller"] = new[] { "RimMind.Personality", "RimMind.Dialogue", "RimMind.Advisor" },
                ["RimMind-Advisor"] = new[] { "RimMind.Personality", "RimMind.Dialogue", "RimMind.Storyteller", "RimMind.Memory" },
                ["RimMind-Memory"] = new[] { "RimMind.Personality", "RimMind.Dialogue", "RimMind.Storyteller", "RimMind.Advisor" },
            };
            foreach (var kvp in submodInternalNamespaces)
            {
                var modDir = kvp.Key;
                var forbiddenNamespaces = kvp.Value;
                foreach (var file in GetSubmodSourceFiles(modDir))
                {
                    var content = File.ReadAllText(file);
                    foreach (var ns in forbiddenNamespaces)
                    {
                        Assert.DoesNotContain($"using {ns}", content);
                    }
                }
            }
        }

        [Fact]
        public void Submods_Do_Not_Override_Core_ContextKeys()
        {
            var coreKeys = new HashSet<string>
            {
                "system_instruction", "npc_identity", "npc_commands", "world_rules",
                "npc_task_instruction", "map_structure", "pawn_base_info", "fixed_relations",
                "ideology", "skills_summary", "current_area", "weather", "time_of_day",
                "nearby_pawns", "season", "colony_status", "health", "mood", "current_job",
                "combat_status", "target_info", "task_progress"
            };
            foreach (var modDir in SubmodDirs)
            {
                foreach (var file in GetSubmodSourceFiles(modDir))
                {
                    var content = File.ReadAllText(file);
                    var idx = 0;
                    while ((idx = content.IndexOf("new ContextProviderDef(", idx)) != -1)
                    {
                        var searchFrom = idx + "new ContextProviderDef(".Length;
                        string? key = null;
                        var namedKeyIdx = content.IndexOf("key:", idx);
                        if (namedKeyIdx >= 0 && namedKeyIdx < searchFrom + 50)
                        {
                            var quoteStart = content.IndexOf('"', namedKeyIdx);
                            if (quoteStart >= 0 && quoteStart < searchFrom + 80)
                            {
                                var quoteEnd = content.IndexOf('"', quoteStart + 1);
                                if (quoteEnd >= 0) key = content.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                            }
                        }
                        if (key == null)
                        {
                            var firstQuote = content.IndexOf('"', searchFrom);
                            if (firstQuote >= 0 && firstQuote < searchFrom + 100)
                            {
                                var quoteEnd = content.IndexOf('"', firstQuote + 1);
                                if (quoteEnd >= 0) key = content.Substring(firstQuote + 1, quoteEnd - firstQuote - 1);
                            }
                        }
                        if (key != null)
                        {
                            Assert.False(coreKeys.Contains(key),
                                $"Submod {modDir} registers key '{key}' which is owned by Core");
                        }
                        idx = searchFrom;
                    }
                }
            }
        }

        [Fact]
        public void Core_IDialogueTrigger_Is_Extension_Point_Not_Submod_Type()
        {
            var interfaceContent = ReadCoreSource("Application/Common/Interfaces/Extension/IDialogueTrigger.cs");
            Assert.Contains("interface IDialogueTrigger", interfaceContent);
            Assert.Contains("IExtension", interfaceContent);

            var nullContent = ReadCoreSource("Application/Common/Defaults/NullDialogueTrigger.cs");
            Assert.Contains("NullDialogueTrigger", nullContent);
            Assert.Contains("IDialogueTrigger", nullContent);
        }

        [Fact]
        public void Core_IExtension_Provides_Registration_Surface_For_Submods()
        {
            var apiContent = ReadCoreSource("Presentation/Api/RimMindAPI.Extensions.cs");
            Assert.Contains("IExtension", apiContent);
            Assert.Contains("Register", apiContent);
        }

        [Fact]
        public void Core_ISettingsTab_Allows_Submod_Settings_Integration()
        {
            var interfaceContent = ReadCoreSource("Presentation/Settings/ISettingsTab.cs");
            Assert.Contains("ISettingsTab", interfaceContent);
            Assert.Contains("IExtension", interfaceContent);
        }

        [Fact]
        public void IScopedAgent_Inherits_IAgentControl()
        {
            var content = ReadCoreSource("Presentation/Agent/IScopedAgent.cs");
            Assert.Contains("IAgentControl", content);
            Assert.Contains("ScopeId", content);
            Assert.Contains("ScopeType", content);
            Assert.Contains("MapId", content);
        }

        [Fact]
        public void ScopedAgent_Implements_IScopedAgent()
        {
            var content = ReadCoreSource("Presentation/Agent/ScopedAgent.cs");
            Assert.Contains("class ScopedAgent : IScopedAgent", content);
            Assert.Contains("IAgentBus", content);
            Assert.Contains("AgentState", content);
            Assert.Contains("GetDebugInfo", content);
            Assert.Contains("ScopedThinkStrategy", content);
        }

        [Fact]
        public void IScopedAgentFactory_Creates_ScopedAgent()
        {
            var content = ReadCoreSource("Presentation/Agent/IScopedAgentFactory.cs");
            Assert.Contains("IScopedAgent Create", content);
            Assert.Contains("scopeType", content);
            Assert.Contains("scopeId", content);
            Assert.Contains("IAgentBus", content);
        }

        [Fact]
        public void ScopedAgentFactory_Implements_IScopedAgentFactory()
        {
            var content = ReadCoreSource("Presentation/Agent/ScopedAgentFactory.cs");
            Assert.Contains("ScopedAgentFactory : IScopedAgentFactory", content);
            Assert.Contains("new ScopedAgent", content);
        }

        [Fact]
        public void CompositionRoot_Registers_ScopedAgentFactory()
        {
            var content = ReadCoreSource("Presentation/Runtime/RimMindCompositionRoot.cs");
            Assert.Contains("IScopedAgentFactory", content);
            Assert.Contains("ScopedAgentFactory", content);
        }

        [Fact]
        public void IScopedAgentManager_Manages_ScopedAgent_Lifecycle()
        {
            var content = ReadCoreSource("Presentation/Agent/IScopedAgentManager.cs");
            Assert.Contains("GetOrCreate", content);
            Assert.Contains("Find", content);
            Assert.Contains("GetAll", content);
            Assert.Contains("Remove", content);
            Assert.Contains("Clear", content);
        }

        [Fact]
        public void ScopedAgentManager_Implements_IScopedAgentManager()
        {
            var content = ReadCoreSource("Presentation/Agent/ScopedAgentManager.cs");
            Assert.Contains("ScopedAgentManager : IScopedAgentManager", content);
            Assert.Contains("IScopedAgentFactory", content);
            Assert.Contains("GetOrCreate", content);
            Assert.Contains("CompositeKey", content);
        }

        [Fact]
        public void CompositionRoot_Registers_ScopedAgentManager()
        {
            var content = ReadCoreSource("Presentation/Runtime/RimMindCompositionRoot.cs");
            Assert.Contains("IScopedAgentManager", content);
            Assert.Contains("ScopedAgentManager", content);
        }

        [Fact]
        public void ProgressFloat_Shows_ScopedAgent_Entries()
        {
            var content = ReadCoreSource("Infrastructure/UI/Window_AgentProgressFloat.cs");
            Assert.Contains("IScopedAgentManager", content);
            Assert.Contains("scopedAgentManager.GetAll()", content);
            Assert.Contains("ScopeType", content);
            Assert.Contains("IsScopedAgent", content);
        }

        [Fact]
        public void FlowLab_Uses_ScopedAgentManager()
        {
            var content = ReadCoreSource("Infrastructure/UI/Window_AgentFlowLab.cs");
            Assert.Contains("IScopedAgentManager", content);
            Assert.Contains("GetOrCreate", content);
        }

        [Fact]
        public void AgentStateDebug_Supports_IAgentControl_Constructor()
        {
            var content = ReadCoreSource("Infrastructure/UI/Window_AgentStateDebug.cs");
            Assert.Contains("IAgentControl? _targetAgent", content);
            Assert.Contains("Window_AgentStateDebug(IAgentControl agent)", content);
            Assert.Contains("DrawScopedAgentDetail", content);
            Assert.Contains("IScopedAgent scopedAgent", content);
        }

        [Fact]
        public void AgentStateDebug_ScopedAgentDetail_Shows_Scope_Info()
        {
            var content = ReadCoreSource("Infrastructure/UI/Window_AgentStateDebug.cs");
            Assert.Contains("SectionIdentity", content);
            Assert.Contains("ScopeId", content);
            Assert.Contains("SuccessRate", content);
            Assert.Contains("RecentBehavior", content);
            Assert.Contains("DestroyScopedAgent", content);
            Assert.Contains("DrawScopedAgentButtons", content);
        }

        [Fact]
        public void ProgressFloat_Passes_AgentControl_To_Details()
        {
            var content = ReadCoreSource("Infrastructure/UI/Window_AgentProgressFloat.cs");
            Assert.Contains("IAgentControl? AgentControl", content);
            Assert.Contains("Window_AgentStateDebug(entry.AgentControl)", content);
        }

        [Fact]
        public void ScopedThinkStrategy_Implements_IThinkStrategy()
        {
            var content = ReadCoreSource("Presentation/Agent/ScopedThinkStrategy.cs");
            Assert.Contains("ScopedThinkStrategy : IThinkStrategy", content);
            Assert.Contains("ScenarioId", content);
            Assert.Contains("BuildEnvelope", content);
            Assert.Contains("ParseDecision", content);
            Assert.Contains("ThinkStrategyHelper.ParseDecisionCore", content);
            Assert.Contains("LlmRequestEnvelopeBuilder", content);
        }

        [Fact]
        public void ScopedThinkStrategy_Uses_Scope_Appropriate_ScenarioId()
        {
            var content = ReadCoreSource("Presentation/Agent/ScopedThinkStrategy.cs");
            Assert.Contains("ScenarioIds.Storyteller", content);
            Assert.Contains("ScenarioIds.Decision", content);
        }

        [Fact]
        public void ScopedAgentMode_ShouldThink_When_Perceptions_Exist()
        {
            var content = ReadCoreSource("Presentation/Agent/ScopedAgent.cs");
            Assert.Contains("perceptions.Count > 0", content);
            Assert.DoesNotContain("NoOpThinkStrategy", content);
        }

        [Fact]
        public void ScopedAgent_Has_Thinking_Pipeline()
        {
            var content = ReadCoreSource("Presentation/Agent/ScopedAgent.cs");
            Assert.Contains("private void Think()", content);
            Assert.Contains("RimMindAPI.Request.Send", content);
            Assert.Contains("ProcessPendingCallback", content);
            Assert.Contains("_thinking", content);
            Assert.Contains("_hasPendingCallback", content);
        }

        [Fact]
        public void ScopedAgent_ForceThink_Triggers_Think()
        {
            var content = ReadCoreSource("Presentation/Agent/ScopedAgent.cs");
            Assert.Contains("public void ForceThink()", content);
            Assert.Contains("_lastThinkTick = 0", content);
            Assert.Contains("Think()", content);
        }

        [Fact]
        public void ScopedAgent_Tick_Drives_Think_Cycle()
        {
            var content = ReadCoreSource("Presentation/Agent/ScopedAgent.cs");
            Assert.Contains("ThinkCooldownTicks", content);
            Assert.Contains("public void Tick()", content);
            Assert.Contains("ProcessPendingCallback()", content);
        }

        [Fact]
        public void ScopedAgent_Records_Behavior_On_Decision()
        {
            var content = ReadCoreSource("Presentation/Agent/ScopedAgent.cs");
            var processIdx = content.IndexOf("private void ProcessPendingCallback()");
            Assert.True(processIdx > 0, "ProcessPendingCallback method must exist");
            var methodSection = content.Substring(processIdx, Math.Min(500, content.Length - processIdx));
            Assert.Contains("RecordBehavior", methodSection);
            Assert.Contains("ActionIntent", methodSection);
        }

        private static int FindMatchingBrace(string content, int openBraceIndex)
        {
            int depth = 0;
            for (int i = openBraceIndex; i < content.Length; i++)
            {
                if (content[i] == '{') depth++;
                else if (content[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i + 1;
                }
            }
            return content.Length;
        }
    }
}
