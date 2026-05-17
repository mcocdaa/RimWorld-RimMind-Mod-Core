using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Sensor;
using RimMind.Presentation.Settings;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Context;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Extension;
using Verse;

namespace RimMind.Presentation.Sensor
{
    public class SensorManager : ISensorManager
    {
        private readonly Dictionary<string, ISensorProvider> _providers = new Dictionary<string, ISensorProvider>();
        private readonly Dictionary<int, int> _lastSenseTick = new Dictionary<int, int>();

        public void RegisterProvider(ISensorProvider provider)
        {
            if (provider == null) return;
            _providers[provider.SensorId] = provider;
        }

        public void UnregisterProvider(string sensorId)
        {
            if (string.IsNullOrEmpty(sensorId)) return;
            _providers.Remove(sensorId);
        }

        public void Tick(Pawn pawn, PerceptionBuffer buffer)
        {
            if (pawn == null || buffer == null) return;
            int now = Find.TickManager.TicksGame;

            foreach (var provider in _providers.Values)
            {
                int lastTick;
                if (_lastSenseTick.TryGetValue(provider.SensorId.GetHashCode() ^ pawn.thingIDNumber, out lastTick))
                {
                    if (now - lastTick < provider.TickInterval) continue;
                }

                try
                {
                    var result = provider.Sense(pawn);
                    if (!string.IsNullOrEmpty(result))
                    {
                        buffer.Add(new Application.Common.Models.Pipeline.PerceptionBufferEntry
                        {
                            Source = provider.SensorId,
                            Content = result,
                            Priority = provider.Priority,
                            TimestampTicks = now
                        });
                    }
                    _lastSenseTick[provider.SensorId.GetHashCode() ^ pawn.thingIDNumber] = now;
                }
                catch { }
            }
        }

        public List<StructuredTool> BuildAgentTools(object pawn)
        {
            var typedPawn = pawn as Pawn;
            if (typedPawn == null) return new List<StructuredTool>();
            return GetAgentTools(typedPawn);
        }

        public List<Application.Common.Models.Client.StructuredTool> GetAgentTools(Pawn pawn)
        {
            var tools = new List<Application.Common.Models.Client.StructuredTool>();
            foreach (var provider in _providers.Values)
            {
                try
                {
                    var providerTools = provider.GetAgentTools(pawn);
                    if (providerTools != null)
                    {
                        foreach (var pt in providerTools)
                        {
                            tools.Add(new Application.Common.Models.Client.StructuredTool
                            {
                                Name = pt.Name,
                                Description = pt.Description,
                                Parameters = pt.Parameters
                            });
                        }
                    }
                }
                catch { }
            }
            return tools;
        }

        public void RegisterSensorContextKeys()
        {
            foreach (var sensor in _providers.Values)
            {
                string key = $"sensor_{sensor.SensorId}";
                var captured = sensor;
                ContextKeyRegistry.Register(key, ContextLayer.L5_Sensor, captured.Priority / 100f,
                    pawnObj =>
                    {
                        var p = pawnObj as Pawn;
                        if (p == null) return new List<ContextEntry>();
                        string? data = captured.Sense(p);
                        if (string.IsNullOrEmpty(data))
                            return new List<ContextEntry>();
                        return new List<ContextEntry>
                        {
                            new ContextEntry(data!)
                        };
                    }, "Core");
            }
        }
    }
}
