using System.Runtime.InteropServices;
using UnityEngine;

namespace Systemic.Unity.Pixels.Animations
{
    /// <summary>
    /// Stores a single keyframe of an LED animation.
    /// The keyframe is made of a time and a color index.
    /// Size: 2 bytes, split this way:
    /// - 9 bits: time 0 - 511 in 500th of a second (i.e )
    ///   + 1    -> 0.002s
    ///   + 500  -> 1s
    /// - 7 bits: color lookup (128 values)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public struct RGBKeyframe
    {
        /// <summary>
        /// The time and color index combined in one value for serialization.
        /// <summary>
        public ushort timeAndColor;

        /// <summary>
        /// Gets the time in milliseconds, from to 0 to 1024 excluded.
        /// </summary>
        public ushort time
        {
            get
            {
                // Take the upper 9 bits and multiply by 2 (scale it to 0 -> 1024)
                return (ushort)(((uint)timeAndColor >> 7) * 2);
            }
        }

        /// <summary>
        /// Gets the color index, from to 0 to 128 excluded.
        /// </summary>
        public ushort colorIndex
        {
            get
            {
                // Take the lower 7 bits for the index
                return (ushort)((uint)timeAndColor & 0b1111111);
            }
        }

        /// <summary>
        /// Gets the 32 bits color for the color index of this instance.
        /// </summary>
        /// <param name="bits">The animation bits with the color palette.</param>
        /// <returns>The 32 bits color for the instance color index.</returns>
        public uint getColor(DataSet.AnimationBits bits)
        {
            return bits.getColor32(colorIndex);
        }

        /// <summary>
        /// Updates the instance timeAndColor member with the given time and color index.
        /// </summary>
        /// <param name="time">The time in milliseconds, from to 0 to 1024 excluded.</param>
        /// <param name="intensity">The color index, from to 0 to 128 excluded.</param>
        public void setTimeAndColorIndex(float time, ushort colorIndex)
        {
            //TODO check colorIndex < 128
            uint timeMs = (uint)Mathf.Round(Mathf.Max(0, time) * 1000);
            uint scaledTime = (timeMs / 2) & 0b111111111;
            timeAndColor = (ushort)((scaledTime << 7) | ((uint)colorIndex & 0b1111111));
        }

        /// <summary>
        /// Compares two RGBKeyframe instances.
        /// </summary>
        /// <param name="other">The RGBKeyframe instance to compare with.</param>
        /// <returns>Whether the two RGBKeyframe instances have the same data.</returns>
        public bool Equals(RGBKeyframe other)
        {
            return timeAndColor == other.timeAndColor;
        }
    }
}
