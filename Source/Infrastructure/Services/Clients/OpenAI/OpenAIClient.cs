using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Features.Queue;
using RimMind.Domain.ValueObjects;
using Newtonsoft.Json;
using Verse;

namespace RimMind.Infrastructure.Services.Clients.OpenAI
{
    public class OpenAIClient : IAIClient
    {
        private static readonly ConcurrentDictionary<string, string> _formatCapabilityCache = new();

        internal static void InvalidateFormatCache()
        {
            _formatCapabilityCache.Clear();
        }

        private string BuildCacheKey() => $"{_settings.ApiEndpoint}|{_settings.ModelName}";

        private readonly IOpenAISettings _settings;

        public OpenAIClient(IOpenAISettings settings)
        {
            _settings = settings;
        }

        public bool IsConfigured() => _settings.IsConfigured();

        public bool IsLocalEndpoint => IsLoopbackEndpoint(_settings.ApiEndpoint);

        private static bool IsLoopbackEndpoint(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) return false;
            if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var uri)) return false;
            if (uri.IsLoopback) return true;
            string host = uri.Host;
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(host, "host.docker.internal", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public async Task<Result<AIResponse, RimMindError>> SendAsync(AIRequest request)
        {
            if (!string.IsNullOrEmpty(request.JsonSchema) || (request.Tools != null && request.Tools.Count > 0))
                return await SendStructuredAsync(request, request.JsonSchema, request.Tools);

            bool wantFormat = _settings.ForceJsonMode && request.UseJsonMode;
            string cacheKey = BuildCacheKey();

            if (wantFormat && _formatCapabilityCache.TryGetValue(cacheKey, out string? cached))
            {
                if (cached == "none")
                    wantFormat = false;
            }

            if (!wantFormat)
            {
                var noFormatResp = await SendAsyncInner(request, useResponseFormat: false);
                if (noFormatResp.IsOk && wantFormat != (_settings.ForceJsonMode && request.UseJsonMode))
                    AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Skipped json_object for {request.RequestId} (cached: endpoint doesn't support it)");
                return noFormatResp;
            }

            var response = await SendAsyncInner(request, useResponseFormat: true);
            if (response.IsOk)
            {
                _formatCapabilityCache[cacheKey] = "json_object";
                return response;
            }

            if (IsResponseFormatError(response))
            {
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] json_object not supported, retrying without response_format for {request.RequestId}", isWarning: true);
                _formatCapabilityCache[cacheKey] = "none";
                response = await SendAsyncInner(request, useResponseFormat: false);
            }
            return response;
        }

        private async Task<Result<AIResponse, RimMindError>> SendAsyncInner(AIRequest request, bool useResponseFormat)
        {
            string endpoint = FormatEndpoint(_settings.ApiEndpoint);
            string json = BuildRequestJson(request, useResponseFormat);

            if (_settings.DebugLogging)
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] ?? {request.RequestId}\n{json}");

            var sw = Stopwatch.StartNew();
            try
            {
                bool isLocal = IsLoopbackEndpoint(_settings.ApiEndpoint);
                float connectTimeout = isLocal ? 300f : 60f;
                (string responseText, long httpStatusCode) = await HttpHelper.PostAsync(
                    endpoint, json, $"Bearer {_settings.ApiKey}", connectTimeout: connectTimeout);
                var parsed = JsonConvert.DeserializeObject<OpenAIResponseDto>(responseText);
                string content = parsed?.choices?[0]?.message?.content ?? string.Empty;
                string? reasoningContent = parsed?.choices?[0]?.message?.reasoning_content;
                int tokens = parsed?.usage?.total_tokens ?? 0;
                int promptTokens = parsed?.usage?.prompt_tokens ?? 0;
                int completionTokens = parsed?.usage?.completion_tokens ?? 0;
                int cachedTokens = parsed?.usage?.prompt_tokens_details?.cached_tokens ?? 0;
                sw.Stop();

                if (_settings.DebugLogging)
                    AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] ?? {request.RequestId} ({tokens} tok)\n{content}");

                var response = AIResponse.Ok(request.RequestId, content, tokens);
                response.ReasoningContent = reasoningContent;
                response.PromptTokens = promptTokens;
                response.CompletionTokens = completionTokens;
                response.CachedTokens = cachedTokens;
                response.ProcessingMs = sw.ElapsedMilliseconds;
                response.HttpStatusCode = httpStatusCode;
                response.RequestPayloadBytes = Encoding.UTF8.GetByteCount(json);
                response.Priority = request.Priority;
                RimMindServiceLocator.Get<IAIDebugLog>()?.Record(request, response, (int)sw.ElapsedMilliseconds);
                return Result<AIResponse, RimMindError>.Ok(response);
            }
            catch (HttpHelper.HttpException ex)
            {
                sw.Stop();
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Request failed ({request.RequestId}): {ex.Message}", isWarning: true);
                var error = RimMindErrors.ClientTransient(ex.Message, ex);
                return Result<AIResponse, RimMindError>.Err(error);
            }
            catch (TaskCanceledException ex)
            {
                sw.Stop();
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Request cancelled ({request.RequestId}): {ex.Message}", isWarning: true);
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.Cancelled());
            }
            catch (Exception ex)
            {
                sw.Stop();
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Request failed ({request.RequestId}): {ex.Message}", isWarning: true);
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.Internal($"OpenAI request failed: {ex.Message}", ex));
            }
        }

        public async Task<Result<AIResponse, RimMindError>> SendStructuredAsync(AIRequest request, string? jsonSchema, List<StructuredTool>? tools)
        {
            string endpoint = FormatEndpoint(_settings.ApiEndpoint);
            string cacheKey = BuildCacheKey();

            string[] formatModes = { "json_schema", "json_object", "none" };
            int startIndex = 0;

            if (_formatCapabilityCache.TryGetValue(cacheKey, out string? cachedBest))
            {
                for (int i = 0; i < formatModes.Length; i++)
                {
                    if (formatModes[i] == cachedBest)
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex > 0)
                    AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Using cached format '{cachedBest}' for {request.RequestId} (skipping {startIndex} unsupported mode(s))");
            }

            for (int i = startIndex; i < formatModes.Length; i++)
            {
                string mode = formatModes[i];
                string? schema = mode == "json_schema" ? jsonSchema : null;

                var response = await TrySendStructuredAsync(request, endpoint, schema, tools, mode);

                if (response.IsOk)
                {
                    if (i > 0 || mode != "json_schema")
                        _formatCapabilityCache[cacheKey] = mode;
                    else
                        _formatCapabilityCache[cacheKey] = "json_schema";
                    return response;
                }

                if (IsResponseFormatError(response))
                {
                    AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Format '{mode}' not supported for {request.RequestId}, downgrading", isWarning: true);
                    continue;
                }

                return response;
            }

            _formatCapabilityCache[cacheKey] = "none";
            return Result<AIResponse, RimMindError>.Err(RimMindErrors.ClientPermanent("All response_format modes failed"));
        }

        public static bool IsResponseFormatError(Result<AIResponse, RimMindError> result)
        {
            if (!result.TryGetValue(out var response)) return false;
            if (response.HttpStatusCode != 400 && response.HttpStatusCode != 422) return false;
            return false;
        }

        private async Task<Result<AIResponse, RimMindError>> TrySendStructuredAsync(AIRequest request, string endpoint, string? jsonSchema, List<StructuredTool>? tools, string formatMode)
        {
            string json = BuildStructuredRequestJson(request, jsonSchema, tools, formatMode);

            if (_settings.DebugLogging)
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] ?? Structured {request.RequestId} (format={formatMode})\n{json}");

            var sw = Stopwatch.StartNew();
            try
            {
                bool isLocal = IsLoopbackEndpoint(_settings.ApiEndpoint);
                float connectTimeout = isLocal ? 300f : 60f;
                (string responseText, long httpStatusCode) = await HttpHelper.PostAsync(
                    endpoint, json, $"Bearer {_settings.ApiKey}", connectTimeout: connectTimeout);
                var parsed = JsonConvert.DeserializeObject<OpenAIResponseDto>(responseText);
                string content = parsed?.choices?[0]?.message?.content ?? string.Empty;
                string? reasoningContent = parsed?.choices?[0]?.message?.reasoning_content;
                int tokens = parsed?.usage?.total_tokens ?? 0;
                int promptTokens = parsed?.usage?.prompt_tokens ?? 0;
                int completionTokens = parsed?.usage?.completion_tokens ?? 0;
                int cachedTokens = parsed?.usage?.prompt_tokens_details?.cached_tokens ?? 0;
                var toolCallsDto = parsed?.choices?[0]?.message?.tool_calls;
                sw.Stop();

                if (_settings.DebugLogging)
                    AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] ?? Structured {request.RequestId} ({tokens} tok)\n{content}");

                var response = new AIResponse
                {
                    Content = content,
                    ReasoningContent = reasoningContent,
                    RequestId = request.RequestId,
                    TokensUsed = tokens,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens,
                    CachedTokens = cachedTokens,
                    State = AIRequestState.Completed,
                };
                response.ProcessingMs = sw.ElapsedMilliseconds;
                response.HttpStatusCode = httpStatusCode;
                response.RequestPayloadBytes = Encoding.UTF8.GetByteCount(json);
                response.Priority = request.Priority;

                if (toolCallsDto != null && toolCallsDto.Count > 0)
                {
                    var converted = toolCallsDto.Select(tc => new
                    {
                        id = tc.Id,
                        type = tc.Type,
                        function = new
                        {
                            name = tc.Function?.Name,
                            arguments = tc.Function?.Arguments,
                        }
                    }).ToList();
                    response.ToolCallsJson = JsonConvert.SerializeObject(converted);
                }
                else if (tools != null && tools.Count > 0 && !string.IsNullOrEmpty(content))
                {
                    AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] No tool_calls in response (format={formatMode}), content length={content.Length} for {request.RequestId}");
                    if (_settings.DebugLogging && content.Length > 0)
                        AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Response content (no tool_calls): {content}");
                }

                RimMindServiceLocator.Get<IAIDebugLog>()?.Record(request, response, (int)sw.ElapsedMilliseconds);
                return Result<AIResponse, RimMindError>.Ok(response);
            }
            catch (HttpHelper.HttpException ex)
            {
                sw.Stop();
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Structured request failed ({request.RequestId}): {ex.Message}", isWarning: true);
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.ClientTransient(ex.Message, ex));
            }
            catch (TaskCanceledException)
            {
                sw.Stop();
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Structured request cancelled ({request.RequestId})", isWarning: true);
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.Cancelled());
            }
            catch (Exception ex)
            {
                sw.Stop();
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Structured request failed ({request.RequestId}): {ex.Message}", isWarning: true);
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.Internal($"OpenAI structured request failed: {ex.Message}", ex));
            }
        }

        private string BuildRequestJson(AIRequest request, bool useResponseFormat = true)
        {
            List<MessageDto> messages = BuildMessages(request);

            if (useResponseFormat)
                EnsureJsonKeyword(messages);

            var body = new OpenAIRequestDto
            {
                model = _settings.ModelName,
                messages = messages,
                max_tokens = request.MaxTokens > 0 ? request.MaxTokens : _settings.MaxTokens,
                temperature = request.Temperature,
                stream = false,
            };

            if (useResponseFormat)
                body.response_format = new ResponseFormatDto { type = "json_object" };

            return JsonConvert.SerializeObject(body, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private string BuildStructuredRequestJson(AIRequest request, string? jsonSchema, List<StructuredTool>? tools, string formatMode = "json_schema")
        {
            List<MessageDto> messages = BuildMessages(request);

            if (formatMode == "json_object")
                EnsureJsonKeyword(messages);

            var body = new OpenAIRequestDto
            {
                model = _settings.ModelName,
                messages = messages,
                max_tokens = request.MaxTokens > 0 ? request.MaxTokens : _settings.MaxTokens,
                temperature = request.Temperature,
                stream = false,
            };

            if (formatMode == "json_schema" && !string.IsNullOrEmpty(jsonSchema))
            {
                body.response_format = new ResponseFormatDto
                {
                    type = "json_schema",
                    json_schema = new { name = "response", schema = JsonConvert.DeserializeObject(jsonSchema!) },
                };
            }
            else if (formatMode == "json_object" && (_settings.ForceJsonMode || request.UseJsonMode))
            {
                body.response_format = new ResponseFormatDto { type = "json_object" };
            }

            if (tools != null && tools.Count > 0)
            {
                body.tools = new List<ToolDto>();
                foreach (var t in tools)
                {
                    body.tools.Add(new ToolDto
                    {
                        Function = new ToolFunctionDto
                        {
                            Name = t.Name,
                            Description = t.Description,
                            Parameters = t.Parameters != null
                                ? JsonConvert.DeserializeObject(t.Parameters)
                                : new { type = "object", properties = new { } },
                        },
                    });
                }
                if (tools.Any(t => t.ToolChoice == "required"))
                    body.tool_choice = "required";
                else
                    body.tool_choice = "auto";
            }

            return JsonConvert.SerializeObject(body, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private List<MessageDto> BuildMessages(AIRequest request)
        {
            var messages = new List<MessageDto>();

            if (request.Messages != null && request.Messages.Count > 0)
            {
                foreach (var m in request.Messages)
                {
                    var dto = new MessageDto { role = m.Role, content = m.Content };
                    if (m.Role == "assistant" && !string.IsNullOrEmpty(m.ReasoningContent))
                        dto.reasoning_content = m.ReasoningContent;
                    if (!string.IsNullOrEmpty(m.ToolCallId))
                        dto.tool_call_id = m.ToolCallId;
                    if (m.ToolCalls != null && m.ToolCalls.Count > 0)
                    {
                        dto.tool_calls = m.ToolCalls.Select(tc => new ToolCallDto
                        {
                            Id = tc.Id,
                            Type = "function",
                            Function = new ToolCallFunctionDto
                            {
                                Name = tc.Name,
                                Arguments = tc.Arguments,
                            },
                        }).ToList();
                    }
                    messages.Add(dto);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(request.SystemPrompt))
                    messages.Add(new MessageDto { role = "system", content = request.SystemPrompt });
                messages.Add(new MessageDto { role = "user", content = request.UserPrompt });
            }

            return messages;
        }

        private static void EnsureJsonKeyword(List<MessageDto> messages)
        {
            foreach (var m in messages)
            {
                if (m.content != null && m.content.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
            }
            int lastSys = -1;
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].role == "system")
                {
                    lastSys = i;
                    break;
                }
            }
            if (lastSys >= 0)
                messages[lastSys].content = (messages[lastSys].content ?? "") + "\n\nPlease respond in JSON format.";
            else
                messages.Insert(0, new MessageDto { role = "system", content = "Please respond in JSON format." });
        }

        private static string FormatEndpoint(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl)) return string.Empty;
            string trimmed = baseUrl.Trim().TrimEnd('/');
            if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                return trimmed;
            var uri = new Uri(trimmed);
            string path = uri.AbsolutePath.Trim('/');
            if (!string.IsNullOrEmpty(path))
                return trimmed + "/chat/completions";
            return trimmed + "/v1/chat/completions";
        }
    }
}
