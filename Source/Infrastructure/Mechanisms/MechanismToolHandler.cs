using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Features.Json;

namespace RimMind.Infrastructure.Mechanisms
{
    public sealed class MechanismToolHandler : IToolHandler
    {
        private readonly IGameMechanism _mechanism;
        private readonly MechanismOperationType _operation;

        public MechanismToolHandler(IGameMechanism mechanism, MechanismOperationType operation)
        {
            _mechanism = mechanism;
            _operation = operation;
            Definition = BuildDefinition(mechanism, operation);
        }

        public string Id => Definition.Id;
        public ToolDefinition Definition { get; }

        public async Task<Result<ToolResult, RimMindError>> ExecuteAsync(ToolCallArgs args, CancellationToken ct)
        {
            var result = await ExecuteOperationAsync(args, ct).ConfigureAwait(false);

            if (result.IsErr)
            {
                return Result<ToolResult, RimMindError>.Ok(new ToolResult
                {
                    ToolCallId = args.ToolCallId,
                    Content = result.Error.Message,
                    IsError = true
                });
            }

            var content = _operation == MechanismOperationType.List
                ? JsonConvert.SerializeObject(result.Value)
                : result.Value?.ToString() ?? "";

            return Result<ToolResult, RimMindError>.Ok(new ToolResult
            {
                ToolCallId = args.ToolCallId,
                Content = content,
                IsError = false
            });
        }

