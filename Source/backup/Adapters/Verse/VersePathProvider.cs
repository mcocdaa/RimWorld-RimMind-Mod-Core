using RimMind.Contracts.Abstractions;

namespace RimMind.Adapters.Verse
{
    public sealed class VersePathProvider : IPathProvider
    {
        public string SaveDataFolderPath => global::Verse.GenFilePaths.SaveDataFolderPath;
    }
}
