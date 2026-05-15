using RimMind.Application.Common.Interfaces.Abstractions;

namespace RimMind.IntegrationTests.Stubs
{
    internal sealed class TestPathProvider : IPathProvider
    {
        public string SaveDataFolderPath => System.IO.Path.GetTempPath();
    }
}
