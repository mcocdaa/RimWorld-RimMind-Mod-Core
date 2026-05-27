using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Flywheel;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    internal sealed class StubTelemetryCollector : ITelemetryCollector
    {
        public List<(string Metric, float Value, Dictionary<string, string> Tags)> Records { get; } = new();

        public void Record(string metric, float value, Dictionary<string, string>? tags = null)
        {
            Records.Add((metric, value, tags ?? new Dictionary<string, string>()));
        }

        public List<TelemetryRecord> GetRecent(int count = 100) => new List<TelemetryRecord>();
        public Dictionary<string, float> ComputeAggregates() => new Dictionary<string, float>();
        public void Clear() { }
        public void Flush() { }
        public List<TelemetryRecord> GetRecentRecords(int count = 100) => new List<TelemetryRecord>();
    }

    public class TelemetryMiddlewareTests
    {
        private static LlmRequestContext CreateContext()
        {
            return new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    NpcId = "npc-1",
                },
            };
        }

        [Fact]
        public async Task RecordsDurationOnSuccess()
        {
            var collector = new StubTelemetryCollector();
            var middleware = new TelemetryMiddleware(telemetry: collector);
            var context = CreateContext();

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse { RequestId = "req-1", Content = "ok", TokensUsed = 50 });
                return Task.CompletedTask;
            });

            Assert.Contains(collector.Records, r => r.Metric == "unified_request_duration_ms");
            Assert.Contains(collector.Records, r => r.Metric == "unified_request_success");
        }

        [Fact]
        public async Task RecordsFailureOnError()
        {
            var collector = new StubTelemetryCollector();
            var middleware = new TelemetryMiddleware(telemetry: collector);
            var context = CreateContext();

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<LlmResponse, RimMindError>.Err(RimMindErrors.ClientTransient("fail"));
                return Task.CompletedTask;
            });

            Assert.Contains(collector.Records, r => r.Metric == "unified_request_failure");
        }

        [Fact]
        public async Task RecordsShortCircuit()
        {
            var collector = new StubTelemetryCollector();
            var middleware = new TelemetryMiddleware(telemetry: collector);
            var context = CreateContext();

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.ShortCircuit("cache_hit");
                return Task.CompletedTask;
            });

            Assert.Contains(collector.Records, r => r.Metric == "unified_request_short_circuit");
        }
    }
}
