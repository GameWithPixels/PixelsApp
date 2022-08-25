using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class ColorUtils
{
    public static float desaturate(Color color)
    {
        return  (Mathf.Min(color.r, Mathf.Min(color.g, color.b)) + Mathf.Max(color.r, Mathf.Max(color.g, color.b))) * 0.5f;
    }

    public static float computeSqrColorDistance(Color color1, Color color2)
    {
        return
            (color1.r - color2.r) * (color1.r - color2.r) +
            (color1.g - color2.g) * (color1.g - color2.g) +
            (color1.b - color2.b) * (color1.b - color2.b);
    }


    public static List<EditRGBKeyframe> extractKeyframes(Color[] pixels)
    {
        var ret = new List<EditRGBKeyframe>();
        
        float computeInterpolationError(int firstIndex, int lastIndex) {
            Color startColor = pixels[firstIndex];
            Color endColor = pixels[lastIndex];
            float sumError = 0.0f;
            for (int i = firstIndex; i <= lastIndex; ++i) {
                float pct = (float)(i - firstIndex) / (lastIndex - firstIndex);
                sumError += computeSqrColorDistance(pixels[i], Color.Lerp(startColor, endColor, pct));
            }
            return sumError;
        }

        float computePixelTime(int pixelIndex) {
            // KeyframeTimeResolutionMs is the smallest time increment in the keyframe data
            return (float)pixelIndex * Systemic.Unity.Pixels.Animations.Constants.KeyframeTimeResolutionMs * 0.001f;
        }

        // Always add the first color
        ret.Add(new EditRGBKeyframe()
        {
            time = 0,
            color = pixels[0]
        });

        const float sqrEpsilon = 0.2f;

        int currentPrev = 0;
        int currentNext = 1;
        while (currentNext < pixels.Length) {
            while (currentNext < pixels.Length && computeInterpolationError(currentPrev, currentNext) < sqrEpsilon) {
                currentNext++;
            }

            // Too much error, add a keyframe
            ret.Add(new EditRGBKeyframe()
            {
                time = computePixelTime(currentNext-1),
                color = pixels[currentNext-1]
            });

            // Next segment
            currentPrev = currentNext-1;
        }

        return ret;
    }
}
