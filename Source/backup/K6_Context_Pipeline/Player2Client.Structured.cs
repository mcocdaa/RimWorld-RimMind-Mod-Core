using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimMind.Application.Common.Helpers;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.Common;
using RimMind.Domain.ValueObjects;
using Newtonsoft.Json;
using RimWorld;
using UnityEngine.Networking;
using Verse;

namespace RimMind.Infrastructure.Services.Clients.Player2
{
    public partial class Player2Client
    {
        public async Task<Result<AIResponse, RimMindError>> SendStructuredAsync(AIRequest request, string? jsonSchema, List<StructuredTool>? tools)
        {
            try
            {
                var messages = new List<Player2MessageDto>();
                if (request.Messages != null && request.Messages.Count > 0)
                    messages = request.Messages.Select(m => new Player2MessageDto { Role = m.Role, Content = m.Content }).ToList();
                else
                {
                    if (!string.IsNullOrEmpty(request.SystemPrompt))
                        messages.Add(new Player2MessageDto { Role = "system", Content = request.SystemPrompt });
                    messages.Add(new Player2MessageDto { Role = "user", Content = request.UserPrompt });
                }

                messages = MergeConsecutiveSameRole(messages);

                var body = new Dictionary<string, object?>
                {
                    ["model"] = "default",
                    ["messages"] = messages,
                    ["max_tokens"] = request.MaxTokens > 0 ? request.MaxTokens : _settings.MaxTokens,
                    ["temperature"] = request.Temperature,
                };

                if (!string.IsNullOrEmpty(jsonSchema))
                {
                    body["response_format"] = new
                    {
                        type = "json_schema",
                        json_schema = new { name = "response", schema = JsonConvert.DeserializeObject(jsonSchema!) },
                    };
                }

                if (tools != null && tools.Count > 0)
                {
                    var toolList = new List<object>();
                    foreach (var t in tools)
                    {
                        toolList.Add(new
                        {
                            type = "function",
                            function = new
                            {
                                name = t.Name,
                                description = t.Description,
                                parameters = t.Parameters != null ? JsonConvert.DeserializeObject(t.Parameters) : new { type = "object", properties = new { } },
                            },
                        });
                    }
                    body["tools"] = toolList;
                    if (tools.Any(t => t.ToolChoice == "required"))
                        body["tool_choice"] = "required";
                    else
                        body["tool_choice"] = "auto";
                }

                string json = JsonConvert.SerializeObject(body, Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                string endpoint = $"{CurrentApiUrl}/v1/chat/completions";
                using var webRequest = new UnityWebRequest(endpoint, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
                webRequest.SetRequestHeader("player2-game-key", GameClientId);
                webRequest.timeout = _isLocalConnection ? RimMindDefaults.Player2StructuredTimeout : 60;

                var asyncOp = webRequest.SendWebRequest();
                while (!asyncOp.isDone)
                {
                    if (Current.Game == null)
                        return Result<AIResponse, RimMindError>.Err(RimMindErrors.Cancelled());
                    await Task.Delay(RimMindDefaults.Player2StructuredPollingDelay);
                }

                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errBody = webRequest.downloadHandler?.text ?? webRequest.error ?? "Unknown error";
                    return Result<AIResponse, RimMindError>.Err(RimMindErrors.ClientTransient(errBody));
                }

                var dto = JsonConvert.DeserializeObject<Player2ResponseDto>(webRequest.downloadHandler.text);
                string? content = dto?.Choices?.FirstOrDefault()?.Message?.Content;
                int tokens = dto?.Usage?.TotalTokens ?? 0;
                int promptTokens = dto?.Usage?.PromptTokens ?? 0;
                int completionTokens = dto?.Usage?.CompletionTokens ?? 0;
                int cachedTokens = dto?.Usage?.PromptTokensDetails?.CachedTokens ?? 0;
                var toolCalls = dto?.Choices?.FirstOrDefault()?.Message?.ToolCalls;
                var response = new AIResponse
                {
                    Content = content ?? "",
                    RequestId = request.RequestId,
                    TokensUsed = tokens,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens,
                    CachedTokens = cachedTokens,
                    State = AIRequestState.Completed,
                };
                if (toolCalls != null && toolCalls.Count > 0)
                {
                    response.ToolCallsJson = JsonConvert.SerializeObject(toolCalls);
                }
                return Result<AIResponse, RimMindError>.Ok(response);
            }
            catch (TaskCanceledException)
            {
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.Cancelled());
            }
            catch (HttpTransport.HttpException ex)
            {
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.ClientTransient(ex.Message, ex));
            }
            catch (Exception ex)
            {
                return Result<AIResponse, RimMindError>.Err(RimMindErrors.Internal($"Player2 structured request failed: {ex.Message}", ex));
            }
        }
    }
}
