namespace RimMind.Application.Common.Interfaces.Npc
{
    /// <summary>
    /// Abstraction for storage driver creation.
    /// Decouples Presentation layer from Infrastructure.StorageDriverFactory static methods.
    /// </summary>
    public interface IStorageDriverFactory
    {
        IStorageDriver? GetDriver();
    }
}
