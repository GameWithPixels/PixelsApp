using System.Runtime.InteropServices;
using UnityEngine;

namespace Systemic.Unity.Pixels.Animations
{
    /// <summary>
    /// Stores a single keyframe of an LED animation
    /// size: 2 bytes, split this way:
    /// - 9 bits: time 0 - 511 in 500th of a second (i.e )
    ///   + 1    -> 0.002s
    ///   + 500  -> 1s
    /// - 7 bits: intensity (0 - 127)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public struct SimpleKeyframe
    {
        public ushort timeAndIntensity;

        public ushort time()
        {
            // Take the upper 9 bits and multiply by 2 (scale it to 0 -> 1024)
            return (ushort)(((uint)timeAndIntensity >> 7) * 2);
        }

        public byte intensity()
        {
            // Take the lower 7 bits and multiply by 2 (scale it to 0 -> 255)
            return (byte)((timeAndIntensity & 0b1111111) * 2);
        }

        public void setTimeAndIntensity(float time, byte intensity)
        {
            //TODO check colorIndex < 128
            uint timeMs = (uint)Mathf.Round(Mathf.Max(0, time) * 1000);
            uint scaledTime = (timeMs / 2) & 0b111111111;
            uint scaledIntensity = ((uint)intensity / 2) & 0b1111111;
            timeAndIntensity = (ushort)((scaledTime << 7) | scaledIntensity);
        }

        public bool Equals(SimpleKeyframe other)
        {
            return timeAndIntensity == other.timeAndIntensity;
        }
    }
}
