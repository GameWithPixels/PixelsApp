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
    /// - 7 bits: color lookup (128 values)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public struct RGBKeyframe
    {
        public ushort timeAndColor;

        public ushort time()
        {
            // Take the upper 9 bits and multiply by 2 (scale it to 0 -> 1024)
            return (ushort)(((uint)timeAndColor >> 7) * 2);
        }

        public ushort colorIndex()
        {
            // Take the lower 7 bits for the index
            return (ushort)((uint)timeAndColor & 0b1111111);
        }

        public uint color(DataSet.AnimationBits bits)
        {
            return bits.getColor32(colorIndex());
        }

        public void setTimeAndColorIndex(float time, ushort colorIndex)
        {
            //TODO check colorIndex < 128
            uint timeMs = (uint)Mathf.Round(Mathf.Max(0, time) * 1000);
            uint scaledTime = (timeMs / 2) & 0b111111111;
            timeAndColor = (ushort)((scaledTime << 7) | ((uint)colorIndex & 0b1111111));
        }

        public bool Equals(RGBKeyframe other)
        {
            return timeAndColor == other.timeAndColor;
        }
    }
}
