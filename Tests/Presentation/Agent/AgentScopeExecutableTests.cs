using RimMind.Application.Common.Models.Agent;
using RimMind.Presentation.Agent;
using Xunit;

namespace RimMind.Tests.Presentation.Agent
{
    public class AgentScopeExecutableTests
    {
        [Fact]
        public void AgentScope_Pawn_CompositeKey_Differs_By_MapId()
        {
            var firstMapScope = AgentScope.Pawn("pawn-42", mapId: 1);
            var secondMapScope = AgentScope.Pawn("pawn-42", mapId: 2);

            Assert.NotEqual(firstMapScope.CompositeKey, secondMapScope.CompositeKey);
        }

        [Fact]
        public void Legacy_Manager_Overloads_Find_And_Remove_Agent_Created_With_MapId()
        {
            var manager = new ScopedAgentManager(new ScopedAgentFactory());
            var bus = new AgentBusImpl();

            var created = manager.GetOrCreate("storyteller", "x", bus, mapId: 1);
            var found = manager.Find("storyteller", "x");
            var removed = manager.Remove("storyteller", "x");

            Assert.Same(created, found);
            Assert.True(removed);
            Assert.Null(manager.Find("storyteller", "x"));
        }

        [Fact]
        public void Legacy_Created_ScopedAgent_Preserves_Caller_Provided_ScopeType()
        {
            var factory = new ScopedAgentFactory();
            var bus = new AgentBusImpl();

            var agent = factory.Create("storyteller", "x", bus, mapId: 1);

            Assert.Equal("storyteller", agent.ScopeType);
            Assert.Equal("x", agent.ScopeId);
            Assert.Equal(1, agent.MapId);
        }
    }
}
