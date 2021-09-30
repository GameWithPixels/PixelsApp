using System.Runtime.InteropServices;

/// <summary>
/// Set of types for manipulating Pixel animation profiles data.
/// </summary>
namespace Systemic.Unity.Pixels.Profiles
{
    /// <summary>
    /// A Pixel LED animation profile which is made of a list of rules.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class Profile
    {
        public ushort rulesOffset;
        public ushort rulesCount;
    }
}
