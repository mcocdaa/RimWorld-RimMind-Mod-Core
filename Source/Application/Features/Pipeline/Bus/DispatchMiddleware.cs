using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Events;

namespace RimMind.Application.Features.Pipeline.Bus
{
    internal sealed class DispatchMiddleware : IMiddleware<BusPublishContext>
    {
        public string Name => "BusDispatch";
        public int Order => 100;
        public string Id => "BusDispatch";

        private readonly IAgentBus _bus;
        private readonly ILogSink? _log;

        public DispatchMiddleware(IAgentBus bus, ILogSink? log = null)
        {
            _bus = bus;
            _log = log;
        }

        public Task InvokeAsync(BusPublishContext context, MiddlewareDelegate<BusPublishContext> next)
        {
            _bus.Publish(context.Event);
            return next(context);
        }
    }
}
