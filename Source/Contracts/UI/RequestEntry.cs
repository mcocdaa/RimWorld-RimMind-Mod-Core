using System;

namespace RimMind.Contracts.UI
{
    public class RequestEntry
    {
        public string title = "";
        public string description = "";
        public string[] options = Array.Empty<string>();
        public string[]? optionTooltips;
        public Action<string>? callback;
        public object? pawn;
        public bool systemBlocked;
        public int expireTicks;
        public int tick;
    }
}
