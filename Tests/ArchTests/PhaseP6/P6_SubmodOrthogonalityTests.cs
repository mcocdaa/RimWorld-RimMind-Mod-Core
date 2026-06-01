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
    }
}
