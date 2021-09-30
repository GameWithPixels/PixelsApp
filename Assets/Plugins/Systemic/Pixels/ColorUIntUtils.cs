using UnityEngine;

namespace Systemic.Unity.Pixels
{
    /// <summary>
    /// Helper static class that implements various color operations with the color information
    /// being stored as an unsigned 32 bits value.
    /// In related methods, the intensity is a byte value between 0 (black) and 255 (white).
    /// </summary>
    public static class ColorUIntUtils
    {
        /// <summary>
        /// Converts a (red, green, blue) bytes triplets to a 32 bits color value.
        /// </summary>
        /// <param name="red">The red component as a byte value.</param>
        /// <param name="green">The green component as a byte value.</param>
        /// <param name="blue">The blue component as a byte value.</param>
        /// <returns>A 32 bits color value.</returns>
        public static uint ToColor(byte red, byte green, byte blue)
        {
            return (uint)red << 16 | (uint)green << 8 | (uint)blue;
        }

        /// <summary>
        /// Extracts the red component of a 32 bits color value.
        /// </summary>
        /// <param name="color">The 32 bits color value.</param>
        /// <returns>The red component of the color.</returns>
        public static byte GetRed(uint color)
        {
            return (byte)((color >> 16) & 0xFF);
        }

        /// <summary>
        /// Extracts the green component of a 32 bits color value.
        /// </summary>
        /// <param name="color">The 32 bits color value.</param>
        /// <returns>The green component of the color.</returns>
        public static byte GetGreen(uint color)
        {
            return (byte)((color >> 8) & 0xFF);
        }

        /// <summary>
        /// Extracts the blue component of a 32 bits color value.
        /// </summary>
        /// <param name="color">The 32 bits color value.</param>
        /// <returns>The blue component of the color.</returns>
        public static byte GetBlue(uint color)
        {
            return (byte)((color) & 0xFF);
        }

        /// <summary>
        /// Combines the two colors by selecting the highest value for each component.
        /// </summary>
        /// <param name="color1">The first color to combine.</param>
        /// <param name="color2">The second color to combine.</param>
        /// <returns></returns>
        public static uint CombineColors(uint color1, uint color2)
        {
            byte red = (byte)Mathf.Max(GetRed(color1), GetRed(color2));
            byte green = (byte)Mathf.Max(GetGreen(color1), GetGreen(color2));
            byte blue = (byte)Mathf.Max(GetBlue(color1), GetBlue(color2));
            return ToColor(red, green, blue);
        }

        /// <summary>
        /// Interpolates linearly between two colors each given for a specific timestamp.
        /// </summary>
        /// <param name="color1">The first color.</param>
        /// <param name="timestamp1">The timestamp for the first color.</param>
        /// <param name="color2">The second color.</param>
        /// <param name="timestamp2">The timestamp for the second color.</param>
        /// <param name="time">The time for which to calculate the color.</param>
        /// <returns>The color for the given time.</returns>
        public static uint InterpolateColors(uint color1, int timestamp1, uint color2, int timestamp2, int time)
        {
            // To stick to integer math, we'll scale the values
            int scaler = 1024;
            int scaledPercent = (time - timestamp1) * scaler / (timestamp2 - timestamp1);
            int scaledRed = GetRed(color1) * (scaler - scaledPercent) + GetRed(color2) * scaledPercent;
            int scaledGreen = GetGreen(color1) * (scaler - scaledPercent) + GetGreen(color2) * scaledPercent;
            int scaledBlue = GetBlue(color1) * (scaler - scaledPercent) + GetBlue(color2) * scaledPercent;
            return ToColor((byte)(scaledRed / scaler), (byte)(scaledGreen / scaler), (byte)(scaledBlue / scaler));
        }

        /// <summary>
        /// Interpolates linearly the two intensities each given for a specific timestamp.
        /// </summary>
        /// <param name="intensity1">The first intensity value.</param>
        /// <param name="timestamp1">The timestamp for the first intensity.</param>
        /// <param name="intensity2">The second intensity value.</param>
        /// <param name="timestamp2">The timestamp for the second intensity.</param>
        /// <param name="time">The time for which to calculate the intensity.</param>
        /// <returns>The intensity for the given time.</returns>
        public static byte InterpolateIntensity(byte intensity1, int timestamp1, byte intensity2, int timestamp2, int time)
        {
            int scaler = 1024;
            int scaledPercent = (time - timestamp1) * scaler / (timestamp2 - timestamp1);
            return (byte)((intensity1 * (scaler - scaledPercent) + intensity2 * scaledPercent) / scaler);
        }

        /// <summary>
        /// Modulates the color with the given intensity. The later is a value
        /// between 0 (black) and (white).
        /// </summary>
        /// <param name="color">The color to modulate.</param>
        /// <param name="intensity">The intensity to apply.</param>
        /// <returns></returns>
        public static uint ModulateColor(uint color, byte intensity)
        {
            int red = GetRed(color) * intensity / 255;
            int green = GetGreen(color) * intensity / 255;
            int blue = GetBlue(color) * intensity / 255;
            return ToColor((byte)red, (byte)green, (byte)blue);
        }

        /// <summary>
        /// Returns a color along the following looped color blending:
        /// [position = 0] red -> green -> blue -> red [position = 255].
        /// </summary>
        /// <param name="position">Position on the rainbow wheel.</param>
        /// <param name="intensity">Intensity of the returned color.</param>
        /// <returns>A color.</returns>
        public static uint RainbowWheel(byte position, byte intensity)
        {
            if (position < 85)
            {
                return ToColor((byte)(position * 3 * intensity / 255), (byte)((255 - position * 3) * intensity / 255), 0);
            }
            else if (position < 170)
            {
                position -= 85;
                return ToColor((byte)((255 - position * 3) * intensity / 255), 0, (byte)(position * 3 * intensity / 255));
            }
            else
            {
                position -= 170;
                return ToColor(0, (byte)(position * 3 * intensity / 255), (byte)((255 - position * 3) * intensity / 255));
            }
        }
    }
}
