using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Events;

namespace RimMind.Application.Features.Pipeline.Bus
{
    internal sealed class BusPublishContext : PipelineContextBase
    {
        public AgentBusEvent Event { get; }

        public BusPublishContext(AgentBusEvent evt, string? traceId = null, System.Threading.CancellationToken ct = default)
            : base(traceId, ct)
        {
            Event = evt;
        }
    }
}
