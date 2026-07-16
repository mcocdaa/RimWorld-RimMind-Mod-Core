using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP7
{
    public class P7C_PawnAgentGizmoTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");
        private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(SourceDir, ".."));

        private static string ReadSource(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static string ReadRepo(string relativePath)
            => File.ReadAllText(Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        /// <summary>
        /// Extract the body of a method from source text by brace-depth matching.
        /// Skips braces inside string literals (both regular and interpolated).
        /// </summary>
        private static string ExtractMethodBody(string source, string methodSignature)
        {
            int methodStart = source.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.True(methodStart >= 0, $"Method signature '{methodSignature}' not found.");
            int braceStart = source.IndexOf('{', methodStart);
            Assert.True(braceStart >= 0, "Opening brace not found after method signature.");

            int depth = 1;
            int pos = braceStart + 1;
            bool inString = false;
            bool inChar = false;
            bool inVerbatim = false;

            while (pos < source.Length && depth > 0)
            {
                char c = source[pos];
                char prev = pos > 0 ? source[pos - 1] : '\0';

                if (inVerbatim)
                {
                    if (c == '"' && pos + 1 < source.Length && source[pos + 1] == '"')
                        pos++; // escaped quote in verbatim string
                    else if (c == '"')
                        inVerbatim = false;
                }
                else if (inString)
                {
                    if (c == '"' && prev != '\\')
                        inString = false;
                    else if (c == '\\' && prev != '\\')
                    { /* skip escape char */ }
                }
                else if (inChar)
                {
                    if (c == '\'' && prev != '\\')
                        inChar = false;
                }
                else
                {
                    if (c == '"' && prev == '@')
                        inVerbatim = true;
                    else if (c == '"')
                        inString = true;
                    else if (c == '\'')
                        inChar = true;
                    else if (c == '{') depth++;
                    else if (c == '}') depth--;
                }

                pos++;
            }

            return source.Substring(braceStart + 1, pos - braceStart - 2);
        }

        [Fact]
        public void CompPawnAgent_Does_Not_Load_Textures_From_Static_Initializer()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");
            int gizmoIndex = content.IndexOf("CompGetGizmosExtra", StringComparison.Ordinal);

            Assert.True(gizmoIndex >= 0, "CompGetGizmosExtra must exist.");
            string beforeGizmos = content.Substring(0, gizmoIndex);

            Assert.DoesNotContain("ContentFinder<Texture2D>.Get", beforeGizmos);
        }

        [Fact]
        public void CompPawnAgent_Uses_One_Player_Facing_Control_Gizmo()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");

            Assert.Contains("RimMind.Agent.Gizmo.Control", content);
            Assert.Contains("Window_RimMindHub.OpenAgentsForPawn(Pawn)", content);
            Assert.DoesNotContain("RimMind.Agent.Gizmo.ForceThinkDesc", content);
            Assert.DoesNotContain("RimMind.Agent.Gizmo.EmergencyStopDesc", content);
        }

        [Fact]
        public void AgentControl_Gizmo_Does_Not_Create_Agent()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");
            string methodBody = ExtractMethodBody(content, "CompGetGizmosExtra");

            // Find the action lambda for the Agent Control gizmo
            int controlKey = methodBody.IndexOf("RimMind.Agent.Gizmo.Control", StringComparison.Ordinal);
            Assert.True(controlKey >= 0, "Agent Control gizmo key must exist in method body.");

            // Extract from the Control key to the end of that yield return block
            int actionStart = methodBody.IndexOf("action =", controlKey, StringComparison.Ordinal);
            Assert.True(actionStart >= 0, "action = not found after Control key.");

            // Find the lambda body: from first { after "action =" to matching }
            int lambdaBraceStart = methodBody.IndexOf('{', actionStart);
            Assert.True(lambdaBraceStart >= 0, "Lambda opening brace not found.");

            int lambdaDepth = 1;
            int lambdaPos = lambdaBraceStart + 1;
            bool lambdaInStr = false;
            bool lambdaInChar = false;
            bool lambdaInVerb = false;
            while (lambdaPos < methodBody.Length && lambdaDepth > 0)
            {
                char lc = methodBody[lambdaPos];
                char lp = lambdaPos > 0 ? methodBody[lambdaPos - 1] : '\0';

                if (lambdaInVerb)
                {
                    if (lc == '"' && lambdaPos + 1 < methodBody.Length && methodBody[lambdaPos + 1] == '"')
                        lambdaPos++;
                    else if (lc == '"') lambdaInVerb = false;
                }
                else if (lambdaInStr)
                {
                    if (lc == '"' && lp != '\\') lambdaInStr = false;
                }
                else if (lambdaInChar)
                {
                    if (lc == '\'' && lp != '\\') lambdaInChar = false;
                }
                else
                {
                    if (lc == '"' && lp == '@') lambdaInVerb = true;
                    else if (lc == '"') lambdaInStr = true;
                    else if (lc == '\'') lambdaInChar = true;
                    else if (lc == '{') lambdaDepth++;
                    else if (lc == '}') lambdaDepth--;
                }

                lambdaPos++;
            }
            string lambdaBody = methodBody.Substring(lambdaBraceStart + 1, lambdaPos - lambdaBraceStart - 2);

            Assert.DoesNotContain("factory.Create", lambdaBody);
            Assert.DoesNotContain("Agent =", lambdaBody);
        }

        [Fact]
        public void AgentControl_Label_Is_Localized()
        {
            string en = ReadRepo("Languages/English/Keyed/RimMind_Core.xml");
            string zh = ReadRepo("Languages/ChineseSimplified/Keyed/RimMind_Core.xml");

            Assert.Contains("<RimMind.Agent.Gizmo.Control>", en);
            Assert.Contains("<RimMind.Agent.Gizmo.ControlDesc>", en);
            Assert.Contains("<RimMind.Agent.Gizmo.Control>", zh);
            Assert.Contains("<RimMind.Agent.Gizmo.ControlDesc>", zh);
        }

        [Fact]
        public void CompGetGizmosExtra_Has_Exactly_One_Player_Facing_Gizmo()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");
            string methodBody = ExtractMethodBody(content, "CompGetGizmosExtra");

            // Count yield return new Command_Action outside of Prefs.DevMode guard
            int devModeGuardIdx = methodBody.IndexOf("Prefs.DevMode", StringComparison.Ordinal);
            string playerFacingBody = devModeGuardIdx > 0
                ? methodBody.Substring(0, devModeGuardIdx)
                : methodBody;

            int yieldCount = 0;
            int idx = 0;
            while ((idx = playerFacingBody.IndexOf("yield return new Command_Action", idx, StringComparison.Ordinal)) >= 0)
            {
                yieldCount++;
                idx++;
            }

            Assert.Equal(1, yieldCount);
        }

        [Fact]
        public void DevView_Gizmo_Is_Guarded_By_DevMode_And_Agent()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");
            Assert.Contains("Prefs.DevMode && Agent != null", content);

            string methodBody = ExtractMethodBody(content, "CompGetGizmosExtra");
            int devViewIdx = methodBody.IndexOf("RimMind.Agent.Gizmo.DevView", StringComparison.Ordinal);
            Assert.True(devViewIdx >= 0, "DevView gizmo key must exist.");

            // DevView must appear after the DevMode guard
            int devModeIdx = methodBody.IndexOf("Prefs.DevMode", StringComparison.Ordinal);
            Assert.True(devModeIdx >= 0, "Prefs.DevMode guard must exist.");
            Assert.True(devViewIdx > devModeIdx, "DevView must be inside Prefs.DevMode guard.");
        }

        [Fact]
        public void PostSpawnSetup_Is_Not_Overridden()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");
            Assert.DoesNotContain("PostSpawnSetup", content);
        }

        [Fact]
        public void CompTick_RegistersWithAgentLoopInsteadOfTickingAgentDirectly()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");
            string methodBody = ExtractMethodBody(content, "CompTick");

            Assert.Contains("EnsureAgentLoopRegistration();", methodBody);
            Assert.DoesNotContain("Agent?.Tick()", methodBody);
            Assert.DoesNotContain("Agent.Tick()", methodBody);
        }

        [Fact]
        public void PostExposeData_Does_Not_AutoCreate_Agent()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");
            string methodBody = ExtractMethodBody(content, "PostExposeData");

            Assert.Contains("SerializeAgent(ref pawnAgent, \"pawnAgent\")", methodBody);
            Assert.DoesNotContain("CreateAgent()", methodBody);
        }

        [Fact]
        public void EnsureAgentCreated_Is_Public_For_DebugCenter()
        {
            string content = ReadSource("Infrastructure/Verse/CompPawnAgent.cs");
            Assert.Contains("public bool EnsureAgentCreated()", content);
        }
    }
}
