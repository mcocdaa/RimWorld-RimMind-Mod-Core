using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    /// <summary>Stub for Unity Texture2D used in test compilation.</summary>
    public class Texture2D { }

    /// <summary>Stub for Unity Color struct used in UI theme tests.</summary>
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new(1f, 1f, 1f, 1f);
        public static bool operator ==(Color lhs, Color rhs) =>
            System.Math.Abs(lhs.r - rhs.r) < 1e-6f && System.Math.Abs(lhs.g - rhs.g) < 1e-6f &&
            System.Math.Abs(lhs.b - rhs.b) < 1e-6f && System.Math.Abs(lhs.a - rhs.a) < 1e-6f;
        public static bool operator !=(Color lhs, Color rhs) => !(lhs == rhs);
        public override bool Equals(object? obj) => obj is Color c && this == c;
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
    }
}

namespace RimWorld
{
    public static class Messages
    {
        public static void Message(string text, MessageTypeDef def, bool historical = false) { }
    }

    public class MessageTypeDef { }
    public static class MessageTypeDefOf
    {
        public static MessageTypeDef RejectInput = new();
        public static MessageTypeDef PositiveEvent = new();
    }
}

namespace Verse
{
    /// <summary>Stub for RimWorld Verse.Pawn used in test compilation.</summary>
    public class Pawn : ThingWithComps
    {
        public int thingIDNumber;
        public bool Dead;
        public Pawn_Name Name => new Pawn_Name();
        public string LabelShort => "TestPawn";
        public object jobs;

        public T? GetComp<T>() where T : ThingComp
        {
            return _comps.OfType<T>().FirstOrDefault();
        }

        public void AddComp(ThingComp comp)
        {
            comp.parent = this;
            _comps.Add(comp);
        }

        private readonly List<ThingComp> _comps = new();
    }

    public class Pawn_Name
    {
        public string ToStringShort => "TestPawn";
    }

    /// <summary>Stub for Verse.ThingWithComps base class.</summary>
    public class ThingWithComps { }

    /// <summary>Stub for Verse.ThingComp base class.</summary>
    public class ThingComp
    {
        public ThingWithComps parent = null!;

        public virtual void PostSpawnSetup(bool respawningAfterLoad) { }
        public virtual void CompTick() { }
        public virtual void PostExposeData() { }
        public virtual IEnumerable<Gizmo> CompGetGizmosExtra() { yield break; }
    }

    /// <summary>Stub for Verse.CompProperties base class.</summary>
    public class CompProperties
    {
        public Type compClass = null!;
    }

    /// <summary>Stub for Verse.Gizmo base class.</summary>
    public class Gizmo { }

    /// <summary>Stub for Verse.Command_Action used in Gizmo tests.</summary>
    public class Command_Action : Gizmo
    {
        public string defaultLabel = "";
        public string defaultDesc = "";
        public UnityEngine.Texture2D? icon;
        public Action? action;
    }

    /// <summary>Stub for Verse.ContentFinder used in Gizmo tests.</summary>
    public static class ContentFinder<T>
    {
        public static T? Get(string path, bool reportFailure = true) => default;
    }

    /// <summary>Stub for Verse.Prefs used in Gizmo tests.</summary>
    public static class Prefs
    {
        public static bool DevMode = false;
    }

    /// <summary>Stub for Verse.Log used in Gizmo tests.</summary>
    public static class Log
    {
        public static void Message(string msg) { }
        public static void Warning(string msg) { }
        public static void Error(string msg) { }
    }

    /// <summary>Stub for Verse.BaseContent used in CompPawnAgent compilation.</summary>
    public static class BaseContent
    {
        public static UnityEngine.Texture2D BadTex = new();
    }

    /// <summary>Stub for Verse.FloatMenuOption used in Gizmo tests.</summary>
    public class FloatMenuOption
    {
        public string Label;
        public Action Action;
        public bool Disabled;

        public FloatMenuOption(string label, Action action) { Label = label; Action = action; }
    }

    /// <summary>Stub for Verse.FloatMenu used in Gizmo tests.</summary>
    public class FloatMenu : Window
    {
        public List<FloatMenuOption> Options;
        public FloatMenu(List<FloatMenuOption> options, string title = "") { Options = options; }
    }

    /// <summary>Stub for Verse.Find used in Gizmo tests.</summary>
    public static class Find
    {
        public static TickManager TickManager = new();
        public static WindowStack WindowStack = new();
    }

    /// <summary>Stub for Verse.TickManager used in Gizmo tests.</summary>
    public class TickManager
    {
        public int TicksGame { get; set; }
    }

    /// <summary>Stub for Verse.WindowStack used in Gizmo tests.</summary>
    public class WindowStack
    {
        public void Add(Window window) { }
    }

    /// <summary>Stub for Verse.Window used in Gizmo tests.</summary>
    public class Window { }

