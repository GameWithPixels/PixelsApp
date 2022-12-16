using System.Runtime.InteropServices;
using UnityEngine;

namespace Systemic.Unity.Pixels.Animations
{
    /// <summary>
    /// Stores a single keyframe of an LED animation.
    /// The keyframe is made of a time and an intensity.
    /// Size: 2 bytes, split this way:
    /// - 9 bits: time 0 - 511 in 500th of a second (i.e )
    ///   + 1    -> 0.002s
    ///   + 500  -> 1s
    /// - 7 bits: intensity (0 - 127)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public struct SimpleKeyframe
    {
        /// <summary>
        /// The time and intensity combined in one value for serialization.
        /// </summary>
        public ushort timeAndIntensity;

        /// <summary>
        /// Gets the time in milliseconds, from to 0 to 1024 excluded.
        /// </summary>
        public ushort time
        {
            get
            {
                // Take the upper 9 bits and multiply by 2 (scale it to 0 -> 1024)
                return (ushort)(((uint)timeAndIntensity >> 7) * 2);
            }
        }

        /// <summary>
        /// Gets the light intensity, from to 0 to 255 excluded.
        /// </summary>
        public byte intensity
        {
            get
            {
                // Take the lower 7 bits and multiply by 2 (scale it to 0 -> 255)
                return (byte)((timeAndIntensity & 0b1111111) * 2);
            }
        }

        /// <summary>
        /// Updates the instance timeAndIntensity member with the given time and intensity.
        /// </summary>
        /// <param name="time">The time in milliseconds, from to 0 to 1024 excluded.</param>
        /// <param name="intensity">The light intensity, from to 0 to 255 excluded.</param>
        public void setTimeAndIntensity(float time, byte intensity)
        {
            //TODO check colorIndex < 128
            uint timeMs = (uint)Mathf.Round(Mathf.Max(0, time) * 1000);
            uint scaledTime = (timeMs / 2) & 0b111111111;
            uint scaledIntensity = ((uint)intensity / 2) & 0b1111111;
            timeAndIntensity = (ushort)((scaledTime << 7) | scaledIntensity);
        }

        /// <summary>
        /// Compares two SimpleKeyframe instances.
        /// </summary>
        /// <param name="other">The SimpleKeyframe instance to compare with.</param>
        /// <returns>Whether the two SimpleKeyframe instances have the same data.</returns>
        public bool Equals(SimpleKeyframe other)
        {
            return timeAndIntensity == other.timeAndIntensity;
        }
    }
}
