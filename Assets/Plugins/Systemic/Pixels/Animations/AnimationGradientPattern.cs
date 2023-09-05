using System.Runtime.InteropServices;
using UnityEngine;

namespace Systemic.Unity.Pixels.Animations
{
    /// <summary>
    /// Represents of a series of RGB keyframes which together make
    /// an animation curve for a light intensity.
    /// size: 8 bytes (+ the actual keyframe data)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public struct Track
    {
        public ushort keyframesOffset;  // Offset into a global keyframe buffer
        public byte keyFrameCount;      // Keyframe count
        public byte padding;
        public uint ledMask;            // Each bit indicates whether the led is included in the animation track

        /// <summary>
        /// Gets the track duration.
        /// </summary>
        /// <param name="bits">The animation bits with the keyframes data.</param>
        /// <returns>The track duration.</returns>
        public ushort getDuration(DataSet.AnimationBits bits)
        {
            var kf = bits.getRGBKeyframe((ushort)(keyframesOffset + keyFrameCount - 1));
            return kf.time;
        }

        /// <summary>
        /// Gets the data of the keyframe at the given index.
        /// </summary>
        /// <param name="bits">The animation bits with the keyframes data.</param>
        /// <param name="keyframeIndex">The index of the keyframe.</param>
        /// <returns>The keyframe data.</returns>
        public SimpleKeyframe getKeyframe(DataSet.AnimationBits bits, ushort keyframeIndex)
        {
            Debug.Assert(keyframeIndex < keyFrameCount);
            return bits.getKeyframe((ushort)(keyframesOffset + keyframeIndex));
        }

        /// <summary>
        /// Evaluate an animation track's for a given time, in milliseconds, and fills returns arrays of led indices and colors.
        /// The returned colors are the given color modulated with the light intensity of the track for the given time.
        /// Values outside the track's range are clamped to first or last keyframe value.
        /// </summary>
        /// <param name="bits">The animation bits with the keyframes data and color palette.</param>
        /// <param name="color">The color for which to modulate the intensity.</param>
        /// <param name="time">The time at which to evaluate the track.</param>
        /// <param name="retIndices">Array of LED indices to be updated.</param>
        /// <param name="retColors">Array of 32 bits colors to be updated.</param>
        /// <returns>The number of LED indices that have been set in the returned arrays.</returns>
        public int evaluate(DataSet.AnimationBits bits, uint color, int time, int[] retIndices, uint[] retColors)
        {
            if (keyFrameCount == 0)
                return 0;

            uint mcolor = ColorUIntUtils.ModulateColor(color, evaluateIntensity(bits, time));

            // Fill the return arrays
            int currentCount = 0;
            for (int i = 0; i < Constants.MaxLEDsCount; ++i)
            {
                if ((ledMask & (1 << i)) != 0)
                {
                    retIndices[currentCount] = i;
                    retColors[currentCount] = mcolor;
                    currentCount++;
                }
            }
            return currentCount;
        }

        /// <summary>
        /// Evaluate an animation track's light intensity for a given time, in milliseconds.
        /// Values outside the track's range are clamped to first or last keyframe value.
        /// </summary>
        /// <param name="bits">The animation bits with the keyframes data and color palette.</param>
        /// <param name="color">The color to modulate.</param>
        /// <param name="time">The time at which to evaluate the track.</param>
        /// <returns>The modulated color.</returns>
        public byte evaluateIntensity(DataSet.AnimationBits bits, int time)
        {
            // Find the first keyframe
            int nextIndex = 0;
            while (nextIndex < keyFrameCount && getKeyframe(bits, (ushort)nextIndex).time < time)
            {
                nextIndex++;
            }

            if (nextIndex == 0)
            {
                // The first keyframe is already after the requested time, clamp to first value
                return getKeyframe(bits, (ushort)nextIndex).intensity;
            }
            else if (nextIndex == keyFrameCount)
            {
                // The last keyframe is still before the requested time, clamp to the last value
                return getKeyframe(bits, (ushort)(nextIndex - 1)).intensity;
            }
            else
            {
                // Grab the prev and next keyframes
                var nextKeyframe = getKeyframe(bits, (ushort)nextIndex);
                ushort nextKeyframeTime = nextKeyframe.time;
                byte nextKeyframeIntensity = nextKeyframe.intensity;

                var prevKeyframe = getKeyframe(bits, (ushort)(nextIndex - 1));
                ushort prevKeyframeTime = prevKeyframe.time;
                byte prevKeyframeIntensity = prevKeyframe.intensity;

                // Compute the interpolation parameter
                return ColorUIntUtils.InterpolateIntensity(prevKeyframeIntensity, prevKeyframeTime, nextKeyframeIntensity, nextKeyframeTime, time);
            }
        }

        /// <summary>
        /// Extracts the LED indices from the LED bit mask.
        /// </summary>
        /// <param name="retIndices">Array of LED indices to be updated.</param>
        /// <returns>The number of LED indices that have been set in the returned arrays.</returns>
        public int extractLEDIndices(int[] retIndices)
        {
            // Fill the return arrays
            int currentCount = 0;
            for (int i = 0; i < Constants.MaxLEDsCount; ++i)
            {
                if ((ledMask & (1 << i)) != 0)
                {
                    retIndices[currentCount] = i;
                    currentCount++;
                }
            }
            return currentCount;
        }

        /// <summary>
        /// Compares two Track instances.
        /// </summary>
        /// <param name="other">The Track instance to compare with.</param>
        /// <returns>Whether the two Track instances have the same data.</returns>
        public bool Equals(Track other)
        {
            return keyframesOffset == other.keyframesOffset && keyFrameCount == other.keyFrameCount && ledMask == other.ledMask;
        }
    }

