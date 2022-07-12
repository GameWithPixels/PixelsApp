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
    /// - 7 bits: color lookup (128 values)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public struct RGBKeyframe
    {
        public ushort timeAndColor;

        public ushort time()
        {
            // Unpack
            uint time50th = ((uint)timeAndColor & 0b1111111110000000) >> 7;
            return (ushort)(time50th * 20);
        }

        public ushort colorIndex()
        {
            // Unpack
            return (ushort)(timeAndColor & 0b01111111);
        }

        public uint color(DataSet.AnimationBits bits)
        {
            return bits.getColor32(colorIndex());
        }

        public void setTimeAndColorIndex(float time, ushort colorIndex)
        {
            //TODO check colorIndex < 128
            uint time50th = (uint)(Mathf.Round(Mathf.Max(0, time) * 1000) / 20);
            timeAndColor = (ushort)(((time50th & 0b111111111) << 7) |
                           ((uint)colorIndex & 0b1111111));
        }

        public bool Equals(RGBKeyframe other)
        {
            return timeAndColor == other.timeAndColor;
        }
    }
}
