using UnityEngine;

namespace RimMind.Contracts.Extensions;

public interface ISettingsTab : IExtension
{
    string Label { get; }
    void Draw(Rect rect);
}