    /// <summary>Stub for Verse.Translate extension method.</summary>
    public static class TranslateStub
    {
        public static string Translate(this string key) => key;
        public static string Translate(this string key, string arg1) => $"{key}:{arg1}";
        public static string Translate(this string key, object arg1) => $"{key}:{arg1}";
    }

    /// <summary>Stub for Verse.IExposable used in test compilation.</summary>
    public interface IExposable { }

    /// <summary>Stub for Verse.LoadSaveMode used in test compilation.</summary>
    public enum LoadSaveMode
    {
        Inactive,
        Saving,
        LoadingVars,
        ResolvingCrossRefs,
        PostLoadInit
    }

    /// <summary>Stub for Verse.LookMode used in test compilation.</summary>
    public enum LookMode
    {
        Reference,
        Value,
        Deep,
        Undef
    }

    /// <summary>Stub for Verse.Scribe used in test compilation.</summary>
    public static class Scribe
    {
        public static LoadSaveMode mode = LoadSaveMode.Inactive;
    }

    /// <summary>Stub for Verse.Scribe_Values used in test compilation.</summary>
    public static class Scribe_Values
    {
        public static void Look<T>(ref T value, string label, T defaultValue = default!) { }
        public static void Look<T>(ref T value, string label, bool saveDestroyedThings) { }
    }

    /// <summary>Stub for Verse.Scribe_Collections used in test compilation.</summary>
    public static class Scribe_Collections
    {
        public static void Look<T>(ref System.Collections.Generic.List<T>? list, string label, LookMode lookMode = LookMode.Undef) where T : new() { }
        public static void Look<T>(ref System.Collections.Generic.List<T>? list, string label, bool saveDestroyedThings) where T : new() { }
    }
}

namespace Verse.AI
{
    /// <summary>Stub for Verse.AI.Job used in test compilation.</summary>
    public class Job { }
    public class JobQueue { }
    public class Pawn_JobTracker { public JobQueue jobQueue = new JobQueue(); }
}

namespace RimMind.Presentation.Agent
{
    using RimMind.Application.Common.Interfaces;
    using RimMind.Application.Common.Interfaces.Agent;
    using Verse;

    /// <summary>Stub for IPawnAgentVerse used in test compilation.</summary>
    public interface IPawnAgentVerse : IPawnAgent, IExposable
    {
        Pawn Pawn { get; }
        new Verse.AI.Job? ConsumePendingJob();
        void SetPendingJob(Verse.AI.Job job);
    }

    /// <summary>Stub for IPawnAgentFactoryVerse used in test compilation.</summary>
    public interface IPawnAgentFactoryVerse : IPawnAgentFactory
    {
        IPawnAgent Create(Pawn pawn, IAgentBus agentBus);
    }

    /// <summary>Stub for IPawnActorVerse used in test compilation.</summary>
    public interface IPawnActorVerse : IPawnActor
    {
        Verse.AI.Job? ConsumePendingJob();
        void SetPendingJob(Verse.AI.Job job);
    }
}

namespace RimMind.Presentation.Api
{
    using RimMind.Application.Common.Interfaces.Agent.Modes;
    using RimMind.Application.Common.Interfaces.Extension;

    /// <summary>Stub for RimMindAPI used in test compilation.</summary>
    public static partial class RimMindAPI
    {
        private static IExtensionRegistry<IAgentMode>? _modes;

        public static IExtensionRegistry<IAgentMode>? Modes
        {
            get => _modes;
            set => _modes = value;
        }

        public static class Request
        {
            public static void Send(RimMind.Domain.Llm.LlmRequestEnvelope envelope, System.Action<RimMind.Domain.ValueObjects.Result<RimMind.Domain.Llm.LlmResponse, RimMind.Domain.ValueObjects.RimMindError>> onComplete) { }
            public static System.Threading.Tasks.Task<RimMind.Domain.ValueObjects.Result<RimMind.Domain.Llm.LlmResponse, RimMind.Domain.ValueObjects.RimMindError>> SendAsync(RimMind.Domain.Llm.LlmRequestEnvelope envelope) => null!;
        }
    }
}

namespace RimMind.Infrastructure.UI
{
    public class Window_AgentStateDebug : global::Verse.Window
    {
        public Window_AgentStateDebug() { }
        public Window_AgentStateDebug(global::Verse.Pawn? pawn) { }
    }

    public class Window_RimMindHub : global::Verse.Window
    {
        public Window_RimMindHub() { }
        public static Window_RimMindHub OpenAgentsForPawn(global::Verse.Pawn selectedPawn) => new();
        public static Window_RimMindHub OpenAIRequests() => new();
    }
}

namespace RimMind.Tests.Stubs
{
    internal static class TestTickProvider
    {
        public static int TicksGame => 0;
    }
}
