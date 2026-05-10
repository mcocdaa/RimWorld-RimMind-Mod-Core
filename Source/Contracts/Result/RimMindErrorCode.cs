namespace RimMind.Contracts.Result
{
    public enum RimMindErrorCode
    {
        ClientNotConfigured = 1000,
        ClientTransientFailure = 1001,
        ClientPermanentFailure = 1002,
        ClientCircuitOpen = 1003,

        ContextBuildFailed = 2000,
        PipelineShortCircuited = 2001,

        ToolNotFound = 3000,
        ToolExecutionFailed = 3001,
        ToolPolicyDenied = 3002,
        ToolMaxDepthExceeded = 3003,

        NpcNotFound = 4000,
        StorageDriverFailed = 4001,

        InternalError = 9000,
        NotImplemented = 9001,
        Cancelled = 9002,
        Timeout = 9003,
    }
}
