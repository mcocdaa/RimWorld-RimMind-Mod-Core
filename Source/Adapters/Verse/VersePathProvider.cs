using RimMind.Kernel.Abstractions;

namespace RimMind.Adapters.Verse
{
    public sealed class VersePathProvider : IPathProvider
    {
        public string SaveDataFolderPath => Verse.GenFilePaths.SaveDataFolderPath;
    }
}
