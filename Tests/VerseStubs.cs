using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
    }

    public static class Mathf
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float Max(float a, float b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
    }
}

namespace Verse
{
    public struct TaggedString
    {
        public string Value;
        public static implicit operator string(TaggedString ts) => ts.Value;
        public static implicit operator TaggedString(string s) => new TaggedString { Value = s };
        public override string ToString() => Value ?? "";
    }

    public static class StringExtensions
    {
        public static TaggedString Translate(this string key) => new TaggedString { Value = key };
        public static TaggedString Translate(this string key, object arg0) => new TaggedString { Value = string.Format(key, arg0) };
        public static TaggedString Translate(this string key, object arg0, object arg1) => new TaggedString { Value = string.Format(key, arg0, arg1) };
        public static TaggedString Translate(this string key, object arg0, object arg1, object arg2) => new TaggedString { Value = string.Format(key, arg0, arg1, arg2) };
    }

    public interface IExposable
    {
        void ExposeData();
    }

    public class GameComponent : IExposable
    {
        public GameComponent() { }
        public GameComponent(Game game) { }
        public virtual void ExposeData() { }
        public virtual void FinalizeInit() { }
        public virtual void StartedNewGame() { }
        public virtual void LoadedGame() { }
        public virtual void GameComponentTick() { }
    }

    public class Game { }

    public class ModSettings : IExposable
    {
        public virtual void ExposeData() { }
    }

    public static class Scribe_Values
    {
        public static void Look<T>(ref T value, string label, T? defaultValue = default) { }
    }

    public static class Scribe_Collections
    {
        public static void Look<T>(ref List<T> list, string label, LookMode lookMode) { }
        public static void Look<T>(ref List<T> list, string label) { }
        public static void Look<TKey, TValue>(ref Dictionary<TKey, TValue> dict, string label, LookMode keyLookMode, LookMode valueLookMode) where TKey : notnull { }
    }

    public enum LookMode { Value, Deep }

    public static class Scribe_Deep
    {
        public static void Look<T>(ref T? value, string label) where T : IExposable, new() { value ??= new T(); }
    }

    public enum LoadSaveMode { Saving, LoadingVars, PostLoadInit }

    public static class Scribe
    {
        public static LoadSaveMode mode = LoadSaveMode.Saving;
    }

    public static class Log
    {
        public static Action<string> Warning = _ => { };
        public static Action<string> Message = _ => { };
        public static Action<string> Error = _ => { };
    }

    public static class UnityData
    {
        public static bool IsInMainThread = true;
    }

    public class Thing
    {
        public int thingIDNumber;
    }

    public class ThingWithComps : Thing
    {
        private List<ThingComp> _comps = new List<ThingComp>();

        public T? GetComp<T>() where T : ThingComp
        {
            foreach (var comp in _comps)
                if (comp is T t) return t;
            return null;
        }

        public T TryGetComp<T>() where T : ThingComp => GetComp<T>()!;

        internal void AddComp(ThingComp comp)
        {
            comp.parent = this;
            _comps.Add(comp);
        }
    }

    public class CompProperties
    {
        public Type? compClass;
    }

    public class Need
    {
        public float CurLevelPercentage = 1f;
    }

    public class Pawn_NeedsTracker
    {
        public Need? mood;
        public Need? food;
    }

    public class Pawn_HealthTracker
    {
        public HediffSet? hediffSet;
        public bool HasHediffsNeedingTend() => false;
        public void AddHediff(Hediff hediff) { }
    }

    public class Name
    {
        public string ToStringShort = "";
    }

    public class Pawn : ThingWithComps
    {
        public bool Dead;
        public bool IsFreeNonSlaveColonist;
        public bool DestroyedOrNull() => false;
        public Map? Map;
        public Pawn_NeedsTracker? needs;
        public Pawn_HealthTracker? health;
        public Pawn_MindState? mindState;
        public Pawn_JobTracker? jobs;
        public string LabelShortCap = "";
        public Name? Name;
        public bool InMentalState;
        private Lord? _lord;

        public Lord? GetLord() => _lord;
        public void SetLord(Lord lord) => _lord = lord;

        public bool IsHashIntervalTick(int interval)
        {
            return thingIDNumber % interval == 0;
        }
    }

    public class Pawn_JobTracker
    {
        public Verse.AI.JobQueue? jobQueue;
    }

    public class Lord
    {
        public bool AllowsFloatMenu(Pawn pawn) => true;
    }

    public static class PawnUtility
    {
        public static bool InValidState(Pawn pawn) => pawn != null && !pawn.Dead;
        public static bool WillSoonHaveBasicNeed(Pawn pawn) => false;
        public static bool PlayerForcedJobNowOrSoon(Pawn pawn) => false;
        public static bool CanCasuallyInteractNow(Pawn pawn) => true;
    }

    public class Pawn_MindState
    {
        public RimWorld.PawnDuty? duty;
    }

    public class MapPawns
    {
        public List<Pawn> AllPawns = new List<Pawn>();
        public List<Pawn> FreeColonists = new List<Pawn>();
        public List<Pawn> FreeColonistsAndPrisoners = new List<Pawn>();
    }

