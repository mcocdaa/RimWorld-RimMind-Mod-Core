namespace RimMind.Presentation.UI.Framework
{
    public sealed class GenerationUiState
    {
        public long BoundGeneration { get; private set; } = -1;

        public bool HasDerivedState { get; private set; }

        public bool HasActiveInteraction { get; private set; }

        public bool Refresh(long generation)
        {
            if (generation == BoundGeneration)
                return false;

            BoundGeneration = generation;
            HasDerivedState = false;
            HasActiveInteraction = false;
            return true;
        }

        public void MarkDerivedState()
        {
            HasDerivedState = true;
        }

        public void MarkInteractionActive()
        {
            HasActiveInteraction = true;
        }

        public void ClearInteraction()
        {
            HasActiveInteraction = false;
        }
    }
}
