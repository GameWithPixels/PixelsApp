using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Systemic.Unity.Pixels.Animations;
using Systemic.Unity.Pixels;
using Newtonsoft.Json;

/// <summary>
/// Simple animation keyframe, time in seconds and color!
/// </summary>
[System.Serializable]
public class EditRGBKeyframe
{
    public float time = -1;
    public Color32 color;

    public EditRGBKeyframe Duplicate()
    {
        return new EditRGBKeyframe
        {
            time = time,
            color = color
        };
    }

    public RGBKeyframe ToRGBKeyframe(EditDataSet editSet, DataSet.AnimationBits bits)
    {
        var ret = new RGBKeyframe();
        // Add the color to the palette if not already there, otherwise grab the color index
        var colorIndex = EditColor.toColorIndex(ref bits.palette, color);
        ret.setTimeAndColorIndex(time, (ushort)colorIndex);
        return ret;
    }

    public SimpleKeyframe ToKeyframe(EditDataSet editSet, DataSet.AnimationBits bits)
    {
        var ret = new SimpleKeyframe();
        // Get the intensity from the color and scale
        ret.setTimeAndIntensity(time, (byte)(ColorUtils.Desaturate(color) * 255.0f));
        return ret;
    }

    public class EqualityComparer
        : IEqualityComparer<EditRGBKeyframe>
    {
        public bool Equals(EditRGBKeyframe x, EditRGBKeyframe y)
        {
            return x.time == y.time && x.color.Equals(y.color);
        }

        public int GetHashCode(EditRGBKeyframe obj)
        {
            return obj.time.GetHashCode() ^ obj.color.GetHashCode();
        }
    }
    public static EqualityComparer DefaultComparer = new EqualityComparer();
}

[System.Serializable]
public class EditRGBGradient
{
    public List<EditRGBKeyframe> keyframes = new List<EditRGBKeyframe>();

    [JsonIgnore]
    public bool empty => keyframes?.Count == 0;

    [JsonIgnore]
    public float duration => keyframes.Count == 0 ? 0 : keyframes.Max(k => k.time);

    [JsonIgnore]
    public float firstTime => keyframes.Count == 0 ? 0 : keyframes.First().time;

    [JsonIgnore]
    public float lastTime => keyframes.Count == 0 ? 0 : keyframes.Last().time;

    public EditRGBGradient Duplicate()
    {
        var track = new EditRGBGradient();
        if (keyframes != null)
        {
            track.keyframes = new List<EditRGBKeyframe>(keyframes.Count);
            foreach (var keyframe in keyframes)
            {
                track.keyframes.Add(keyframe.Duplicate());
            }
        }
        return track;
    }

}

/// <summary>
/// Simple list of keyframes for a led
/// </summary>
[System.Serializable]
public class EditPattern
{
    public string name = "LED Pattern";
    public readonly List<EditRGBGradient> gradients = new List<EditRGBGradient>();

    [JsonIgnore]
    public float duration => gradients.Count > 0 ? gradients.Max(g => g.duration) : 1.0f;

    public EditPattern Duplicate()
    {
        var track = new EditPattern();
        track.name = name;
        foreach (var g in gradients)
        {
            track.gradients.Add(g.Duplicate());
        }
        return track;
    }

    public RGBTrack[] ToRGBTracks(EditDataSet editSet, DataSet.AnimationBits bits)
    {
        var ret = new RGBTrack[gradients.Count];
        for (int i = 0; i < gradients.Count; ++i)
        {
            // Add the keyframes
            int keyframesOffset = bits.rgbKeyframes.Count;
            foreach (var editKeyframe in gradients[i].keyframes)
            {
                var kf = editKeyframe.ToRGBKeyframe(editSet, bits);
                bits.rgbKeyframes.Add(kf);
            }

            ret[i] = new RGBTrack
            {
                keyframesOffset = (ushort)keyframesOffset,
                keyFrameCount = (byte)gradients[i].keyframes.Count,
                ledMask = (uint)(1 << i),
            };
        }

        return ret;
    }

