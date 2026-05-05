using RimMind.Core.Agent;
using RimMind.Core.Comps;
using Verse;
using Verse.AI;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PawnAgentPendingJobTests
    {
        private static Pawn CreatePawn(int id = 1)
        {
            return new Pawn { thingIDNumber = id };
        }

        private static (Pawn pawn, CompPawnAgent comp, PawnAgent agent) CreateActiveAgent(int id = 1)
        {
            var pawn = CreatePawn(id);
            var agent = new PawnAgent(pawn);
            agent.TransitionTo(AgentState.Active);
            var comp = new CompPawnAgent { Agent = agent };
            pawn.AddComp(comp);
            return (pawn, comp, agent);
        }

        [Fact]
        public void ConsumePendingJob_NoJob_ReturnsNull()
        {
            var (_, _, agent) = CreateActiveAgent();
            var result = agent.ConsumePendingJob();
            Assert.Null(result);
        }

        [Fact]
        public void ConsumePendingJob_AfterSet_ReturnsJob()
        {
            var (_, _, agent) = CreateActiveAgent();
            var job = new Job();
            agent.SetPendingJob(job);
            var result = agent.ConsumePendingJob();
            Assert.Same(job, result);
        }

        [Fact]
        public void ConsumePendingJob_ClearsAfterConsume()
        {
            var (_, _, agent) = CreateActiveAgent();
            agent.SetPendingJob(new Job());
            agent.ConsumePendingJob();
            var result = agent.ConsumePendingJob();
            Assert.Null(result);
        }

        [Fact]
        public void SetPendingJob_OverwritesPrevious()
        {
            var (_, _, agent) = CreateActiveAgent();
            var job1 = new Job();
            var job2 = new Job();
            agent.SetPendingJob(job1);
            agent.SetPendingJob(job2);
            var result = agent.ConsumePendingJob();
            Assert.Same(job2, result);
        }
    }

    public class ThinkNode_RimMindAgentTests
    {
        private static Pawn CreatePawnWithComp(int id = 1)
        {
            return new Pawn { thingIDNumber = id };
        }

        [Fact]
        public void TryIssueJobPackage_NoComp_ReturnsNoJob()
        {
            var pawn = CreatePawnWithComp();
            var node = new ThinkNode_RimMindAgent();
            var result = node.TryIssueJobPackage(pawn, default);
            Assert.Null(result.Job);
        }

        [Fact]
        public void TryIssueJobPackage_CompNullAgent_ReturnsNoJob()
        {
            var pawn = CreatePawnWithComp();
            var comp = new CompPawnAgent { Agent = null };
            pawn.AddComp(comp);
            var node = new ThinkNode_RimMindAgent();
            var result = node.TryIssueJobPackage(pawn, default);
            Assert.Null(result.Job);
        }

        [Fact]
        public void TryIssueJobPackage_AgentNotActive_ReturnsNoJob()
        {
            var pawn = CreatePawnWithComp();
            var agent = new PawnAgent(pawn);
            var comp = new CompPawnAgent { Agent = agent };
            pawn.AddComp(comp);
            var node = new ThinkNode_RimMindAgent();
            var result = node.TryIssueJobPackage(pawn, default);
            Assert.Null(result.Job);
        }

        [Fact]
        public void TryIssueJobPackage_ActiveAgentNoPendingJob_ReturnsNoJob()
        {
            var pawn = CreatePawnWithComp();
            var agent = new PawnAgent(pawn);
            agent.TransitionTo(AgentState.Active);
            var comp = new CompPawnAgent { Agent = agent };
            pawn.AddComp(comp);
            var node = new ThinkNode_RimMindAgent();
            var result = node.TryIssueJobPackage(pawn, default);
            Assert.Null(result.Job);
        }

        [Fact]
        public void TryIssueJobPackage_ActiveAgentWithPendingJob_ReturnsJob()
        {
            var pawn = CreatePawnWithComp();
            var agent = new PawnAgent(pawn);
            agent.TransitionTo(AgentState.Active);
            var job = new Job();
            agent.SetPendingJob(job);
            var comp = new CompPawnAgent { Agent = agent };
            pawn.AddComp(comp);
            var node = new ThinkNode_RimMindAgent();
            var result = node.TryIssueJobPackage(pawn, default);
            Assert.Same(job, result.Job);
            Assert.Same(node, result.SourceNode);
        }

        [Fact]
        public void TryIssueJobPackage_ConsumesPendingJob()
        {
            var pawn = CreatePawnWithComp();
            var agent = new PawnAgent(pawn);
            agent.TransitionTo(AgentState.Active);
            agent.SetPendingJob(new Job());
            var comp = new CompPawnAgent { Agent = agent };
            pawn.AddComp(comp);
            var node = new ThinkNode_RimMindAgent();
            node.TryIssueJobPackage(pawn, default);
            var result2 = node.TryIssueJobPackage(pawn, default);
            Assert.Null(result2.Job);
        }

        [Fact]
        public void GetPriority_NoComp_ReturnsZero()
        {
            var pawn = CreatePawnWithComp();
            var node = new ThinkNode_RimMindAgent();
            Assert.Equal(0f, node.GetPriority(pawn));
        }

        [Fact]
        public void GetPriority_AgentNotActive_ReturnsZero()
        {
            var pawn = CreatePawnWithComp();
            var agent = new PawnAgent(pawn);
            var comp = new CompPawnAgent { Agent = agent };
            pawn.AddComp(comp);
            var node = new ThinkNode_RimMindAgent();
            Assert.Equal(0f, node.GetPriority(pawn));
        }

        [Fact]
        public void GetPriority_ActiveAgent_ReturnsConfiguredPriority()
        {
            var pawn = CreatePawnWithComp();
            var agent = new PawnAgent(pawn);
            agent.TransitionTo(AgentState.Active);
            var comp = new CompPawnAgent { Agent = agent };
            pawn.AddComp(comp);
            var node = new ThinkNode_RimMindAgent { priority = 7 };
            Assert.Equal(7f, node.GetPriority(pawn));
        }

        [Fact]
        public void GetPriority_DefaultPriority_IsFive()
        {
            var pawn = CreatePawnWithComp();
            var agent = new PawnAgent(pawn);
            agent.TransitionTo(AgentState.Active);
            var comp = new CompPawnAgent { Agent = agent };
            pawn.AddComp(comp);
            var node = new ThinkNode_RimMindAgent();
            Assert.Equal(5f, node.GetPriority(pawn));
        }

        [Fact]
        public void DeepCopy_PreservesPriority()
        {
            var node = new ThinkNode_RimMindAgent { priority = 9 };
            var copy = (ThinkNode_RimMindAgent)node.DeepCopy();
            Assert.Equal(9, copy.priority);
            Assert.NotSame(node, copy);
        }
    }
}