    public class Map
    {
        public int uniqueID;
        public MapPawns? mapPawns;
    }

    public class WorldPawns
    {
        public List<Pawn> AllPawnsAlive = new List<Pawn>();
    }

    public static class Find
    {
        public static TickManager TickManager = new TickManager();
        public static List<Map> Maps = new List<Map>();
        public static WorldPawns? WorldPawns;
        public static Storyteller? Storyteller;
        public static SignalManager? SignalManager = new SignalManager();
    }

    public class TickManager
    {
        public int TicksGame = 100000;
        public bool Paused = false;
    }

    public static class GenFilePaths
    {
        public static string SaveDataFolderPath = "/tmp/test";
    }

    public static class LongEventHandler
    {
        public static void ExecuteWhenFinished(Action action) { action(); }
    }

    public class Signal
    {
        public string tag = "";
        public SignalArgs args = new SignalArgs();
        public Signal(string tag, SignalArgs args) { this.tag = tag; this.args = args; }
    }

    public class SignalArgs
    {
        public readonly Dictionary<string, object> args = new Dictionary<string, object>();
        public void Add(string key, object value) { args[key] = value; }
        public bool TryGetArg<T>(string key, out T? value)
        {
            value = default;
            if (args.TryGetValue(key, out var obj) && obj is T t) { value = t; return true; }
            return false;
        }
    }

    public class SignalManager
    {
        public static SignalManager? Instance;
        public void Send(Signal signal) { }
        public void SendSignal(Signal signal) { }
    }

    public class Def
    {
        public string defName = "";
        public virtual IEnumerable<string> ConfigErrors() { yield break; }
    }

    public static class DefDatabase<T> where T : Def, new()
    {
        private static readonly List<T> _all = new List<T>();
        public static T? GetNamedSilentFail(string name)
        {
            foreach (var def in _all)
                if (def.defName == name) return def;
            return null;
        }
        public static List<T> AllDefsListForReading => _all;
        public static void AddDef(T def) { _all.Add(def); }
        public static void Clear() { _all.Clear(); }
    }

    public class Hediff
    {
        public RimWorld.HediffDef? def;
    }

    public class HediffSet
    {
        public Hediff? GetFirstHediffOfDef(RimWorld.HediffDef def) => null;
    }

    public class Storyteller
    {
        public Difficulty? difficulty;
    }

    public class Difficulty
    {
        public float threatScale = 1f;
    }

    public class Gizmo { }

    public class Command_Action : Gizmo
    {
        public TaggedString defaultLabel;
        public TaggedString defaultDesc;
        public Action? action;
    }

    public static class ContentFinder<T>
    {
        public static T? Get(string path, bool reportFailure = true) => default;
    }

    public class Texture2D { }

    public static class Prefs
    {
        public static bool DevMode = false;
    }

    public class Window { }

    public class Dialog_MessageBox : Window
    {
        public Dialog_MessageBox(TaggedString text, TaggedString okBtnLabel, Action? okAction = null) { }
    }

    public static class WindowStack
    {
        public static void Add(Window window) { }
    }

    public class Mod
    {
        public Mod(ModContentPack content) { }
        public T GetSettings<T>() where T : ModSettings, new() => new T();
        public virtual string SettingsCategory() => "";
        public virtual void DoSettingsWindowContents(Rect inRect) { }
    }

    public class ModContentPack { }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
    }

    public class ThingComp
    {
        public ThingWithComps? parent;
        public virtual void PostSpawnSetup(bool respawningAfterLoad) { }
        public virtual void CompTick() { }
        public virtual void PostExposeData() { }
        public virtual IEnumerable<Gizmo> CompGetGizmosExtra() { yield break; }
    }
}

namespace Verse.AI
{
    public enum JobCondition
    {
        None, Succeeded, Interrupted, InterruptedByPriority, Incompletable,
        PlayerForced, Quiet, Erased, ErasedPather, TargetLost, TargetInvalid,
        NoAvailablePath, OffMesh, ThingDespawned, ThingBlocked, PawnUnavailable, MaxRosterSize
    }

    public enum ToilCompleteMode
    {
        Never, Instant, Delay, PatherArrive, WaitForPatherToEnd, FinishLastToil
    }

    public enum TargetIndex { A = 0, B = 1, C = 2 }

    public struct LocalTargetInfo
    {
        private Thing? _thing;
        public Thing? Thing => _thing;
        public bool IsValid => _thing != null;
        public LocalTargetInfo(Thing thing) { _thing = thing; }
        public static implicit operator LocalTargetInfo(Thing thing) => new LocalTargetInfo(thing);
    }

    public class JobDef : Verse.Def
    {
        public Type? driverClass;
    }

    public struct JobTag
    {
        public static JobTag Misc = default;
        public static JobTag Fieldwork = default;
        public static JobTag SatisfyingNeeds = default;
        public static JobTag Idle = default;
    }

