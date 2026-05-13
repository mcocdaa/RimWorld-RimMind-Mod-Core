using UnityEngine;

namespace RimMind.Contracts.Extension;

public interface ISettingsTab : IExtension
{
    string Label { get; }
    void Draw(Rect rect);
}
