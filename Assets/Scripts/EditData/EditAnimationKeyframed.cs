using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Systemic.Unity.Pixels.Animations;

/// <summary>
/// Simple list of keyframes for a led
/// </summary>
[System.Serializable]
public class EditRGBTrack
{
    public readonly List<int> ledIndices;
    public readonly EditRGBGradient gradient;

    public bool empty => gradient.empty;
    public float duration => gradient.duration;
    public float firstTime => gradient.firstTime;
    public float lastTime => gradient.lastTime;

    public EditRGBTrack(EditRGBGradient gradient, List<int> ledIndices = null)
    {
        this.gradient = gradient ?? new EditRGBGradient();
        this.ledIndices = ledIndices ?? new List<int>();
    }

    public EditRGBTrack Duplicate()
    {
        return new EditRGBTrack(gradient.Duplicate(), new List<int>(ledIndices));
    }

    public RGBTrack ToTrack(EditDataSet editSet, DataSet.AnimationBits bits)
    {
        // Add the keyframes
        int keyframesOffset = bits.rgbKeyframes.Count;
        foreach (var editKeyframe in gradient.keyframes)
        {
            var kf = editKeyframe.ToRGBKeyframe(editSet, bits);
            bits.rgbKeyframes.Add(kf);
        }

        return new RGBTrack
        {
            keyframesOffset = (ushort)keyframesOffset,
            keyFrameCount = (byte)gradient.keyframes.Count,
            ledMask = (uint)ledIndices.Sum(index => 1 << index)
        };
    }
}

[System.Serializable]
public class EditAnimationKeyframed
    : EditAnimation
{
    public float speedMultiplier = 1.0f;
    [Slider, FloatRange(0.1f, 30.0f, 0.1f), Units("sec")]
    public override float duration
    {
        get
        {
            return pattern?.duration ?? 0 * speedMultiplier;
        }
        set
        {
            if (pattern != null)
            {
                speedMultiplier = value / pattern.duration;
            }
        }
    }
    [RGBPattern, Name("LED Pattern")]
    public EditPattern pattern = new EditPattern();
    [Name("Traveling Order")]
    public bool flowOrder = false;

    //[Slider, FloatRange(-0.5f, 0.5f), Name("Hue Adjustment")]
    //public float hueAdjust = 0.0f;

    public override AnimationType type => AnimationType.Keyframed;

    public override IAnimation ToAnimation(EditDataSet editSet, DataSet.AnimationBits bits)
    {
        //// Copy the pattern so we can adjust the hue of the keyframes
        //var patternCopy = pattern.Duplicate();
        //foreach (var t in patternCopy.gradients)
        //{
        //    foreach (var k in t.keyframes)
        //    {
        //        float h, s, v;
        //        Color.RGBToHSV(k.color, out h, out s, out v);
        //        h = Mathf.Repeat(h + hueAdjust, 1.0f);
        //        k.color = Color.HSVToRGB(h, s, v);
        //    }
        //}
        //var tracks = patternCopy.ToRGBTracks(editSet, bits);
        return new AnimationKeyframed
        {
            duration = (ushort)(duration * 1000), // stored in milliseconds
            speedMultiplier256 = (ushort)(speedMultiplier * 256.0f),
            tracksOffset = (ushort)editSet.getPatternRGBTrackOffset(pattern),
            trackCount = (ushort)pattern.gradients.Count,
            flowOrder = flowOrder ? (byte)1 : (byte)0,
        };
    }

    public override EditAnimation Duplicate()
    {
        return new EditAnimationKeyframed
        {
            name = name,
            pattern = pattern,
            flowOrder = flowOrder,
            speedMultiplier = speedMultiplier,
            duration = duration,
            //hueAdjust = hueAdjust;
        };
    }

    public override void ReplacePattern(EditPattern oldPattern, EditPattern newPattern)
    {
        if (pattern == oldPattern)
        {
            pattern = newPattern;
        }
    }
    public override bool DependsOnPattern(EditPattern pattern, out bool asRGB)
    {
        asRGB = true;
        return this.pattern == pattern;
    }

    /// <summary>
    /// Specialized converter
    /// </sumary>
    public class Converter
        : JsonConverter<EditAnimationKeyframed>
    {
        AppDataSet dataSet;
        public Converter(AppDataSet dataSet)
        {
            this.dataSet = dataSet;
        }
        public override void WriteJson(JsonWriter writer, EditAnimationKeyframed value, JsonSerializer serializer)
        {
            using (new IgnoreThisConverter(serializer, this))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("name");
                serializer.Serialize(writer, value.name);
                var patternIndex = dataSet.patterns.IndexOf(value.pattern);
                writer.WritePropertyName("patternIndex");
                serializer.Serialize(writer, patternIndex);
                writer.WritePropertyName("speedMultiplier");
                serializer.Serialize(writer, value.speedMultiplier);
                writer.WritePropertyName("duration");
                serializer.Serialize(writer, value.duration);
                //writer.WritePropertyName("hueAdjust");
                //serializer.Serialize(writer, value.hueAdjust);
                writer.WriteEndObject();
            }
        }

        public override EditAnimationKeyframed ReadJson(JsonReader reader, System.Type objectType, EditAnimationKeyframed existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (hasExistingValue)
                throw (new System.NotImplementedException());

            using (new IgnoreThisConverter(serializer, this))
            {
                JObject jsonObject = JObject.Load(reader);
                var ret = new EditAnimationKeyframed();
                ret.name = jsonObject["name"].Value<string>();
                int patternIndex = jsonObject.ContainsKey("patternIndex") ? jsonObject["patternIndex"].Value<int>() : -1;
                if (patternIndex >= 0 && patternIndex < dataSet.patterns.Count)
                    ret.pattern = dataSet.patterns[patternIndex];
                else
                    ret.pattern = AppDataSet.Instance.AddNewDefaultPattern();
                ret.speedMultiplier = jsonObject["speedMultiplier"].Value<float>();
                ret.duration = jsonObject["duration"].Value<float>();
                //ret.hueAdjust = jsonObject["hueAdjust"].Value<float>();
                return ret;
            }
        }
    }
}
