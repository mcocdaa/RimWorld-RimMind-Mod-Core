# RimMind Core test contracts

Core uses three compact contract projects:

- `Tests/Contracts/`: domain, application, lifecycle, public API, and UI contracts.
- `IntegrationTests/Contracts/`: runtime adapter and mechanism contracts.
- `ArchTests/Contracts/`: dependency direction, runtime boundary, and cross-Mod contracts.

The three projects are one budget unit. The current retained suite discovers
23 + 6 + 9 = 38 tests, below the project target of 88 and the hard limit of 99.
Scenario matrices run inside named `Fact` contracts through
`TestSupport/ContractCaseRunner.cs`; active contract folders do not use
`Theory`.

## Retired legacy tests

Files outside `Contracts/` are retained on disk but excluded from compilation.
Their behavior mapping is recorded in the root contract mapping document.
Deletion requires explicit owner approval for each exact file path; directories
are never deletion candidates.