    /// <summary>
    /// A keyframe-based animation with a gradient applied over
    /// size: 8 bytes (+ actual track and keyframe data)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class AnimationGradientPattern
        : IAnimationPreset
    {
        public AnimationType type { get; set; } = AnimationType.GradientPattern;
        public AnimationFlags animFlags { get; set; } = AnimationFlags.None;
        public ushort duration { get; set; } // in ms

        public ushort tracksOffset; // Offset into a global buffer of tracks
        public ushort trackCount;
        public ushort gradientTrackOffset;
        public byte overrideWithFace;
        public byte overrideWithFacePadding;

        public AnimationInstance CreateInstance(DataSet.AnimationBits bits)
        {
            return new AnimationInstanceGradientPattern(this, bits);
        }
    };

    /// <summary>
    /// Keyframe-based animation instance data
    /// </summary>
    public class AnimationInstanceGradientPattern
        : AnimationInstance
    {
        uint rgb = 0;

        public AnimationInstanceGradientPattern(AnimationGradientPattern preset, DataSet.AnimationBits bits)
            : base(preset, bits)
        {
        }

        public override void start(int startTime)
        {
            base.start(startTime);
            var preset = getPreset();
            if (preset.overrideWithFace != 0)
            {
                rgb = animationBits.getColor32(Constants.PaletteColorFromFace);
            }
        }

        /// <summary>
        /// Computes the list of LEDs that need to be on, and what their intensities should be
        /// based on the different tracks of this animation.
        /// </summary>
		public override int updateLEDs(int ms, int[] retIndices, uint[] retColors)
        {
            int time = ms - startTime;
            var preset = getPreset();

            int trackTime = time * 1000 / preset.duration;

            // Figure out the color from the gradient
            var gradient = animationBits.getRGBTrack(preset.gradientTrackOffset);

            uint gradientColor = 0;
            if (preset.overrideWithFace != 0)
            {
                gradientColor = rgb;
            }
            else
            {
                gradientColor = gradient.evaluateColor(animationBits, trackTime);
            }

            // Each track will append its led indices and colors into the return array
            // The assumption is that led indices don't overlap between tracks of a single animation,
            // so there will always be enough room in the return arrays.
            int totalCount = 0;
            var indices = new int[Constants.MaxLEDsCount];
            var colors = new uint[Constants.MaxLEDsCount];
            for (int i = 0; i < preset.trackCount; ++i)
            {
                var track = animationBits.getTrack((ushort)(preset.tracksOffset + i));
                int count = track.evaluate(animationBits, gradientColor, trackTime, indices, colors);
                for (int j = 0; j < count; ++j)
                {
                    retIndices[totalCount + j] = indices[j];
                    retColors[totalCount + j] = colors[j];
                }
                totalCount += count;
            }
            return totalCount;
        }

        public override int stop(int[] retIndices)
        {
            var preset = getPreset();
            // Each track will append its led indices and colors into the return array
            // The assumption is that led indices don't overlap between tracks of a single animation,
            // so there will always be enough room in the return arrays.
            int totalCount = 0;
            var indices = new int[Constants.MaxLEDsCount];
            for (int i = 0; i < preset.trackCount; ++i)
            {
                var track = animationBits.getRGBTrack((ushort)(preset.tracksOffset + i));
                int count = track.extractLEDIndices(indices);
                for (int j = 0; j < count; ++j)
                {
                    retIndices[totalCount + j] = indices[j];
                }
                totalCount += count;
            }
            return totalCount;
        }

        public AnimationGradientPattern getPreset()
        {
            return (AnimationGradientPattern)animationPreset;
        }
    };
}