        private async Task<Result<object?, RimMindError>> ExecuteOperationAsync(ToolCallArgs args, CancellationToken ct)
        {
            switch (_operation)
            {
                case MechanismOperationType.Query:
                {
                    var readArgs = BuildReadArgs(args);
                    var r = await _mechanism.ExecuteQueryAsync(readArgs, ct).ConfigureAwait(false);
                    return r.IsOk
                        ? Result<object?, RimMindError>.Ok(r.Value)
                        : Result<object?, RimMindError>.Err(r.Error);
                }
                case MechanismOperationType.Set:
                case MechanismOperationType.Add:
                case MechanismOperationType.Remove:
                case MechanismOperationType.Toggle:
                case MechanismOperationType.Trigger:
                case MechanismOperationType.Watch:
                {
                    var writeArgs = BuildWriteArgs(args);
                    var r = _operation switch
                    {
                        MechanismOperationType.Set => await _mechanism.ExecuteSetAsync(writeArgs, ct).ConfigureAwait(false),
                        MechanismOperationType.Add => await _mechanism.ExecuteAddAsync(writeArgs, ct).ConfigureAwait(false),
                        MechanismOperationType.Remove => await _mechanism.ExecuteRemoveAsync(writeArgs, ct).ConfigureAwait(false),
                        MechanismOperationType.Toggle => await _mechanism.ExecuteToggleAsync(writeArgs, ct).ConfigureAwait(false),
                        MechanismOperationType.Trigger => await _mechanism.ExecuteTriggerAsync(writeArgs, ct).ConfigureAwait(false),
                        MechanismOperationType.Watch => await _mechanism.ExecuteWatchAsync(writeArgs, ct).ConfigureAwait(false),
                        _ => Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(_mechanism.MechanismId, _operation.ToString().ToLowerInvariant()))
                    };
                    return r.IsOk
                        ? Result<object?, RimMindError>.Ok(r.Value)
                        : Result<object?, RimMindError>.Err(r.Error);
                }
                case MechanismOperationType.List:
                {
                    var pawnId = ExtractInt(args.ArgumentsJson, "pawn_id");
                    var r = await _mechanism.ExecuteListAsync(pawnId, ct).ConfigureAwait(false);
                    return r.IsOk
                        ? Result<object?, RimMindError>.Ok(r.Value)
                        : Result<object?, RimMindError>.Err(r.Error);
                }
                default:
                    return Result<object?, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(_mechanism.MechanismId, _operation.ToString().ToLowerInvariant()));
            }
        }

        private MechanismReadArgs BuildReadArgs(ToolCallArgs args)
        {
            return new MechanismReadArgs
            {
                MechanismId = _mechanism.MechanismId,
                PawnId = ExtractInt(args.ArgumentsJson, "pawn_id"),
                MapId = ExtractNullableInt(args.ArgumentsJson, "map_id"),
                DefName = ExtractString(args.ArgumentsJson, "filter_def_name") ?? ExtractString(args.ArgumentsJson, "def_name"),
                TraceId = args.TraceId
            };
        }

        private MechanismWriteArgs BuildWriteArgs(ToolCallArgs args)
        {
            return new MechanismWriteArgs
            {
                MechanismId = _mechanism.MechanismId,
                PawnId = ExtractInt(args.ArgumentsJson, "pawn_id"),
                MapId = ExtractNullableInt(args.ArgumentsJson, "map_id"),
                DefName = ExtractString(args.ArgumentsJson, "def_name"),
                Action = ExtractString(args.ArgumentsJson, "action") ?? _operation.ToString().ToLowerInvariant(),
                ValueJson = ExtractString(args.ArgumentsJson, "value") ?? ExtractString(args.ArgumentsJson, "params"),
                TraceId = args.TraceId,
                Params = ExtractParamsDictionary(args.ArgumentsJson)
            };
        }

        private static Dictionary<string, string>? ExtractParamsDictionary(string? json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                var paramsToken = obj["params"];
                if (paramsToken is Newtonsoft.Json.Linq.JObject paramsObj)
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var prop in paramsObj.Properties())
                    {
                        dict[prop.Name] = prop.Value?.ToString() ?? "";
                    }
                    return dict.Count > 0 ? dict : null;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static ToolDefinition BuildDefinition(IGameMechanism mechanism, MechanismOperationType operation)
        {
            var toolId = $"{mechanism.MechanismId}.{OperationSuffix(operation)}";
            var description = BuildDescription(mechanism, operation);
            var schema = BuildParameterSchema(mechanism, operation);

            return new ToolDefinition
            {
                Id = toolId,
                Description = description,
                ParametersSchema = schema,
                Category = mechanism.Scope.ToString().ToLowerInvariant()
            };
        }

        private static string BuildDescription(IGameMechanism mechanism, MechanismOperationType operation)
        {
            var desc = operation switch
            {
                MechanismOperationType.Query => mechanism.Docs.QueryDescription ?? mechanism.Docs.Summary,
                MechanismOperationType.Set => mechanism.Docs.SetDescription ?? mechanism.Docs.Summary,
                MechanismOperationType.Add => mechanism.Docs.AddDescription ?? mechanism.Docs.Summary,
                MechanismOperationType.Remove => mechanism.Docs.RemoveDescription ?? mechanism.Docs.Summary,
                MechanismOperationType.Toggle => mechanism.Docs.ToggleDescription ?? mechanism.Docs.Summary,
                MechanismOperationType.Trigger => mechanism.Docs.TriggerDescription ?? mechanism.Docs.Summary,
                MechanismOperationType.List => mechanism.Docs.ListDescription ?? mechanism.Docs.Summary,
                MechanismOperationType.Watch => mechanism.Docs.WatchDescription ?? mechanism.Docs.Summary,
                _ => mechanism.Docs.Summary
            };

            if (mechanism.GetRiskForOperation(operation) == MechanismRisk.Dangerous)
            {
                desc = $"[DANGEROUS] {desc}";
            }

            return desc;
        }

        private static string BuildParameterSchema(IGameMechanism mechanism, MechanismOperationType operation)
        {
            var properties = new Dictionary<string, object>();
            var required = new List<string>();

            if (mechanism.Scope == MechanismScope.Pawn)
            {
                properties["pawn_id"] = new { type = "integer", description = "Pawn thing ID" };
                required.Add("pawn_id");
                properties["map_id"] = new { type = "integer", description = "Optional map ID for multi-map scenarios" };
            }
            else if (mechanism.Scope == MechanismScope.Map || mechanism.Scope == MechanismScope.Colony)
            {
                properties["pawn_id"] = new { type = "integer", description = "Pawn thing ID (optional)" };
                properties["map_id"] = new { type = "integer", description = "Optional map ID; defaults to current map" };
            }
            else
            {
                properties["map_id"] = new { type = "integer", description = "Optional map ID for map-specific queries" };
            }

            switch (operation)
            {
                case MechanismOperationType.Query:
                    properties["filter_def_name"] = new { type = "string", description = "Optional def name to filter" };
                    break;

                case MechanismOperationType.Set:
                case MechanismOperationType.Add:
                case MechanismOperationType.Remove:
                case MechanismOperationType.Toggle:
                    properties["def_name"] = new { type = "string", description = "Def name to target" };
                    properties["value"] = new { type = "string", description = "Value for the operation (JSON)" };
                    break;

                case MechanismOperationType.Trigger:
                    properties["def_name"] = new { type = "string", description = "Optional def name to target" };
                    properties["params"] = new { type = "object", description = "Optional parameters for the trigger" };
                    break;

                case MechanismOperationType.List:
                    break;

                case MechanismOperationType.Watch:
                    properties["def_name"] = new { type = "string", description = "Optional def name to watch" };
                    break;
            }

            var writeActions = mechanism.GetWriteActions();
            if (writeActions != null && writeActions.Count > 0
                && (operation == MechanismOperationType.Set
                    || operation == MechanismOperationType.Add
                    || operation == MechanismOperationType.Remove
                    || operation == MechanismOperationType.Toggle
                    || operation == MechanismOperationType.Trigger))
            {
                properties["action"] = new
                {
                    type = "string",
                    @enum = writeActions.Select(a => a.Action).ToArray(),
                    description = "Action to perform"
                };
                required.Add("action");

                foreach (var wa in writeActions)
                {
                    if (wa.RequiredParams == null) continue;
                    foreach (var p in wa.RequiredParams)
                    {
                        if (!properties.ContainsKey(p))
                        {
                            properties[p] = new { type = "string", description = $"Parameter for {wa.Action}" };
                        }
                    }
                }
            }

            var schema = new
            {
                type = "object",
                properties,
                required = required.ToArray()
            };

            return JsonConvert.SerializeObject(schema);
        }

        private static string OperationSuffix(MechanismOperationType operation)
        {
            return operation switch
            {
                MechanismOperationType.Query => "query",
                MechanismOperationType.Set => "set",
                MechanismOperationType.Add => "add",
                MechanismOperationType.Remove => "remove",
                MechanismOperationType.Toggle => "toggle",
                MechanismOperationType.Trigger => "trigger",
                MechanismOperationType.List => "list",
                MechanismOperationType.Watch => "watch",
                _ => operation.ToString().ToLowerInvariant()
            };
        }

        private static string? ExtractString(string? json, string propertyName) => JsonHelpers.ExtractString(json ?? "{}", propertyName);

        private static int ExtractInt(string? json, string propertyName)
        {
            var str = JsonHelpers.ExtractString(json ?? "{}", propertyName);
            return int.TryParse(str, out var val) ? val : 0;
        }

        private static int? ExtractNullableInt(string? json, string propertyName)
        {
            var str = JsonHelpers.ExtractString(json ?? "{}", propertyName);
            return int.TryParse(str, out var val) ? val : (int?)null;
        }
    }
}
