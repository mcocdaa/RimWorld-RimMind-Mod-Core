using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Json;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;

namespace RimMind.Infrastructure.Mechanisms
{
    public sealed class MechanismListToolHandler : IToolHandler
    {
        private readonly IMechanismReader _reader;
        private readonly IMechanismMetadata _metadata;

        public MechanismListToolHandler(IMechanismReader reader, IMechanismMetadata metadata)
        {
            _reader = reader;
            _metadata = metadata;
            Definition = BuildDefinition(metadata);
        }

        public string Id => Definition.Id;
        public ToolDefinition Definition { get; }

        public async Task<Result<ToolResult, RimMindError>> ExecuteAsync(ToolCallArgs args, CancellationToken ct)
        {
            var pawnId = ExtractNullableInt(args.ArgumentsJson, "pawn_id");
            var category = ExtractString(args.ArgumentsJson, "category");

            var result = await _reader.ExecuteListAsync(pawnId, ct).ConfigureAwait(false);

            if (result.IsErr)
            {
                return Result<ToolResult, RimMindError>.Ok(new ToolResult
                {
                    ToolCallId = args.ToolCallId,
                    Content = result.Error.Message,
                    IsError = true
                });
            }

            var items = result.Value;
            if (!string.IsNullOrEmpty(category) && items != null)
            {
                items = FilterByCategory(items, category);
            }

            return Result<ToolResult, RimMindError>.Ok(new ToolResult
            {
                ToolCallId = args.ToolCallId,
                Content = JsonConvert.SerializeObject(items),
                IsError = false
            });
        }

        private static IReadOnlyList<MechanismEnumResult> FilterByCategory(IReadOnlyList<MechanismEnumResult> items, string category)
        {
            var filtered = new List<MechanismEnumResult>();
            foreach (var item in items)
            {
                if (item.DefName?.StartsWith(category) == true
                    || item.Label?.StartsWith(category, System.StringComparison.OrdinalIgnoreCase) == true)
                {
                    filtered.Add(item);
                }
            }
            return filtered.AsReadOnly();
        }

        private static ToolDefinition BuildDefinition(IMechanismMetadata metadata)
        {
            var toolId = $"{metadata.MechanismId}.list";
            var description = metadata.Docs.ListDescription ?? metadata.Docs.Summary;

            var properties = new Dictionary<string, object>();
            if (metadata.Scope == MechanismScope.Pawn)
            {
                properties["pawn_id"] = new { type = "integer", description = "Pawn thing ID (optional)" };
            }
            properties["category"] = new { type = "string", description = "Optional category prefix to filter results" };

            var schema = new
            {
                type = "object",
                properties,
                required = new string[0]
            };

            return new ToolDefinition
            {
                Id = toolId,
                Description = description,
                ParametersSchema = JsonConvert.SerializeObject(schema),
                Category = metadata.Scope.ToString().ToLowerInvariant()
            };
        }

        private static string? ExtractString(string? json, string propertyName) => JsonHelpers.ExtractString(json ?? "{}", propertyName);

        private static int? ExtractNullableInt(string? json, string propertyName)
        {
            var str = JsonHelpers.ExtractString(json ?? "{}", propertyName);
            return int.TryParse(str, out var val) ? val : (int?)null;
        }
    }
}
