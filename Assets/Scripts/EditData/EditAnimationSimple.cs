using System.Collections;
using System.Collections.Generic;
using Systemic.Unity.Pixels.Animations;
using UnityEngine;

[System.Serializable]
public class EditAnimationSimple
    : EditAnimation
{
    [Slider, FloatRange(0.1f, 30.0f, 0.1f), Units("sec")]
    public override float duration { get; set; }
    [FaceMask, IntRange(0, 19), Name("Face Mask")]
    public int faces = 0xFFFFF;
    public EditColor color = EditColor.FromColor(new Color32(0xFF, 0x30, 0x00, 0xff));
    [Index, IntRange(1, 10), Name("Repeat Count")]
    public int count = 1;
    [Slider]
    [FloatRange(0.1f, 1.0f), Name("Fading Sharpness")]
    public float fade = 0.1f;

    public override AnimationType type { get { return AnimationType.Simple; } }
    public override IAnimationPreset ToAnimation(EditDataSet editSet, DataSet.AnimationBits bits)
    {
        return new AnimationSimple
        {
            duration = (ushort)(duration * 1000.0f),
            faceMask = (uint)faces,
            colorIndex = (ushort)color.toColorIndex(ref bits.palette),
            fade = (byte)(255.0f * fade),
            count = (byte)count
        };
    }

    public override EditAnimation Duplicate()
    {
        return new EditAnimationSimple
        {
            name = name,
            duration = duration,
            faces = faces,
            color = color,
            count = count
        };
    }
}
