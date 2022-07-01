using System.Collections;
using System.Collections.Generic;
using Systemic.Unity.Pixels.Animations;
using UnityEngine;

[System.Serializable]
public class EditAnimationRainbow
    : EditAnimation
{
    [Slider, FloatRange(0.1f, 30.0f, 0.1f), Units("sec")]
    public override float duration { get; set; }
    [FaceMask, IntRange(0, 19), Name("Face Mask")]
    public int faces = 0xFFFFF;
    [Index, IntRange(1, 10), Name("Repeat Count")]
    public int count = 1;
    [Slider]
    [FloatRange(0.1f, 1.0f), Name("Fading Sharpness")]
    public float fade = 0.1f;
    [Name("Traveling Order")]
    public bool traveling = true;

    public override AnimationType type { get { return AnimationType.Rainbow; } }
    public override IAnimation ToAnimation(EditDataSet editSet, DataSet.AnimationBits bits)
    {
        return new AnimationRainbow
        {
            duration = (ushort)(duration * 1000.0f),
            faceMask = (uint)faces,
            fade = (byte)(255.0f * fade),
            count = (byte)count,
            traveling = traveling ? (byte)1 : (byte)0,
        };
    }

    public override EditAnimation Duplicate()
    {
        return new EditAnimationRainbow
        {
            name = name,
            duration = duration,
            faces = faces,
            fade = fade,
            count = count,
            traveling = traveling,
        };
    }
}
