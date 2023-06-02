using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systemic.Unity.Pixels.Animations
{
    public static class Constants
    {
        public const int MaxLEDsCount = 20;
        public const uint FaceMaskAll = 0xFFFFFFFF;

        static int[] faceIndices = new int[] { 17, 1, 19, 13, 3, 10, 8, 5, 15, 7, 9, 11, 14, 4, 12, 0, 18, 2, 16, 6 };
        public static int getFaceIndex(int i) => faceIndices[i];

        public const ushort PaletteColorFromFace = 127;
        public const ushort PaletteColorFromRandom = 126;

        public const int KeyframeTimeResolutionMs = 2;
    }
}
