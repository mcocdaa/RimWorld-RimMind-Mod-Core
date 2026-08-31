# Core debug actions

Start at `../AICoreDebugActions.cs`. It keeps the stable class entry; responsibility files hold the actions.

## Reading map

- `AICoreDebugActions.Requests.cs`: connection, request trace, queue, and settings diagnostics.
- `AICoreDebugActions.ContextAndAgents.cs`: context, registries, learning state, AgentBus, history, and NPC diagnostics.
- `AICoreDebugActions.Windows.cs`: window entry points and UI layout inspection.
- `AICoreDebugActions.Autotests.cs`: game-side H2, P, K, L, and layout checks.

## Flow

Each action captures a runtime or game scope when invoked, reads the required service, then logs a report or opens a window. Async context preview publishes only while its captured runtime generation remains current.

## Local invariants

- Keep DebugAction methods thin and grouped by purpose.
- Resolve services at invocation time; never cache lifecycle-owned services here.
- Keep Verse and Unity side effects on the main thread.
- Preserve `[RIMTEST]` case IDs for external test parsing.

## Verification

The source contracts are in `Tests/Contracts/DebugCenterLifecycleContract.cs` and `Tests/Contracts/DebugCenterUiRegressionContract.cs`.