    public class Job
    {
        public JobDef? def;
        public ThinkNode? jobGiver;
        public int loadID;
        public LocalTargetInfo targetA;
        public LocalTargetInfo targetB;
        public int count = 1;
        public bool playerForced;
        public bool voluntary;
        public bool expireRequiresEnemyNear;
        public float targetA_thingIDNumber;
        public bool createdViaJobMaker;

        public LocalTargetInfo GetTarget(TargetIndex ind)
        {
            return ind switch
            {
                TargetIndex.A => targetA,
                TargetIndex.B => targetB,
                _ => default
            };
        }
    }

    public static class JobMaker
    {
        private static int _nextId = 1;

        public static Job MakeJob(JobDef def) => new Job { def = def, loadID = _nextId++, createdViaJobMaker = true };
        public static Job MakeJob(JobDef def, LocalTargetInfo targetA) => new Job { def = def, loadID = _nextId++, targetA = targetA, createdViaJobMaker = true };
        public static Job MakeJob(JobDef def, LocalTargetInfo targetA, LocalTargetInfo targetB) => new Job { def = def, loadID = _nextId++, targetA = targetA, targetB = targetB, createdViaJobMaker = true };
    }

    public class QueuedJob
    {
        public Job job;
        public JobTag tag;
        public QueuedJob(Job job, JobTag tag) { this.job = job; this.tag = tag; }
    }

    public class JobQueue
    {
        private readonly List<QueuedJob> _queue = new List<QueuedJob>();

        public void EnqueueFirst(Job job, JobTag tag) { _queue.Insert(0, new QueuedJob(job, tag)); }
        public void EnqueueLast(Job job, JobTag tag) { _queue.Add(new QueuedJob(job, tag)); }
        public bool AnyCanBeginNow(Verse.Pawn pawn, bool forced = false) => _queue.Count > 0;
        public QueuedJob? FirstOrDefault(Func<QueuedJob, bool> predicate)
        {
            foreach (var qj in _queue) if (predicate(qj)) return qj;
            return null;
        }
        public void Remove(QueuedJob queuedJob) { _queue.Remove(queuedJob); }
        public int Count => _queue.Count;
        public QueuedJob? Peek() => _queue.Count > 0 ? _queue[0] : null;
    }

    public class Toil
    {
        public Action? initAction;
        public Action? tickAction;
        public ToilCompleteMode defaultCompleteMode = ToilCompleteMode.Instant;
        public bool atomicWithPrevious;
        public int defaultDuration;
        public bool failOnDowned;

        public Toil WithEffect(Verse.Def effecterDef, TargetIndex index) => this;
        public Toil WithEffect(Verse.Def effecterDef, Func<Verse.Pawn, TargetIndex> index) => this;
    }

    public static class ToilMaker
    {
        public static Toil MakeToil() => new Toil();
    }

    public class JobDriver
    {
        public Verse.Pawn pawn = null!;
        public Job job = null!;
        public virtual IEnumerable<Toil> MakeNewToil() { yield break; }
        protected virtual IEnumerable<Toil> MakeNewToils() { yield break; }
        public virtual bool TryMakePreToilReservations(bool errorOnFailed) => true;
        public virtual void Notify_Starting() { }
        protected virtual void Cleanup(Verse.AI.JobCondition condition) { }
    }

    public struct JobIssueParams { }

    public class ThinkNode
    {
        public float priority = 0f;
        public virtual ThinkResult TryIssueJobPackage(Verse.Pawn pawn, JobIssueParams jobParams) => default;
        public virtual float GetPriority(Verse.Pawn pawn) => 0f;
        public virtual ThinkNode DeepCopy(bool resolve = true) => (ThinkNode)MemberwiseClone();
        public virtual ThinkNode DeepCopy(ThinkNode like) => (ThinkNode)MemberwiseClone();
    }

    public struct ThinkResult
    {
        public Job? Job;
        public ThinkNode? SourceNode;
        public bool FromQueue;

        public ThinkResult(Job job, ThinkNode sourceNode, Verse.TaggedString? tag, bool fromQueue)
        {
            Job = job; SourceNode = sourceNode; FromQueue = fromQueue;
        }

        public static ThinkResult NoJob => default;
    }
}

namespace RimWorld
{
    public class PawnDuty
    {
        public DutyDef? def;
        public PawnDuty() { }
        public PawnDuty(DutyDef def) { this.def = def; }
    }

    public class DutyDef : Verse.Def { }

    public class HediffDef : Verse.Def
    {
        public static HediffDef Named(string name) => new HediffDef { defName = name };
    }

    public class TaleDef : Verse.Def
    {
        public static TaleDef Named(string name) => new TaleDef { defName = name };
    }

    public static class HediffMaker
    {
        public static Verse.Hediff MakeHediff(HediffDef def, Verse.Pawn pawn) => new Verse.Hediff();
    }

    public static class TaleRecorder
    {
        public static void RecordTale(TaleDef taleDef, Verse.Pawn pawn) { }
    }

    public static class EffecterDefOf
    {
        public static Verse.Def? Construction;
    }
}
