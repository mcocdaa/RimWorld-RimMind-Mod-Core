using System.Collections.Generic;
using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Bus
{
    internal static class BusPublishPipelineFactory
    {
        public static IPipeline<BusPublishContext> Create(
            IEnumerable<IMiddleware<BusPublishContext>> middlewares)
        {
            return new Pipeline<BusPublishContext>(middlewares);
        }
    }
}
