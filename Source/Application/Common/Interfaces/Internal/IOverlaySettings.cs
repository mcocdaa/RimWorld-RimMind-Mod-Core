namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface IOverlaySettings
    {
        bool RequestOverlayEnabled { get; set; }
        float RequestOverlayX { get; set; }
        float RequestOverlayY { get; set; }
        float RequestOverlayW { get; set; }
        float RequestOverlayH { get; set; }

        void Persist();
    }
}
