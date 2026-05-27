using System;

namespace RimMind.IntegrationTests
{
    /// <summary>
    /// xUnit shared fixture for TestWorld.
    /// Creates a TestWorld once per test class and disposes it after all tests complete.
    /// </summary>
    public sealed class TestWorldFixture : IDisposable
    {
        public TestWorld World { get; }

        public TestWorldFixture()
        {
            World = TestWorld.Create(colonistCount: 2);
        }

        public void Dispose()
        {
            World?.Dispose();
        }
    }

    /// <summary>
    /// Collection definition for RimWorld integration tests.
    /// Tests in this collection are not run in parallel with each other
    /// because they share RimWorld static state (Find.CurrentMap, etc.).
    /// </summary>
    [CollectionDefinition("RimWorld Integration", DisableParallelization = true)]
    public class RimWorldIntegrationCollection { }
}
