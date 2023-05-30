using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Systemic.Unity.Pixels.Animations;

[System.Serializable]
public class EditAnimationGradientPattern
    : EditAnimation
{
    [Slider, FloatRange(0.1f, 30.0f, 0.1f), Units("sec")]
    public override float duration { get; set; }

    [GreyscalePattern, Name("Greyscale Design")]
    public EditPattern pattern = new EditPattern();
    [Gradient]
    public EditRGBGradient gradient = new EditRGBGradient();
    [Name("Override Color Based On Face Up")]
    public bool overrideWithFace = false;

    public override AnimationType type => AnimationType.GradientPattern;

    public override IAnimationPreset ToAnimation(EditDataSet editSet, DataSet.AnimationBits bits)
    {
        // Add gradient
        int gradientTrackOffset = bits.rgbTracks.Count;
        var gradientTrack = new EditRGBTrack(gradient).ToTrack(editSet, bits);
        bits.rgbTracks.Add(gradientTrack);

        return new AnimationGradientPattern
        {
            duration = (ushort)(duration * 1000), // stored in milliseconds
            tracksOffset = (ushort)editSet.getPatternTrackOffset(pattern),
            trackCount = (ushort)pattern?.gradients.Count,
            gradientTrackOffset = (ushort)gradientTrackOffset,
            overrideWithFace = (byte)(overrideWithFace ? 1 : 0),
        };
    }

    public override EditAnimation Duplicate()
    {
        return new EditAnimationGradientPattern
        {
            name = name,
            pattern = pattern,
            gradient = gradient?.Duplicate(),
            overrideWithFace = overrideWithFace,
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
        asRGB = false;
        return this.pattern == pattern;
    }

    /// <summary>
    /// Specialized converter
    /// </sumary>
    public class Converter
        : JsonConverter<EditAnimationGradientPattern>
    {
        readonly AppDataSet dataSet;
        public Converter(AppDataSet dataSet)
        {
            this.dataSet = dataSet;
        }
        public override void WriteJson(JsonWriter writer, EditAnimationGradientPattern value, JsonSerializer serializer)
        {
            using (new IgnoreThisConverter(serializer, this))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("name");
                serializer.Serialize(writer, value.name);
                var patternIndex = dataSet.patterns.IndexOf(value.pattern);
                writer.WritePropertyName("patternIndex");
                serializer.Serialize(writer, patternIndex);
                writer.WritePropertyName("duration");
                serializer.Serialize(writer, value.duration);
                writer.WritePropertyName("gradient");
                serializer.Serialize(writer, value.gradient);
                writer.WritePropertyName("overrideWithFace");
                serializer.Serialize(writer, value.overrideWithFace);
                writer.WriteEndObject();
            }
        }

        public override EditAnimationGradientPattern ReadJson(JsonReader reader, System.Type objectType, EditAnimationGradientPattern existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (hasExistingValue)
                throw (new System.NotImplementedException());

            using (new IgnoreThisConverter(serializer, this))
            {
                JObject jsonObject = JObject.Load(reader);
                var ret = new EditAnimationGradientPattern();
                ret.name = jsonObject["name"].Value<string>();
                int patternIndex = jsonObject.ContainsKey("patternIndex") ? jsonObject["patternIndex"].Value<int>() : -1;
                if (patternIndex >= 0 && patternIndex < dataSet.patterns.Count)
                    ret.pattern = dataSet.patterns[patternIndex];
                else
                    ret.pattern = AppDataSet.Instance.AddNewDefaultPattern();
                //float speedMultiplier = jsonObject["speedMultiplier"].Value<float>();
                //ret.duration = (ret.pattern?.duration ?? 0) * speedMultiplier;
                ret.duration = jsonObject["duration"].Value<float>();
                ret.gradient = jsonObject["gradient"].ToObject<EditRGBGradient>();
                if (jsonObject["overrideWithFace"] != null)
                {
                    ret.overrideWithFace = jsonObject["overrideWithFace"].Value<bool>();
                }
                return ret;
            }
        }
    }
}
