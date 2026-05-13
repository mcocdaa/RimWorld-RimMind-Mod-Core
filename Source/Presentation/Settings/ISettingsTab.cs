using RimMind.Application.Common.Interfaces.Extension;
using Verse;

namespace RimMind.Presentation.Settings;

public interface ISettingsTab : IExtension
{
    string Label { get; }
    void Draw(Rect rect);
}
