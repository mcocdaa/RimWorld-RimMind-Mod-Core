using System.Collections.Generic;
using Newtonsoft.Json;

namespace RimMind.Adapters.Client.OpenAI
{
    internal class OpenAIRequestDto
    {
        public string model = "";
        public List<MessageDto>? messages;
        public int max_tokens;
        public float temperature;
        public bool stream;
        public ResponseFormatDto? response_format;
        public List<ToolDto>? tools;
        public object? tool_choice;
    }

    internal class OpenAIResponseDto
    {
        public List<ChoiceDto>? choices;
        public UsageDto? usage;
    }

    internal class ChoiceDto
    {
        public int index;
        public MessageDto? message;
        public string? finish_reason;
    }

    internal class UsageDto
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
        public PromptTokensDetailsDto? prompt_tokens_details;
    }

    internal class PromptTokensDetailsDto
    {
        public int cached_tokens;
    }

    internal class ResponseFormatDto
    {
        public string type = "";
        public object? json_schema;
    }

    internal class ToolDto
    {
        public string type = "function";
        public ToolFunctionDto? Function;
    }

    internal class ToolFunctionDto
    {
        public string Name = "";
        public string Description = "";
        public object? Parameters;
    }

    internal class ToolCallFunctionDto
    {
        public string Name = "";
        public string Arguments = "";
    }

    internal class MessageDto
    {
        public string role = "";
        public string? content;
        public string? reasoning_content;
        public string? name;
        public string? tool_call_id;
        public List<ToolCallDto>? tool_calls;

        [JsonProperty("function_call")]
        public object? function_call;
    }

    internal class ToolCallDto
    {
        public string Id = "";
        public string Type = "function";
        public ToolCallFunctionDto? Function;
    }
}
