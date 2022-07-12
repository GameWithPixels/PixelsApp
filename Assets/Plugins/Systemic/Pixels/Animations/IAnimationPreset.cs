
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
        //TODO [SkipEnumValue]
        Unknown = 0,
        //TODO [Name("Simple Flashes")][DisplayOrder(0)]
        Simple,
        //TODO [Name("Colorful Rainbow")][DisplayOrder(1)]
        Rainbow,
        //TODO [Name("Color LED Pattern")][DisplayOrder(3)]
        Keyframed,
        //TODO [Name("Gradient LED Pattern")][DisplayOrder(4)]
        GradientPattern,
        //TODO [Name("Simple Gradient")][DisplayOrder(2)]
        Gradient,
    };

    /// <summary>
    /// Base class for animation presets. All presets have a few properties in common.
    /// Presets are stored in flash, so do not have methods or vtables or anything like that.
    /// </summary>
    public interface IAnimationPreset
    {
        AnimationType type { get; set; }
        byte padding_type { get; set; } // to keep duration 16-bit aligned
        ushort duration { get; set; } // in ms
        AnimationInstance CreateInstance(DataSet.AnimationBits bits);
    };
}
