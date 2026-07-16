using System;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Features.Agent;
using RimMind.Domain.Enums;
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
            var scheduler = new AgentLoopScheduler();
            var manager = new ScopedAgentManager(new ScopedAgentFactory(), scheduler);
            var bus = new AgentBusImpl();
            var legacyManagerKey = "storyteller:x";

            var created = manager.GetOrCreate("storyteller", "x", bus, mapId: 1);
            var found = manager.Find("storyteller", "x");
            var registered = scheduler.Find(AgentLoopKeys.ForScoped(legacyManagerKey));
            var removed = manager.Remove("storyteller", "x");

            Assert.Same(created, found);
            Assert.Same(created, registered);
            Assert.True(removed);
            Assert.Null(manager.Find("storyteller", "x"));
            Assert.Null(scheduler.Find(AgentLoopKeys.ForScoped(legacyManagerKey)));
            Assert.Equal(AgentState.Terminated, created.State);
        }

        [Fact]
        public void GetOrCreate_Scope_Registers_Agent_And_Remove_Unregisters_Then_Terminates()
        {
            var scheduler = new AgentLoopScheduler();
            var manager = new ScopedAgentManager(new ScopedAgentFactory(), scheduler);
            var bus = new AgentBusImpl();
            var scope = AgentScope.Map(7);
            var loopKey = AgentLoopKeys.ForScoped(scope.CompositeKey);

            var agent = manager.GetOrCreate(scope, bus);

            Assert.Same(agent, scheduler.Find(loopKey));

            Assert.True(manager.Remove(scope));
            Assert.Null(scheduler.Find(loopKey));
            Assert.Equal(AgentState.Terminated, agent.State);
        }

        [Fact]
        public void Clear_Unregisters_And_Terminates_All_Scoped_Agents()
        {
            var scheduler = new AgentLoopScheduler();
            var manager = new ScopedAgentManager(new ScopedAgentFactory(), scheduler);
            var bus = new AgentBusImpl();
            var mapScope = AgentScope.Map(7);
            var globalScope = AgentScope.Global("colony");
            var mapAgent = manager.GetOrCreate(mapScope, bus);
            var globalAgent = manager.GetOrCreate(globalScope, bus);

            manager.Clear();

            Assert.Null(scheduler.Find(AgentLoopKeys.ForScoped(mapScope.CompositeKey)));
            Assert.Null(scheduler.Find(AgentLoopKeys.ForScoped(globalScope.CompositeKey)));
            Assert.Equal(AgentState.Terminated, mapAgent.State);
            Assert.Equal(AgentState.Terminated, globalAgent.State);
        }

        [Fact]
        public void Constructor_Rejects_Null_Dependencies()
        {
            var factory = new ScopedAgentFactory();
            var scheduler = new AgentLoopScheduler();

            Assert.Throws<ArgumentNullException>(() => new ScopedAgentManager(null!, scheduler));
            Assert.Throws<ArgumentNullException>(() => new ScopedAgentManager(factory, null!));
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
