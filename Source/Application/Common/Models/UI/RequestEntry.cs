using System;

namespace RimMind.Application.Common.Models.UI
{
    public class RequestEntry
    {
        public string title = "";
        public string description = "";
        public string[] options = Array.Empty<string>();
        public string[]? optionTooltips;
        public Action<string>? callback;
        public object? pawn;
        public string source = "";
        public bool systemBlocked;
        public int expireTicks;
        public int tick;

        public int ExpireAtTicks
        {
            get => expireTicks;
            set => expireTicks = value;
        }
    }
}
