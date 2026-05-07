using System;
using System.Collections.Generic;

namespace RimMind.Contracts.Pipeline;

public interface IPipelineContext
{
    string TraceId { get; }
    DateTime StartTimeUtc { get; }
    bool IsShortCircuited { get; }
    string? ShortCircuitReason { get; }
    IDictionary<string, object?> Items { get; }
    void ShortCircuit(string reason);
}
