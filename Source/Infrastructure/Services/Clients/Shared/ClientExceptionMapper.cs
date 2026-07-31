using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

namespace RimMind.Infrastructure.Services.Clients.Shared
{
    /// <summary>
    /// Maps client exceptions to <see cref="RimMindError"/> results with consistent logging.
    /// Eliminates duplicate 3-catch patterns across OpenAI and Player2 clients.
    /// </summary>
    internal static class ClientExceptionMapper
    {
        /// <summary>
        /// Maps an exception to a Result error using the standard client error hierarchy:
        /// <list type="bullet">
        /// <item><see cref="TaskCanceledException"/> → <see cref="RimMindErrors.Cancelled"/></item>
        /// <item><see cref="HttpTransport.HttpException"/> → <see cref="RimMindErrors.ClientTransient"/></item>
        /// <item>Other exceptions → <see cref="RimMindErrors.Internal"/> (or ClientTransient if <paramref name="useClientTransientForGeneric"/> is true)</item>
        /// </list>
        /// </summary>
        /// <param name="ex">The caught exception.</param>
        /// <param name="clientName">Client identifier for error messages (e.g. "OpenAI", "Player2").</param>
        /// <param name="requestId">Request identifier for log correlation.</param>
        /// <param name="operationLabel">Operation label for logs (e.g. "request", "stream").</param>
        /// <param name="logSink">Optional log sink for warning messages.</param>
        /// <param name="useClientTransientForGeneric">
        /// When true, generic exceptions use <see cref="RimMindErrors.ClientTransient"/> instead of
        /// <see cref="RimMindErrors.Internal"/>. Preserves legacy Player2 streaming behavior.
        /// </param>
        public static Result<LlmResponse, RimMindError> MapException(
            Exception ex,
            string clientName,
            string requestId,
            string operationLabel,
            ILogSink? logSink,
            bool useClientTransientForGeneric = false)
        {
            string logPrefix = $"{clientName} {operationLabel}";

            if (ex is TaskCanceledException)
            {
                logSink?.LogFromBackground($"[RimMind-Core] {logPrefix} cancelled ({requestId})", isWarning: true);
                return Result<LlmResponse, RimMindError>.Err(RimMindErrors.Cancelled());
            }

            if (ex is HttpTransport.HttpException httpEx)
            {
                logSink?.LogFromBackground($"[RimMind-Core] {logPrefix} failed ({requestId}): {httpEx.Message}", isWarning: true);
                return Result<LlmResponse, RimMindError>.Err(RimMindErrors.ClientTransient(httpEx.Message, httpEx));
            }

            logSink?.LogFromBackground($"[RimMind-Core] {logPrefix} failed ({requestId}): {ex.Message}", isWarning: true);
            if (useClientTransientForGeneric)
                return Result<LlmResponse, RimMindError>.Err(RimMindErrors.ClientTransient(ex.Message, ex));
            return Result<LlmResponse, RimMindError>.Err(RimMindErrors.Internal($"{clientName} {operationLabel} failed: {ex.Message}", ex));
        }
    }
}
