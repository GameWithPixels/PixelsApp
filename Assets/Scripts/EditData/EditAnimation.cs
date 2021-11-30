﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Systemic.Unity.Pixels.Animations;
using Systemic.Unity.Pixels;

/// <summary>
/// An animation is a list of tracks!
/// </summary>
[System.Serializable]
public abstract class EditAnimation
    : EditObject
{
    public string name;
	public abstract float duration { get; set; }
    public PreviewSettings defaultPreviewSettings = new PreviewSettings() { design = PixelDesignAndColor.V5_Grey };

    [JsonIgnore]
    public abstract AnimationType type { get; }

    public abstract IAnimation ToAnimation(EditDataSet editSet, DataSet.AnimationBits bits);
    public abstract EditAnimation Duplicate();
    public virtual void ReplacePattern(EditPattern oldPattern, EditPattern newPattern)
    {
        // Base does nothing
    }
    public virtual bool DependsOnPattern(EditPattern pattern, out bool asRGB)
    {
        // Base does not
        asRGB = false;
        return false;
    }

    public static EditAnimation Create(AnimationType type)
    {
        switch (type)
        {
            case AnimationType.Simple:
                return new EditAnimationSimple();
            case AnimationType.Gradient:
                return new EditAnimationGradient();
            case AnimationType.Keyframed:
                return new EditAnimationKeyframed();
            case AnimationType.Rainbow:
                return new EditAnimationRainbow();
            case AnimationType.GradientPattern:
                return new EditAnimationGradientPattern();
            default:
                throw new System.Exception("Unknown animation type");
        }
    }

    public static System.Type GetAnimationType(AnimationType type)
    {
        switch (type)
        {
            case AnimationType.Simple:
                return typeof(EditAnimationSimple);
            case AnimationType.Gradient:
                return typeof(EditAnimationGradient);
            case AnimationType.Keyframed:
                return typeof(EditAnimationKeyframed);
            case AnimationType.Rainbow:
                return typeof(EditAnimationRainbow);
            case AnimationType.GradientPattern:
                return typeof(EditAnimationGradientPattern);
            default:
                throw new System.Exception("Unknown animation type");
        }
    }
}

public class EditAnimationConverter
    : JsonConverter<EditAnimation>
{
    public override void WriteJson(JsonWriter writer, EditAnimation value, JsonSerializer serializer)
    {
        using (new IgnoreThisConverter(serializer, this))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            serializer.Serialize(writer, value.type);
            writer.WritePropertyName("data");
            serializer.Serialize(writer, value);
            writer.WriteEndObject();
        }
    }

    public override EditAnimation ReadJson(JsonReader reader, System.Type objectType, EditAnimation existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (hasExistingValue)
            throw (new System.NotImplementedException());

        using (new IgnoreThisConverter(serializer, this))
        {
            JObject editAnimObject = JObject.Load(reader);
            var type = editAnimObject["type"].ToObject<AnimationType>();
            var ret = (EditAnimation)editAnimObject["data"].ToObject(EditAnimation.GetAnimationType(type), serializer);
            return ret;
        }
    }
}
