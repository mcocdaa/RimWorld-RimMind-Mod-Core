using System;
using RimMind.Presentation.Runtime.Services;

namespace RimMind.Presentation.UI.Framework
{
    public sealed class GenerationUiOperation
    {
        private readonly RuntimeServiceHub _runtimeHub;
        private readonly string _eventSource;
        private bool _staleRecorded;

        public GenerationUiOperation(
            RuntimeServiceHub runtimeHub,
            RuntimeGenerationToken runtimeToken,
            string eventSource)
        {
            _runtimeHub = runtimeHub ?? throw new ArgumentNullException(nameof(runtimeHub));
            RuntimeToken = runtimeToken;
            _eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
        }

        public RuntimeGenerationToken RuntimeToken { get; }

        public bool CanPublish()
        {
            if (_runtimeHub.IsCurrent(RuntimeToken))
                return true;

            if (!_staleRecorded)
            {
                _staleRecorded = true;
                _runtimeHub.RecordStaleCompletion(_eventSource);
            }

            return false;
        }
    }
}
