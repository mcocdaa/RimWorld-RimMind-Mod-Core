using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Helpers;
using RimMind.Domain.Common;
using RimMind.Domain.ValueObjects;
using Newtonsoft.Json;

namespace RimMind.Infrastructure.Services.Clients.Player2
{
    public partial class Player2Client : IAIClient
    {
        private const float LocalConnectTimeout = 300f;
        private const float RemoteConnectTimeout = 60f;

        public const string GameClientId = "019a8368-b00b-72bc-b367-2825079dc6fb";
        private static string LocalUrl => $"http://localhost:{RimMindDefaults.Player2LocalPort}";

        private readonly string _apiKey;
        private readonly bool _isLocalConnection;
        private readonly ISettingsProvider _settings;
        private readonly ILogSink? _logSink;
        private readonly IAIDebugLog? _aiDebugLog;

        private string RemoteUrl => string.IsNullOrWhiteSpace(_settings.Player2RemoteUrl)
            ? "https://api.player2.game"
            : _settings.Player2RemoteUrl.Trim().TrimEnd('/');

        private string CurrentApiUrl => _isLocalConnection ? LocalUrl : RemoteUrl;

        private Player2Client(string apiKey, bool isLocal, ISettingsProvider settings,
            ILogSink? logSink, IAIDebugLog? aiDebugLog)
        {
            _apiKey = apiKey;
            _isLocalConnection = isLocal;
            _settings = settings;
            _logSink = logSink;
            _aiDebugLog = aiDebugLog;

            InitiateHealthCheckIfNeeded();
        }

        public static async Task<Player2Client> CreateAsync(ISettingsProvider settings,
            ILogSink? logSink = null, IAIDebugLog? aiDebugLog = null)
        {
            try
            {
                string? localKey = await TryGetLocalPlayer2Key(logSink);
                if (!string.IsNullOrEmpty(localKey))
                {
                    logSink?.LogFromBackground("[RimMind-Core] Player2 local app detected.");
                    ShowNotification("RimMind.Infrastructure.Player2.LocalDetected");
                    return new Player2Client(localKey!, isLocal: true, settings, logSink, aiDebugLog);
                }

                if (!string.IsNullOrEmpty(settings.ApiKey))
                {
                    logSink?.LogFromBackground("[RimMind-Core] Using manual Player2 API key.");
                    return new Player2Client(settings.ApiKey, isLocal: false, settings, logSink, aiDebugLog);
                }

                ShowNotification("RimMind.Infrastructure.Player2.LocalNotFound");
                return new Player2Client(string.Empty, isLocal: false, settings, logSink, aiDebugLog);
            }
            catch (Exception ex)
            {
                logSink?.LogFromBackground($"[RimMind-Core] Failed to create Player2 client: {ex.Message}", isWarning: true);
                return new Player2Client(string.Empty, isLocal: false, settings, logSink, aiDebugLog);
            }
        }

        public bool IsConfigured() => !string.IsNullOrEmpty(_apiKey);

        public bool IsLocalEndpoint => _isLocalConnection;

        public void Dispose()
        {
            // Player2Client uses a shared static health check CTS;
            // individual disposal does not stop the health check loop.
            // Use StopHealthCheck() to halt it globally.
        }

