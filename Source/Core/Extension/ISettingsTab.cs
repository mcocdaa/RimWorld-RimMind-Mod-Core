using RimMind.Contracts.Extension;
using UnityEngine;

namespace RimMind.Core.Extension
{
    public interface ISettingsTab : IExtension
    {
        string Label { get; }
        void Draw(Rect rect);
    }
}
