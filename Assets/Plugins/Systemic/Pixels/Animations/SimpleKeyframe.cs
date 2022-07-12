using System.Runtime.InteropServices;
using UnityEngine;

namespace Systemic.Unity.Pixels.Animations
{
    /// <summary>
    /// Stores a single keyframe of an LED animation
    /// size: 2 bytes, split this way:
    /// - 9 bits: time 0 - 511 in 50th of a second (i.e )
    ///   + 1    -> 0.02s
    ///   + 500  -> 10s
    /// - 7 bits: intensity (0 - 127)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public struct SimpleKeyframe
    {
        public ushort timeAndIntensity;

        public ushort time()
        {
            // Unpack
            uint time50th = ((uint)timeAndIntensity & 0b1111111110000000) >> 7;
            return (ushort)(time50th * 20);
        }

        public byte intensity()
        {
            // Unpack
            return (byte)((timeAndIntensity & 0b01111111) * 2); // Scale it to 0 -> 255
        }

        public void setTimeAndIntensity(float time, byte intensity)
        {
            uint time50th = (uint)(Mathf.Round(Mathf.Max(0, time) * 1000) / 20);
            timeAndIntensity = (ushort)(((time50th & 0b111111111) << 7) |
                           ((uint)(intensity / 2) & 0b1111111));
        }

        public bool Equals(SimpleKeyframe other)
        {
            return timeAndIntensity == other.timeAndIntensity;
        }
    }
}
