namespace RimMind.Contracts.Extensions
{
    public interface IAgentActionBridge
    {
        void ExecuteAction(string npcId, string actionName, string[]? args = null);
        bool CanExecute(string npcId, string actionName);
    }
}