    public Track[] ToTracks(EditDataSet editSet, DataSet.AnimationBits bits)
    {
        var ret = new Track[gradients.Count];
        for (int i = 0; i < gradients.Count; ++i)
        {
            // Add the keyframes
            int keyframesOffset = bits.keyframes.Count;
            foreach (var editKeyframe in gradients[i].keyframes)
            {
                var kf = editKeyframe.ToKeyframe(editSet, bits);
                bits.keyframes.Add(kf);
            }

            ret[i] = new Track
            {
                keyframesOffset = (ushort)keyframesOffset,
                keyFrameCount = (byte)gradients[i].keyframes.Count,
                ledMask = (uint)(1 << i),
            };
        }

        return ret;
    }

    public void FromTexture(Texture2D texture)
    {
        gradients.Clear();
        for (int i = 0; i < texture.height; ++i)
        {
            var gradientPixels = texture.GetPixels(0, i, texture.width, 1, 0);
            var keyframes = ColorUtils.ExtractKeyframes(gradientPixels);
            var gradient = new EditRGBGradient() { keyframes = keyframes };
            gradients.Add(gradient);
        }
    }

    public Texture2D ToTexture()
    {
        Texture2D ret = null;
        float timeScale = 1000f / Constants.KeyframeTimeResolutionMs;
        int width = Mathf.RoundToInt(duration * timeScale);
        int height = gradients.Count;
        if (width > 0 && height > 0)
        {
            ret = new Texture2D(width, height, TextureFormat.ARGB32, false);
            ret.filterMode = FilterMode.Point;
            ret.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = ret.GetPixels();
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = Color.black;
            }
            for (int j = 0; j < gradients.Count; ++j)
            {
                var currentGradient = gradients[j];
                int x = 0, lastMax = 0;
                for (int i = 1; i < currentGradient.keyframes.Count; ++i)
                {
                    int max = Mathf.RoundToInt(currentGradient.keyframes[i].time * timeScale);
                    for (; x < max; ++x)
                    {
                        Color prevColor = currentGradient.keyframes[i - 1].color;
                        Color nextColor = currentGradient.keyframes[i].color;
                        pixels[j * ret.width + x] = Color.Lerp(prevColor, nextColor, ((float)x - lastMax) / (max - lastMax));
                    }
                    lastMax = max;
                }
            }
            ret.SetPixels(pixels);
            ret.Apply(false);
        }
        return ret;
    }

    public Texture2D ToGreyscaleTexture()
    {
        Texture2D ret = null;
        float timeScale = 1000f / Constants.KeyframeTimeResolutionMs;
        int width = Mathf.RoundToInt(duration * timeScale);
        int height = gradients.Count;
        if (width > 0 && height > 0)
        {
            ret = new Texture2D(width, height, TextureFormat.ARGB32, false);
            ret.filterMode = FilterMode.Point;
            ret.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = ret.GetPixels();
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = Color.black;
            }
            for (int j = 0; j < gradients.Count; ++j)
            {
                var currentGradient = gradients[j];
                int x = 0, lastMax = 0;
                for (int i = 1; i < currentGradient.keyframes.Count; ++i)
                {
                    int max = Mathf.RoundToInt(currentGradient.keyframes[i].time * timeScale);
                    for (; x < max; ++x)
                    {
                        float prevIntensity = ColorUtils.Desaturate(currentGradient.keyframes[i - 1].color);
                        float nextIntensity = ColorUtils.Desaturate(currentGradient.keyframes[i].color);
                        Color prevColor = new Color(prevIntensity, prevIntensity, prevIntensity);
                        Color nextColor = new Color(nextIntensity, nextIntensity, nextIntensity);
                        pixels[j * ret.width + x] = Color.Lerp(prevColor, nextColor, ((float)x - lastMax) / (max - lastMax));
                    }
                    lastMax = max;
                }
            }
            ret.SetPixels(pixels);
            ret.Apply(false);
        }
        return ret;
    }
}