        public async Task<Result<AIResponse, RimMindError>> SendAsync(AIRequest request)
        {
            if (!string.IsNullOrEmpty(request.JsonSchema) || (request.Tools != null && request.Tools.Count > 0))
                return await SendStructuredAsync(request, request.JsonSchema, request.Tools);

            string endpoint = $"{CurrentApiUrl}/v1/chat/completions";
            string json = BuildRequestJson(request);

            if (_settings.DebugLogging)
                _logSink?.LogFromBackground($"[RimMind-Core] >> {request.RequestId} (Player2)\n{json}");

            var sw = Stopwatch.StartNew();
            try
            {
                await EnsureHealthCheck();

                (string responseText, long httpStatusCode) = await PostAsync(endpoint, json);
                var parsed = JsonConvert.DeserializeObject<Player2ResponseDto>(responseText);
                string content = parsed?.Choices?[0]?.Message?.Content ?? string.Empty;
                int tokens = parsed?.Usage?.TotalTokens ?? 0;
                int promptTokens = parsed?.Usage?.PromptTokens ?? 0;
                int completionTokens = parsed?.Usage?.CompletionTokens ?? 0;
                int cachedTokens = parsed?.Usage?.PromptTokensDetails?.CachedTokens ?? 0;
                sw.Stop();

                if (_settings.DebugLogging)
                    _logSink?.LogFromBackground($"[RimMind-Core] << {request.RequestId} ({tokens} tok)\n{content}");

                var response = AIResponse.Ok(request.RequestId, content, tokens);
                response.PromptTokens = promptTokens;
                response.CompletionTokens = completionTokens;
                response.CachedTokens = cachedTokens;
                response.ProcessingMs = sw.ElapsedMilliseconds;
                response.HttpStatusCode = httpStatusCode;
                response.RequestPayloadBytes = Encoding.UTF8.GetByteCount(json);
                response.Priority = request.Priority;
                _aiDebugLog?.Record(request, response, (int)sw.ElapsedMilliseconds);
                return Result<AIResponse, RimMindError>.Ok(response);
            }
            catch (TaskCanceledException)
            {
                sw.Stop();
                _logSink?.LogFromBackground($"[RimMind-Core] Player2 request cancelled ({request.RequestId})", isWarning: true);
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.Cancelled());
            }
            catch (HttpTransport.HttpException ex)
            {
                sw.Stop();
                _logSink?.LogFromBackground($"[RimMind-Core] Player2 request failed ({request.RequestId}): {ex.Message}", isWarning: true);
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.ClientTransient(ex.Message, ex));
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logSink?.LogFromBackground($"[RimMind-Core] Player2 request failed ({request.RequestId}): {ex.Message}", isWarning: true);
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.Internal($"Player2 request failed: {ex.Message}", ex));
            }
        }

        private string BuildRequestJson(AIRequest request)
        {
            var messages = new List<Player2MessageDto>();

            if (request.Messages != null && request.Messages.Count > 0)
            {
                messages = request.Messages
                    .Select(m => new Player2MessageDto { Role = m.Role, Content = m.Content })
                    .ToList();
            }
            else
            {
                if (!string.IsNullOrEmpty(request.SystemPrompt))
                    messages.Add(new Player2MessageDto { Role = "system", Content = request.SystemPrompt });
                messages.Add(new Player2MessageDto { Role = "user", Content = request.UserPrompt });
            }

            messages = MergeConsecutiveSameRole(messages);

            var body = new Player2RequestDto
            {
                Model = "default",
                Messages = messages,
                MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : _settings.MaxTokens,
                Temperature = request.Temperature,
                Stream = false,
            };

            return JsonConvert.SerializeObject(body, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private static List<Player2MessageDto> MergeConsecutiveSameRole(List<Player2MessageDto> messages)
        {
            if (messages == null || messages.Count <= 1) return messages!;

            var merged = new List<Player2MessageDto>(messages.Count);
            var current = messages[0];

            for (int i = 1; i < messages.Count; i++)
            {
                if (string.Equals(current.Role, messages[i].Role, StringComparison.OrdinalIgnoreCase))
                {
                    current = new Player2MessageDto
                    {
                        Role = current.Role,
                        Content = current.Content + "\n" + messages[i].Content
                    };
                }
                else
                {
                    merged.Add(current);
                    current = messages[i];
                }
            }
            merged.Add(current);

            return merged;
        }

        private async Task<(string text, long statusCode)> PostAsync(string url, string jsonBody)
        {
            bool isLocal = _isLocalConnection;
            float connectTimeout = isLocal ? LocalConnectTimeout : RemoteConnectTimeout;
            return await HttpTransport.PostAsync(url, jsonBody, $"Bearer {_apiKey}",
                "player2-game-key", GameClientId, connectTimeout: connectTimeout);
        }
    }
}
