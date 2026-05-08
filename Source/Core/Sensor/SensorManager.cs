using System;
using RimMind.Contracts.Sensor;
using System.Collections.Generic;
using System.Linq;
using RimMind.Kernel.Context;
using RimMind.Contracts.Client;
using RimMind.Contracts.Extensions;
using RimMind.Contracts.Internal;
using RimMind.Core.Runtime;
using Verse;

namespace RimMind.Core.Sensor
{
    /// <summary>
    /// SensorManager: Central coordinator for all Sensor providers.
    /// Handles timed polling, Agent Tool aggregation, and multi-source fusion.
    /// </summary>
    public class SensorManager : GameComponent, ISensorManager
    {
        public static ISensorManager? Instance
        {
            get => RimMindServiceLocator.Get<ISensorManager>();
            private set
            {
                if (value != null)
                    RimMindServiceLocator.Register<ISensorManager>(value);
            }
        }

        private int _lastPollTick;

        public SensorManager(Game game)
        {
            Instance = this;
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Instance = this;
            RegisterSensorContextKeys();
        }

        public void RegisterSensorContextKeys()
        {
            foreach (var sensor in RimMindRuntime.Instance.SensorProvidersList)
            {
                string key = $"sensor_{sensor.SensorId}";
                var captured = sensor;
                ContextKeyRegistry.Register(key, ContextLayer.L5_Sensor, captured.Priority / 100f,
                    pawnObj =>
                    {
                        var pawn = pawnObj as Pawn;
                        if (pawn == null) return new List<RimMind.Contracts.Context.ContextEntry>();
                        string? data = captured.Sense(pawn);
                        if (string.IsNullOrEmpty(data))
                            return new List<RimMind.Contracts.Context.ContextEntry>();
                        return new List<RimMind.Contracts.Context.ContextEntry> { new RimMind.Contracts.Context.ContextEntry(data!) };
                    }, "Core");
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (!Find.TickManager.Paused && Find.TickManager.TicksGame % 150 != 0) return;

            foreach (var sensor in RimMindRuntime.Instance.SensorProvidersList.ToArray())
            {
                if (sensor.TickInterval <= 0) continue;
                if (Find.TickManager.TicksGame % sensor.TickInterval != 0) continue;

                try
                {
                    foreach (var map in Find.Maps)
                    {
                        foreach (var pawn in map.mapPawns.FreeColonists)
                        {
                            string? data = sensor.Sense(pawn);
                            if (!string.IsNullOrEmpty(data))
                                global::RimMind.Core.Perception.PerceptionBridge.PublishPerception(
                                    pawn.thingIDNumber, sensor.SensorId, data!,
                                    sensor.Priority / 100f, RimMindRuntime.Instance.EventBus);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimMind-Core] Sensor '{sensor.SensorId}' tick error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Build Agent Tools list from all registered sensors for the given pawn.
        /// Converts AgentToolDefinition to StructuredTool for AI tool calling.
        /// </summary>
        public List<StructuredTool> BuildAgentTools(object pawn)
        {
            var typedPawn = pawn as Pawn;
            if (typedPawn == null) return new List<StructuredTool>();
            var tools = new List<StructuredTool>();
            foreach (var sensor in RimMindRuntime.Instance.SensorProvidersList)
            {
                try
                {
                    var defs = sensor.GetAgentTools(typedPawn);
                    foreach (var def in defs)
                    {
                        tools.Add(new StructuredTool
                        {
                            Name = def.Name,
                            Description = def.Description,
                            Parameters = def.Parameters
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimMind-Core] Sensor '{sensor.SensorId}' GetAgentTools error: {ex.Message}");
                }
            }
            return tools;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _lastPollTick, "lastPollTick");
        }
    }
}
