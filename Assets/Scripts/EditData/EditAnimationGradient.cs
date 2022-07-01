using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Systemic.Unity.Pixels.Animations;

[System.Serializable]
public class EditAnimationGradient
    : EditAnimation
{
    [Slider, FloatRange(0.1f, 30.0f, 0.1f), Units("sec")]
    public override float duration { get; set; }
    [FaceMask, IntRange(0, 19), Name("Face Mask")]
    public int faces = 0xFFFFF;
    [Gradient]
    public EditRGBGradient gradient = new EditRGBGradient();

    public override AnimationType type => AnimationType.Gradient;

    public override IAnimation ToAnimation(EditDataSet editSet, DataSet.AnimationBits bits)
    {
        // Add gradient
        int gradientTrackOffset = bits.rgbTracks.Count;
        var gradientTrack = new EditRGBTrack(gradient).ToTrack(editSet, bits);
        bits.rgbTracks.Add(gradientTrack);

        return new AnimationGradient
        {
            duration = (ushort)(duration * 1000.0f),
            faceMask = (uint)faces,
            gradientTrackOffset = (ushort)gradientTrackOffset,
        };
    }

    public override EditAnimation Duplicate()
    {
        return new EditAnimationGradient
        {
            name = name,
            duration = duration,
            faces = faces,
            gradient = gradient.Duplicate(),
        };
    }
}
