using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Models.Client;
using Newtonsoft.Json;

namespace RimMind.Infrastructure.Services.Clients.OpenAI
{
    public partial class OpenAIClient
    {
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
