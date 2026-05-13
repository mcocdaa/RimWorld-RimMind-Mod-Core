using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Common;

namespace RimMind.Application.Features.Pipeline.Bus
{
    internal sealed class ThreadAffinityCheckMiddleware : IMiddleware<BusPublishContext>
    {
        public string Name => "BusThreadAffinity";
        public int Order => 50;
        public string Id => "BusThreadAffinity";

        private readonly IThreadChecker? _threadChecker;
        private readonly ILogSink? _log;

        public ThreadAffinityCheckMiddleware(IThreadChecker? threadChecker = null, ILogSink? log = null)
        {
            _threadChecker = threadChecker;
            _log = log;
        }

        public Task InvokeAsync(BusPublishContext context, MiddlewareDelegate<BusPublishContext> next)
        {
            if (_threadChecker != null && !_threadChecker.IsMainThread)
            {
                _log?.Warning("[BusThreadAffinity] Bus publish called from non-main thread, deferring");
            }
            return next(context);
        }
    }
}
