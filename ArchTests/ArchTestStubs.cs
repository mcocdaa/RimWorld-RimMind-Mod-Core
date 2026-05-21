using System;
using System.Collections.Generic;
using Verse;

namespace RimMind.Application.Common.Models.Npc
{
    public class NpcProfile
    {
        public string NpcId = "";
        public int PawnId;
        public string Name = "";
        public string ShortName = "";
        public string DisplayName = "";
    }

    public interface INpcManager
    {
        void SpawnNpc(NpcProfile profile);
        void KillNpc(string npcId);
        bool IsNpcAlive(string npcId);
        NpcProfile? GetNpc(string npcId);
        IReadOnlyList<NpcProfile> GetAllNpcs();
        string GetNpcForMap(Map map);
        Pawn? FindPawnByNpcId(string npcId);
        Pawn? FindProxyPawnForMap(Map map);
        void RegisterActiveAgent(int thingId);
        void UnregisterActiveAgent(int thingId);
        HashSet<int> GetActiveAgentPawnIds();
        void IndexPawn(Pawn pawn);
        void UnindexPawn(int thingId);
        string GetMapNpcId(Map map);
    }

    public class NpcManager : GameComponent, INpcManager
    {
        public static INpcManager? Instance => RimMindServiceLocator.Get<INpcManager>();
        public NpcManager() : base() { }
        public NpcManager(Game game) : base() { }
        public void SpawnNpc(NpcProfile profile) { }
        public void KillNpc(string npcId) { }
        public bool IsNpcAlive(string npcId) => false;
        public NpcProfile? GetNpc(string npcId) => null;
        public IReadOnlyList<NpcProfile> GetAllNpcs() => Array.Empty<NpcProfile>();
        public string GetNpcForMap(Map map) => "";
        public Pawn? FindPawnByNpcId(string npcId) => null;
        public Pawn? FindProxyPawnForMap(Map map) => null;
        public void RegisterActiveAgent(int thingId) { }
        public void UnregisterActiveAgent(int thingId) { }
        public HashSet<int> GetActiveAgentPawnIds() => new HashSet<int>();
        public void IndexPawn(Pawn pawn) { }
        public void UnindexPawn(int thingId) { }
        public string GetMapNpcId(Map map) => "";
    }

    internal static class TransientExceptionChecker
    {
        public static bool IsTransient(Exception ex) => ex is TimeoutException;
    }
}

namespace RimMind.Presentation.Agent
{
    using RimMind.Domain.Events;

    public class PawnAgent
    {
        private readonly IAgentBus _agentBus;

        public PawnAgent(IAgentBus agentBus)
        {
            _agentBus = agentBus;
        }
    }
}

namespace RimMind.Domain.Events
{
    public interface IAgentBus
    {
        void Publish<T>(T evt) where T : notnull;
        void Subscribe<T>(Action<T> handler) where T : notnull;
    }
}

namespace RimMind.Application.Common.Models.Client
{
    public enum AIRequestPriority { Low, Normal, High, Critical }

    public enum AIRequestState { Queued, Processing, Completed, Error, Cancelled }

    public class StructuredTool
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public static class HttpTransport
    {
        public class HttpException : Exception
        {
            public int StatusCode { get; }
            public HttpException(int statusCode, string message) : base(message) { StatusCode = statusCode; }
        }
    }

    public class MessageDto
    {
        public string role = "";
        public string content = "";
    }

    public class Player2MessageDto
    {
        public string Role = "";
        public string Content = "";
    }
}

namespace RimMind.Application.Common.Models.UI
{
    public class RequestEntry
    {
        public string RequestId { get; set; } = "";
        public string ModId { get; set; } = "";
        public string Status { get; set; } = "";
        public int TicksQueued { get; set; }
    }
}

namespace RimMind.Presentation.Agent
{
    public class PerceptionPipeline { }
}

namespace RimMind.Application.Common.Models.UI
{
    public interface IAudioPlayer
    {
        void PlayNotification();
        void PlayError();
    }
}

namespace RimMind.Application.Common.Interfaces.Sensor
{
    using RimMind.Application.Common.Models.Client;

    public interface ISensorManager
    {
        List<StructuredTool> BuildAgentTools(Verse.Pawn pawn);
        void RegisterSensorContextKeys();
    }

    public class SensorManager : Verse.GameComponent, ISensorManager
    {
        public static ISensorManager? Instance => RimMindServiceLocator.Get<ISensorManager>();
        public SensorManager() : base() { }
        public SensorManager(Verse.Game game) : base() { }
        public List<StructuredTool> BuildAgentTools(Verse.Pawn pawn) => new();
        public void RegisterSensorContextKeys() { }
    }
}

namespace RimMind.Application.Common.Interfaces.Internal
{
    internal static class RimMindServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static T? Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out var svc))
                return (T)svc;
            return default;
        }

        public static void Register<T>(T service) where T : notnull
        {
            _services[typeof(T)] = service;
        }
    }
}
