
using System;
/// <summary>
/// Set of types for manipulating Pixel LEDs animations data.
/// </summary>
namespace Systemic.Unity.Pixels.Animations
{
    /// <summary>
    /// Defines the supported types of Animation Presets.
    /// </summary>
    public enum AnimationType : byte
    {
        None = 0,
        Simple,
        Rainbow,
        Keyframed,
        GradientPattern,
        Gradient,
    };

    [Flags]
    public enum AnimationFlags : byte
    {
        None,
        Traveling = 1,
        UseLedIndices = 2,
    };

    /// <summary>
    /// Base class for animation presets. All presets have a few properties in common.
    /// Presets are stored in flash, so do not have methods or vtables or anything like that.
    /// </summary>
    public interface IAnimationPreset
    {
        AnimationType type { get; set; }
        AnimationFlags animFlags { get; set; }
        ushort duration { get; set; } // in ms
        AnimationInstance CreateInstance(DataSet.AnimationBits bits);
    };
}
