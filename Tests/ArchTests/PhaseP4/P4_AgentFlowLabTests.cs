using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_AgentFlowLabTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private const string FlowLabRelative = "Infrastructure/UI/Window_AgentFlowLab.cs";
        private const string AsyncCoordinatorRelative = "Infrastructure/UI/AgentFlow/AgentFlowAsyncCoordinator.cs";

        [Fact]
        public void FlowLab_Has_FlowLabStep_Enum()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("enum FlowLabStep", content);
            Assert.Contains("SelectTarget", content);
            Assert.Contains("CreateAgent", content);
            Assert.Contains("BuildContext", content);
            Assert.Contains("SendRequest", content);
            Assert.Contains("ParseDecision", content);
            Assert.Contains("MapMechanism", content);
            Assert.Contains("DryRun", content);
            Assert.Contains("Execute", content);
        }

        [Fact]
        public void FlowLab_Has_StepStatus_Enum()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("enum StepStatus", content);
            Assert.Contains("Pending", content);
            Assert.Contains("Active", content);
            Assert.Contains("Completed", content);
            Assert.Contains("Failed", content);
        }

        [Fact]
        public void FlowLab_Has_StepStatus_Tracking()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("_stepStatuses", content);
            Assert.Contains("SetStepStatus", content);
            Assert.Contains("ResetStepStatuses", content);
        }

        [Fact]
        public void FlowLab_Has_StepStatus_Visual_Indicators()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("StepStatusColor", content);
            Assert.Contains("StepStatusSymbol", content);
            Assert.Contains("DrawStepHeader", content);
        }

        [Fact]
        public void FlowLab_Has_Offline_Mode_Toggle()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("_offlineMode", content);
            Assert.Contains("DrawOfflineModeToggle", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.OfflineMode", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.LiveMode", content);
        }

        [Fact]
        public void FlowLab_Offline_Mode_Defaults_To_True()
        {
            var content = ReadSourceFile(FlowLabRelative);
            var offlineModeLine = content.IndexOf("_offlineMode = true");
            Assert.True(offlineModeLine > 0, "_offlineMode must default to true");
        }

        [Fact]
        public void FlowLab_Has_Offline_Stub_Response()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("HandleOfflineRequest", content);
            Assert.Contains("stub", content);
            Assert.Contains("AgentDecision", content);
        }

        [Fact]
        public void FlowLab_Has_Live_Request_Handler()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("HandleLiveRequest", content);
            Assert.Contains("ISettingsProvider", content);
            Assert.Contains("IsConfigured", content);
        }

        [Fact]
        public void FlowLab_Offline_Mode_Displays_Stub_Indicator()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("RimMind.UI.AgentFlowLab.OfflineStubUsed", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.OfflineHint", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.LiveHint", content);
        }

        [Fact]
        public void FlowLab_DryRun_Is_Default_Before_Execute()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("_dryRunCompleted", content);
            Assert.Contains("ExecuteRequiresDryRun", content);
            Assert.Contains("!_dryRunCompleted", content);
        }

        [Fact]
        public void FlowLab_Has_Auto_Dry_Run_After_Request()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("AutoDryRun", content);
            Assert.Contains("PerformDryRun", content);
        }

        [Fact]
        public void FlowLab_DryRun_Shows_Risk_And_Approval()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("IHumanApprovalGate", content);
            Assert.Contains("RequiresApproval", content);
            Assert.Contains("APPROVAL REQUIRED", content);
            Assert.Contains("MechanismRisk.Dangerous", content);
        }

        [Fact]
        public void FlowLab_DryRun_Shows_Tool_Mapping()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("IToolRegistry", content);
            Assert.Contains("FindById", content);
            Assert.Contains("no tool mapping", content);
        }

        [Fact]
        public void FlowLab_DryRun_Uses_DecisionMapper()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("DecisionMapper.ToWriteArgs", content);
            Assert.Contains("DecisionMapper.InferOperationType", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.DryRunDecision", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.DryRunTarget", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.DryRunNoTarget", content);
        }

        [Fact]
        public void FlowLab_Has_Decision_Validation()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("IDecisionValidator", content);
            Assert.Contains("ValidationResult", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.ValidationPassed", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.ValidationFailed", content);
        }

        [Fact]
        public void FlowLab_Has_Parsed_Decision_Display()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("_parsedDecisionInfo", content);
            Assert.Contains("FormatDecision", content);
            Assert.Contains("ActionIntent", content);
        }

        [Fact]
        public void FlowLab_Has_Open_Logs_Navigation()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("DrawOpenLogs", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.OpenLogs", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.OpenRequestLog", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.OpenToolCallDebug", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.OpenMechanismStatus", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.OpenContextKeys", content);
        }

        [Fact]
        public void FlowLab_Open_Logs_Opens_Debug_Windows()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("new Window_RequestLog()", content);
            Assert.Contains("new Window_ToolCallDebug()", content);
            Assert.Contains("new Window_MechanismStatus()", content);
            Assert.Contains("new Window_ContextKeyDebug()", content);
        }

        [Fact]
        public void FlowLab_Execute_Requires_Confirmation()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("Dialog_MessageBox", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.ConfirmExecute", content);
        }

        [Fact]
        public void FlowLab_Step_Status_Updates_On_Create_Agent()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Active)", content);
            Assert.Contains("SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Completed)", content);
            Assert.Contains("SetStepStatus(FlowLabStep.CreateAgent, StepStatus.Failed)", content);
        }

        [Fact]
        public void FlowLab_Step_Status_Updates_On_Build_Context()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("SetStepStatus(FlowLabStep.BuildContext, StepStatus.Active)", content);
            Assert.Contains("FlowLabStep.BuildContext", content);
            Assert.Contains("StepStatus.Completed", content);
            Assert.Contains("StepStatus.Failed", content);
            Assert.Contains("_lastSnapshot", content);
        }

        [Fact]
        public void FlowLab_Reset_Clears_All_Step_Statuses()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("ResetStepStatuses", content);
            Assert.Contains("_dryRunCompleted = false", content);
            Assert.Contains("_lastDecision = null", content);
        }

        [Fact]
        public void FlowLab_Constructor_Seeds_SelectedPawn_From_ProvidedPawn()
        {
            var content = ReadSourceFile(FlowLabRelative);

            Assert.Contains("_initialPawn = pawn;", content);
            Assert.Contains("_selectedPawn = pawn;", content);
        }

        [Fact]
        public void FlowLab_LiveRequest_Uses_ThinkStrategyHelper_ParseDecisionCore()
        {
            var content = ReadSourceFile(FlowLabRelative);

            Assert.Contains("ThinkStrategyHelper.ParseDecisionCore", content);
            Assert.DoesNotContain("ActionIntent: \"parsed_from_response\"", content);
            Assert.Contains("new LlmResponse", content);
        }

        [Fact]
        public void FlowLab_LiveRequest_Asks_For_Actionable_Action_Block()
        {
            var content = ReadSourceFile(FlowLabRelative);

            Assert.Contains("<Action>", content);
            Assert.Contains("pawn.job.force_rest", content);
            Assert.DoesNotContain("Reply: {\\\"status\\\":\\\"ok\\\"}", content);
        }

        [Fact]
        public void FlowLab_Execute_Uses_Last_DryRun_WriteArgs()
        {
            var content = ReadSourceFile(FlowLabRelative);

            Assert.Contains("private MechanismWriteArgs? _lastWriteArgs;", content);
            Assert.Contains("private MechanismOperationType _lastOperationType;", content);
            Assert.Contains("_lastWriteArgs = writeArgs;", content);
            Assert.Contains("mechanismRegistry.FindById(_lastWriteArgs.MechanismId)", content);
            Assert.Contains("ExecuteMappedMechanism", content);
            Assert.DoesNotContain("MechanismId = firstMech.MechanismId", content);
        }

        [Fact]
        public void FlowLab_DryRun_Does_Not_Enable_Execute_Without_Target_Mechanism()
        {
            var content = ReadSourceFile(FlowLabRelative);
            int noTargetIndex = content.IndexOf("RimMind.UI.AgentFlowLab.DryRunNoTarget", StringComparison.Ordinal);

            Assert.True(noTargetIndex >= 0, "Dry run must report no-target mappings.");

            string noTargetBranch = content.Substring(noTargetIndex, Math.Min(900, content.Length - noTargetIndex));
            Assert.Contains("_dryRunResult = sb.ToString();", noTargetBranch);
            Assert.Contains("_lastError = noTarget;", noTargetBranch);
            Assert.Contains("_lastWriteArgs = null;", noTargetBranch);
            Assert.Contains("_lastOperationType = MechanismOperationType.Set;", noTargetBranch);
            Assert.Contains("SetStepStatus(FlowLabStep.DryRun, StepStatus.Failed)", noTargetBranch);
            Assert.Contains("SetStepStatus(FlowLabStep.MapMechanism, StepStatus.Failed)", noTargetBranch);
            Assert.Contains("return;", noTargetBranch);
        }

        [Fact]
        public void FlowLab_DryRun_Does_Not_Enable_Execute_Without_Decision()
        {
            var content = ReadSourceFile(FlowLabRelative);

            Assert.Contains("if (_lastDecision == null)", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.DryRunNoDecision", content);
            Assert.Contains("_lastWriteArgs = null;", content);
            Assert.Contains("_dryRunCompleted = false;", content);
        }

        [Fact]
        public void FlowLab_Execute_Callback_Fails_When_WriteArgs_Are_Missing()
        {
            var content = ReadSourceFile(FlowLabRelative);

            Assert.Contains("if (_lastWriteArgs == null)", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.ExecuteNoWriteArgs", content);
            Assert.Contains("SetStepStatus(FlowLabStep.Execute, StepStatus.Failed)", content);
        }

        [Fact]
        public void FlowLab_Exposes_Generic_NonPawn_Scopes_With_ScopedAgent()
        {
            var content = ReadSourceFile(FlowLabRelative);

            Assert.Contains("private enum AgentFlowScope", content);
            Assert.Contains("AgentFlowScope.Map", content);
            Assert.Contains("AgentFlowScope.Colony", content);
            Assert.Contains("AgentFlowScope.Global", content);
            Assert.DoesNotContain("AgentFlowScope.Storyteller", content);
            Assert.Contains("IScopedAgent", content);
            Assert.Contains("IScopedAgentManager", content);
            Assert.Contains("DrawNonPawnScope", content);
            Assert.Contains("ResolveScopeId", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.ScopedAgentActive", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.ScopeHint", content);
        }

        [Fact]
        public void FlowLab_Scope_Switch_Clears_ScopedAgent()
        {
            var content = ReadSourceFile(FlowLabRelative);

            int switchIdx = content.IndexOf("_selectedScope = scope;");
            Assert.True(switchIdx > 0, "Scope switch must exist");
            string switchBlock = content.Substring(switchIdx - 50, 200);
            Assert.Contains("_scopedAgent = null", switchBlock);
            Assert.Contains("_agent = null", switchBlock);
        }

        [Fact]
        public void FlowLab_Uses_Async_Coordinator_For_Context_And_Confirmed_Execution()
        {
            var flowLab = ReadSourceFile(FlowLabRelative);
            var coordinator = ReadSourceFile(AsyncCoordinatorRelative);

            Assert.Contains("AgentFlowAsyncCoordinator", flowLab);
            Assert.Contains("BeginContextBuild", coordinator);
            Assert.Contains("PollContextBuild", coordinator);
            Assert.Contains("BeginMechanismExecution", coordinator);
            Assert.Contains("PollMechanismExecution", coordinator);
            Assert.DoesNotContain("ExecuteMappedMechanism(targetMech, _lastWriteArgs, _lastOperationType).Result", flowLab);
        }

        [Fact]
        public void FlowLab_Discards_Execution_Completion_For_A_Stale_Target_Generation()
        {
            var content = ReadSourceFile(FlowLabRelative);

            Assert.Contains("private int _targetGeneration;", content);
            Assert.Contains("InvalidateCurrentTarget();", content);
            Assert.Contains("new AgentFlowExecutionContext(", content);
            Assert.Contains("HasPendingMechanismExecutionForGeneration(_targetGeneration)", content);
            Assert.Contains("execution.Context.TargetGeneration != _targetGeneration", content);
            Assert.Contains("result ignored for stale", content);
        }

    }
}
