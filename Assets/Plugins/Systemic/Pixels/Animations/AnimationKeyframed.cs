using System.Runtime.InteropServices;
using UnityEngine;

namespace Systemic.Unity.Pixels.Animations
{
    /// <summary>
    /// Represents of a series of RGB keyframes which together make
    /// an animation curve for an RGB color.
    /// size: 8 bytes (+ the actual keyframe data)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public struct RGBTrack
    {
        public ushort keyframesOffset;  // offset into a global keyframe buffer
        public byte keyFrameCount;      // Keyframe count
        public byte padding;
        public uint ledMask;            // Each bit indicates whether the led is included in the animation track

        /// <summary>
        /// Gets the track duration.
        /// </summary>
        /// <param name="bits">The animation bits with the RGB keyframes data.</param>
        /// <returns>The track duration.</returns>
        public ushort getDuration(DataSet.AnimationBits bits)
        {
            var kf = bits.getRGBKeyframe((ushort)(keyframesOffset + keyFrameCount - 1));
            return kf.time;
        }

        /// <summary>
        /// Gets the data of the RGB keyframe at the given index.
        /// </summary>
        /// <param name="bits">The animation bits with the RGB keyframes data.</param>
        /// <param name="keyframeIndex">The index of the keyframe.</param>
        /// <returns>The RGB keyframe data.</returns>
        public RGBKeyframe getKeyframe(DataSet.AnimationBits bits, ushort keyframeIndex)
        {
            Debug.Assert(keyframeIndex < keyFrameCount);
            return bits.getRGBKeyframe((ushort)(keyframesOffset + keyframeIndex));
        }

        /// <summary>
        /// Evaluate an animation track's for a given time, in milliseconds, and fills returns arrays of led indices and colors
        /// The returned colors are the color of the track for the given time.
        /// Values outside the track's range are clamped to first or last keyframe value.
        /// </summary>
        /// <param name="bits">The animation bits with the RGB keyframes data and color palette.</param>
        /// <param name="time">The time at which to evaluate the track.</param>
        /// <param name="retIndices">Array of LED indices to be updated.</param>
        /// <param name="retColors">Array of 32 bits colors to be updated.</param>
        /// <returns>The number of LED indices that have been set in the returned arrays.</returns>
        public int evaluate(DataSet.AnimationBits bits, int time, int[] retIndices, uint[] retColors)
        {
            if (keyFrameCount == 0)
                return 0;

            uint color = evaluateColor(bits, time);

            // Fill the return arrays
            int currentCount = 0;
            for (int i = 0; i < Constants.MaxLEDsCount; ++i)
            {
                if ((ledMask & (1 << i)) != 0)
                {
                    retIndices[currentCount] = i;
                    retColors[currentCount] = color;
                    currentCount++;
                }
            }
            return currentCount;
        }

        /// <summary>
        /// Evaluate an animation track's color for a given time, in milliseconds
        /// Values outside the track's range are clamped to first or last keyframe value.
        /// </summary>
        public uint evaluateColor(DataSet.AnimationBits bits, int time)
        {
            if (keyFrameCount == 0)
            {
                return 0;
            }

            // Find the first keyframe
            int nextIndex = 0;
            while (nextIndex < keyFrameCount && getKeyframe(bits, (ushort)nextIndex).time < time)
            {
                nextIndex++;
            }

            uint color = 0;
            if (nextIndex == 0)
            {
                // The first keyframe is already after the requested time, clamp to first value
                color = getKeyframe(bits, (ushort)nextIndex).getColor(bits);
            }
            else if (nextIndex == keyFrameCount)
            {
                // The last keyframe is still before the requested time, clamp to the last value
                color = getKeyframe(bits, (ushort)(nextIndex - 1)).getColor(bits);
            }
            else
            {
                // Grab the prev and next keyframes
                var nextKeyframe = getKeyframe(bits, (ushort)nextIndex);
                ushort nextKeyframeTime = nextKeyframe.time;
                uint nextKeyframeColor = nextKeyframe.getColor(bits);

                var prevKeyframe = getKeyframe(bits, (ushort)(nextIndex - 1));
                ushort prevKeyframeTime = prevKeyframe.time;
                uint prevKeyframeColor = prevKeyframe.getColor(bits);

                // Compute the interpolation parameter
                color = ColorUIntUtils.InterpolateColors(prevKeyframeColor, prevKeyframeTime, nextKeyframeColor, nextKeyframeTime, time);
            }

            return color;
        }

        /// <summary>
        /// Extracts the LED indices from the LED bit mask
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
        /// Compares two RgbTrack instances.
        /// </summary>
        /// <param name="other">The RgbTrack instance to compare with.</param>
        /// <returns>Whether the two RgbTrack instances have the same data.</returns>
        public bool Equals(RGBTrack other)
        {
            return keyframesOffset == other.keyframesOffset && keyFrameCount == other.keyFrameCount && ledMask == other.ledMask;
        }
    }

    /// <summary>
    /// A keyframe-based animation
    /// size: 8 bytes (+ actual track and keyframe data)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class AnimationKeyframed
        : IAnimationPreset
    {
        public AnimationType type { get; set; } = AnimationType.Keyframed;
        public byte padding_type { get; set; } // to keep duration 16-bit aligned
        public ushort duration { get; set; } // in ms

        public ushort tracksOffset; // Offset into a global buffer of tracks
        public ushort trackCount;
        public byte flowOrder; // boolean, if true the indices are led indices, not face indices
        public byte paddingOrder;

        public AnimationInstance CreateInstance(DataSet.AnimationBits bits)
        {
            return new AnimationInstanceKeyframed(this, bits);
        }
    };

    /// <summary>
    /// Keyframe-based animation instance data
    /// </summary>
    public class AnimationInstanceKeyframed
        : AnimationInstance
    {
        public AnimationInstanceKeyframed(AnimationKeyframed preset, DataSet.AnimationBits bits)
            : base(preset, bits)
        {
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

            // Each track will append its led indices and colors into the return array
            // The assumption is that led indices don't overlap between tracks of a single animation,
            // so there will always be enough room in the return arrays.
            int totalCount = 0;
            var indices = new int[Constants.MaxLEDsCount];
            var colors = new uint[Constants.MaxLEDsCount];
            for (int i = 0; i < preset.trackCount; ++i)
            {
                var track = animationBits.getRGBTrack((ushort)(preset.tracksOffset + i));
                int count = track.evaluate(animationBits, trackTime, indices, colors);
                for (int j = 0; j < count; ++j)
                {
                    if (preset.flowOrder != 0)
                    {
                        // Use reverse lookup so that the indices are actually led Indices, not face indices
                        retIndices[totalCount + j] = Constants.getFaceIndex(indices[j]);
                    }
                    else
                    {
                        retIndices[totalCount + j] = indices[j];
                    }
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

        public AnimationKeyframed getPreset()
        {
            return (AnimationKeyframed)animationPreset;
        }
    };
}
