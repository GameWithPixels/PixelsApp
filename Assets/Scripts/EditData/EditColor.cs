using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Systemic.Unity.Pixels;
using Systemic.Unity.Pixels.Animations;

[System.Serializable]
public struct EditColor
{
    public enum ColorType
    {
        RGB = 0,
        Face,
        Random
    }

    public ColorType type;
    public Color32 rgbColor; // Used when type is ColorType.RGB

    [JsonIgnore]
    public Color32 asColor32
    {
        get
        {
            switch (type)
            {
                case ColorType.RGB:
                    return rgbColor;
                case ColorType.Face:
                case ColorType.Random:
                default:
                    throw new System.NotImplementedException();
            }
        }
    }

    public static EditColor FromColor(Color color)
    {
        return new EditColor { type = ColorType.RGB, rgbColor = color };
    }

    public uint toColorIndex(ref List<Color> palette)
    {
        switch (type)
        {
            case ColorType.RGB:
                return EditColor.toColorIndex(ref palette, rgbColor);
            case ColorType.Face:
                return Constants.PaletteColorFromFace;
            case ColorType.Random:
                return Constants.PaletteColorFromRandom;
            default:
                throw new System.NotImplementedException();
        }
    }

    public static uint toColorIndex(ref List<Color> palette, Color rgbColor)
    {
        var rgb = GammaUtils.Gamma(rgbColor);
        int colorIndex = palette.IndexOf(rgb);
        if (colorIndex == -1)
        {
            colorIndex = palette.Count;
            palette.Add(rgb);
        }
        return (uint)colorIndex;
    }
}
