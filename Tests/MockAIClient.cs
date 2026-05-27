using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RimMind.Presentation.Tests
{
    /// <summary>
    /// MockAIClient — stubbed for test compilation.
    /// Original referenced deleted Domain types (LlmRequestEnvelope, LlmResponse, RimMindError, etc.).
    /// Retained as placeholder; will be re-implemented when Domain types are restored.
    /// </summary>
    public class MockAIClient : IDisposable
    {
        public bool IsLocalEndpoint => true;
        public bool IsConfigured() => true;
        public bool SupportsStreaming => false;
        public void Dispose() { }
    }
}
