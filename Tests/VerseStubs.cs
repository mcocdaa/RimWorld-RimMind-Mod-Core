using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    /// <summary>Stub for Unity Texture2D used in test compilation.</summary>
    public class Texture2D { }
}

namespace RimWorld { }

namespace Verse
{
    /// <summary>Stub for RimWorld Verse.Pawn used in test compilation.</summary>
    public class Pawn : ThingWithComps
    {
        public int thingIDNumber;
        public bool Dead;

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
    }

    /// <summary>Stub for Verse.Translate extension method.</summary>
    public static class TranslateStub
    {
        public static string Translate(this string key) => key;
        public static string Translate(this string key, string arg1) => $"{key}:{arg1}";
        public static string Translate(this string key, object arg1) => $"{key}:{arg1}";
    }
}

namespace Verse.AI
{
    /// <summary>Stub for Verse.AI.Job used in test compilation.</summary>
    public class Job { }
    public class JobQueue { }
    public class Pawn_JobTracker { public JobQueue jobQueue = new JobQueue(); }
}
