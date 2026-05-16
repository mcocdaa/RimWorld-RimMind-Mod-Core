using RimMind.Application.Common.Interfaces.Extension;
using UnityEngine;

namespace RimMind.Application.Common.Interfaces.Extension;

public interface ISettingsTab : IExtension
{
    string Label { get; }
    void Draw(Rect rect);
}
