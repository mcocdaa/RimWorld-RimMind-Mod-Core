using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Pipeline.AI;
using RimMind.Domain.Common;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Tools
{
    internal sealed class ToolCallDispatchMiddleware : IMiddleware<AIRequestContext>
    {
        public string Name => "ToolCallDispatch";
        public int Order => 600;
        public string Id => "ToolCallDispatch";

        private readonly IToolRegistry _toolRegistry;
        private readonly ILogSink? _log;

        public ToolCallDispatchMiddleware(IToolRegistry toolRegistry, ILogSink? log = null)
        {
            _toolRegistry = toolRegistry;
            _log = log;
        }

        public async Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            if (context.Response?.ToolCallsJson != null)
            {
                _log?.Message($"[ToolCallDispatch] Dispatching tool calls for {context.Request.RequestId}");
            }
            await next(context);
        }
    }
}
