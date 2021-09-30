using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Systemic.Unity.Pixels.Animations
{
    /// <summary>
    /// Data Set is the set of all behaviors, conditions, rules, animations and colors
    /// stored in the memory of a Pixel die. This data gets transfered straight to the dice.
    /// For that purpose, the data is essentially 'exploded' into flat buffers. i.e. all
    /// the key-frames of all the animations are stored in a single key-frame array, and
    /// individual tracks reference 'their' key-frames using an offset and count into that array.
    /// </summary>
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public class DataSet
    {
        public const int MAX_COLOR_MAP_SIZE = (1 << 7);
        public const int MAX_PALETTE_SIZE = MAX_COLOR_MAP_SIZE * 3;
        public const int SPECIAL_COLOR_INDEX = (MAX_COLOR_MAP_SIZE - 1);

        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public class AnimationBits
        {
            public List<Color> palette = new List<Color>();
            public List<RGBKeyframe> rgbKeyframes = new List<RGBKeyframe>();
            public List<RGBTrack> rgbTracks = new List<RGBTrack>();
            public List<SimpleKeyframe> keyframes = new List<SimpleKeyframe>();
            public List<Track> tracks = new List<Track>();

            public uint getColor32(ushort colorIndex)
            {

                var cl32 = (Color32)(getColor(colorIndex));
                return ColorUIntUtils.ToColor(cl32.r, cl32.g, cl32.b);
            }

            public const ushort PALETTE_COLOR_FROM_FACE = 127;
            public const ushort PALETTE_COLOR_FROM_RANDOM = 126;

            public Color getColor(ushort colorIndex)
            {
                if (colorIndex == PALETTE_COLOR_FROM_FACE)
                {
                    return Color.blue;
                }
                else if (colorIndex == PALETTE_COLOR_FROM_RANDOM)
                {
                    return Color.black;
                }
                else
                {
                    return palette[colorIndex];
                }
            }
            public RGBKeyframe getRGBKeyframe(ushort keyFrameIndex) => rgbKeyframes[keyFrameIndex];
            public SimpleKeyframe getKeyframe(ushort keyFrameIndex) => keyframes[keyFrameIndex];
            public ushort getPaletteSize() => (ushort)(palette.Count * 3);
            public ushort getRGBKeyframeCount() => (ushort)rgbKeyframes.Count;
            public RGBTrack getRGBTrack(ushort trackIndex) => rgbTracks[trackIndex];
            public ushort getRGBTrackCount() => (ushort)rgbTracks.Count;
            public ushort getKeyframeCount() => (ushort)keyframes.Count;
            public Track getTrack(ushort trackIndex) => tracks[trackIndex];
            public ushort getTrackCount() => (ushort)tracks.Count;

            public int ComputeDataSize()
            {
                return roundUpTo4(palette.Count * Marshal.SizeOf<byte>() * 3) + // 3 bytes per color
                    rgbKeyframes.Count * Marshal.SizeOf<RGBKeyframe>() +
                    rgbTracks.Count * Marshal.SizeOf<RGBTrack>() +
                    keyframes.Count * Marshal.SizeOf<SimpleKeyframe>() +
                    tracks.Count * Marshal.SizeOf<Track>();
            }

            public System.IntPtr WriteBytes(System.IntPtr ptr)
            {
                // Copy palette
                System.IntPtr current = ptr;
                var currentCopy = current;
                foreach (var color in palette)
                {
                    Color32 cl32 = color;
                    Marshal.WriteByte(current, cl32.r);
                    current += 1;
                    Marshal.WriteByte(current, cl32.g);
                    current += 1;
                    Marshal.WriteByte(current, cl32.b);
                    current += 1;
                }

                // Round up to nearest multiple of 4
                current = currentCopy + roundUpTo4(palette.Count * 3 * Marshal.SizeOf<byte>());

                // Copy keyframes
                foreach (var keyframe in rgbKeyframes)
                {
                    Marshal.StructureToPtr(keyframe, current, false);
                    current += Marshal.SizeOf<RGBKeyframe>();
                }

                // Copy rgb tracks
                foreach (var track in rgbTracks)
                {
                    Marshal.StructureToPtr(track, current, false);
                    current += Marshal.SizeOf<RGBTrack>();
                }

                // Copy keyframes
                foreach (var keyframe in keyframes)
                {
                    Marshal.StructureToPtr(keyframe, current, false);
                    current += Marshal.SizeOf<SimpleKeyframe>();
                }

                // Copy tracks
                foreach (var track in tracks)
                {
                    Marshal.StructureToPtr(track, current, false);
                    current += Marshal.SizeOf<Track>();
                }

                return current;
            }
        }

        public AnimationBits animationBits = new AnimationBits();
        public List<IAnimation> animations = new List<IAnimation>();
        public List<Profiles.ICondition> conditions = new List<Profiles.ICondition>();
        public List<Profiles.IAction> actions = new List<Profiles.IAction>();
        public List<Profiles.Rule> rules = new List<Profiles.Rule>();
        public Profiles.Profile behavior = null;
        public ushort padding;

        public int ComputeDataSetDataSize()
        {
            return animationBits.ComputeDataSize() +
                roundUpTo4(animations.Count * Marshal.SizeOf<ushort>()) + // offsets
                animations.Sum((anim) => Marshal.SizeOf(anim.GetType())) + // actual animations
                roundUpTo4(conditions.Count * Marshal.SizeOf<ushort>()) + // offsets
                conditions.Sum((cond) => Marshal.SizeOf(cond.GetType())) + // actual conditions
                roundUpTo4(actions.Count * Marshal.SizeOf<ushort>()) + // offsets
                actions.Sum((action) => Marshal.SizeOf(action.GetType())) + // actual actions
                rules.Count * Marshal.SizeOf<Profiles.Rule>() +
                Marshal.SizeOf<Profiles.Profile>();
        }

        // D. J. Bernstein hash function
        public static uint ComputeHash(byte[] data)
        {
            uint hash = 5381;
            for (int i = 0; i < data.Length; ++i)
            {
                hash = 33 * hash ^ data[i];
            }
            return hash;
        }

        public uint ComputeHash()
        {
            byte[] dataSetDataBytes = ToByteArray();
            var hash = ComputeHash(dataSetDataBytes);

            //StringBuilder hexdumpBuilder = new StringBuilder("Profile hash: ");
            //hexdumpBuilder.Append(hash);
            //for (int i = 0; i < dataSetDataBytes.Length; ++i)
            //{
            //    if (i % 8 == 0)
            //    {
            //        hexdumpBuilder.AppendLine();
            //    }
            //    hexdumpBuilder.Append(dataSetDataBytes[i].ToString("X02") + " ");
            //}
            //Debug.Log(hexdumpBuilder.ToString());

            return hash;
        }

        public IAnimation getAnimation(ushort animIndex) => animations[animIndex];
        public ushort getAnimationCount() => (ushort)animations.Count;
        public Profiles.ICondition getCondition(int conditionIndex) => conditions[conditionIndex];
        public ushort getConditionCount() => (ushort)conditions.Count;
        public Profiles.IAction getAction(int actionIndex) => actions[actionIndex];
        public ushort getActionCount() => (ushort)actions.Count;
        public Profiles.Rule getRule(int ruleIndex) => rules[ruleIndex];
        public ushort getRuleCount() => (ushort)rules.Count;
        public Profiles.Profile getBehavior() => behavior;

        public byte[] ToTestAnimationByteArray()
        {
            Debug.Assert(animations.Count == 1);
            int size = animationBits.ComputeDataSize() + Marshal.SizeOf(animations[0].GetType());
            System.IntPtr ptr = Marshal.AllocHGlobal(size);
            for (int i = 0; i < size; ++i)
            {
                Marshal.WriteByte(ptr + i, 0);
            }

            System.IntPtr current = animationBits.WriteBytes(ptr);
            Marshal.StructureToPtr(animations[0], current, false);

            byte[] ret = new byte[size];
            Marshal.Copy(ptr, ret, 0, size);
            Marshal.FreeHGlobal(ptr);
            return ret;
        }

        public byte[] ToByteArray()
        {
            int size = ComputeDataSetDataSize();
            System.IntPtr ptr = Marshal.AllocHGlobal(size);
            for (int i = 0; i < size; ++i)
            {
                Marshal.WriteByte(ptr + i, 0);
            }

            WriteBytes(ptr);

            byte[] ret = new byte[size];
            Marshal.Copy(ptr, ret, 0, size);
            Marshal.FreeHGlobal(ptr);

            return ret;
        }

        public System.IntPtr WriteBytes(System.IntPtr ptr)
        {
            // Copy palette
            System.IntPtr current = ptr;
            current = animationBits.WriteBytes(current);

            // Copy animations
            // Offsets first
            short offset = 0;
            var currentCopy = current;
            foreach (var anim in animations)
            {
                Marshal.WriteInt16(current, offset);
                current += Marshal.SizeOf<ushort>();
                offset += (short)Marshal.SizeOf(anim.GetType());
            }

            // Round up to nearest multiple of 4
            current = currentCopy + roundUpTo4(animations.Count * Marshal.SizeOf<ushort>());

            // Then animations
            foreach (var anim in animations)
            {
                Marshal.StructureToPtr(anim, current, false);
                current += Marshal.SizeOf(anim.GetType());
            }

            // Copy conditions
            // Offsets first
            offset = 0;
            currentCopy = current;
            foreach (var cond in conditions)
            {
                Marshal.WriteInt16(current, offset);
                current += Marshal.SizeOf<ushort>();
                offset += (short)Marshal.SizeOf(cond.GetType());
            }

            // Round up to nearest multiple of 4
            current = currentCopy + roundUpTo4(conditions.Count * Marshal.SizeOf<ushort>());

            // Then conditions
            foreach (var cond in conditions)
            {
                Marshal.StructureToPtr(cond, current, false);
                current += Marshal.SizeOf(cond.GetType());
            }

            // Copy actions
            // Offsets first
            offset = 0;
            currentCopy = current;
            foreach (var action in actions)
            {
                Marshal.WriteInt16(current, offset);
                current += Marshal.SizeOf<ushort>();
                offset += (short)Marshal.SizeOf(action.GetType());
            }

            // Round up to nearest multiple of 4
            current = currentCopy + roundUpTo4(actions.Count * Marshal.SizeOf<ushort>());

            // Then actions
            foreach (var action in actions)
            {
                Marshal.StructureToPtr(action, current, false);
                current += Marshal.SizeOf(action.GetType());
            }

            // Rules
            foreach (var rule in rules)
            {
                Marshal.StructureToPtr(rule, current, false);
                current += Marshal.SizeOf<Profiles.Rule>();
            }

            // Behaviors
            Marshal.StructureToPtr(behavior, current, false);
            current += Marshal.SizeOf<Profiles.Profile>();

            return current;
        }

        public void Compress()
        {
            // // First try to find identical sets of keyframes in tracks
            // for (int t = 0; t < rgbTracks.Count; ++t)
            // {
            //     RGBTrack trackT = rgbTracks[t];
            //     for (int r = t + 1; r < rgbTracks.Count; ++r)
            //     {
            //         RGBTrack trackR = rgbTracks[r];

            //         // Only try to collapse tracks that are not exactly the same
            //         if (!trackT.Equals(trackR))
            //         {
            //             if (trackR.keyFrameCount == trackT.keyFrameCount)
            //             {
            //                 // Compare actual keyframes
            //                 bool kfEquals = true;
            //                 for (int k = 0; k < trackR.keyFrameCount; ++k)
            //                 {
            //                     var kfRk = trackR.GetKeyframe(this, (ushort)k);
            //                     var kfTk = trackT.GetKeyframe(this, (ushort)k);
            //                     if (!kfRk.Equals(kfTk))
            //                     {
            //                         kfEquals = false;
            //                         break;
            //                     }
            //                 }

            //                 if (kfEquals)
            //                 {
            //                     // Sweet, we can compress the keyframes
            //                     // Fix up any other tracks
            //                     for (int i = 0; i < rgbTracks.Count; ++i)
            //                     {
            //                         RGBTrack tr = rgbTracks[i];
            //                         if (tr.keyframesOffset > trackR.keyframesOffset)
            //                         {
            //                             tr.keyframesOffset -= trackR.keyFrameCount;
            //                             rgbTracks[i] = tr;
            //                         }
            //                     }

            //                     // Remove the duplicate keyframes
            //                     var newKeyframes = new List<RGBKeyframe>(keyframes.Count - trackR.keyFrameCount);
            //                     for (int i = 0; i < trackR.keyframesOffset; ++i)
            //                     {
            //                         newKeyframes.Add(keyframes[i]);
            //                     }
            //                     for (int i = trackR.keyframesOffset + trackR.keyFrameCount; i < keyframes.Count; ++i)
            //                     {
            //                         newKeyframes.Add(keyframes[i]);
            //                     }
            //                     keyframes = newKeyframes;

            //                     // And make R point to the keyframes of T
            //                     trackR.keyframesOffset = trackT.keyframesOffset;
            //                     rgbTracks[r] = trackR;
            //                 }
            //             }
            //         }
            //     }
            // }

            // // Then remove duplicate RGB tracks
            // for (int t = 0; t < rgbTracks.Count; ++t)
            // {
            //     RGBTrack trackT = rgbTracks[t];
            //     for (int r = t + 1; r < rgbTracks.Count; ++r)
            //     {
            //         RGBTrack trackR = rgbTracks[r];
            //         if (trackR.Equals(trackT))
            //         {
            //             // Remove track R and fix anim tracks
            //             // Fix up other animation tracks
            //             for (int j = 0; j < tracks.Count; ++j)
            //             {
            //                 AnimationTrack trj = tracks[j];
            //                 if (trj.trackOffset == r)
            //                 {
            //                     trj.trackOffset = (ushort)t;
            //                 }
            //                 else if (trj.trackOffset > r)
            //                 {
            //                     trj.trackOffset--;
            //                 }
            //                 tracks[j] = trj;
            //             }

            //             if (r == heatTrackIndex)
            //             {
            //                 heatTrackIndex = (ushort)t;
            //             }
            //             else if (r < heatTrackIndex)
            //             {
            //                 heatTrackIndex--;
            //             }

            //             // Remove the duplicate RGBTrack
            //             var newRGBTracks = new List<RGBTrack>();
            //             for (int j = 0; j < r; ++j)
            //             {
            //                 newRGBTracks.Add(rgbTracks[j]);
            //             }
            //             for (int j = r + 1; j < rgbTracks.Count; ++j)
            //             {
            //                 newRGBTracks.Add(rgbTracks[j]);
            //             }
            //             rgbTracks = newRGBTracks;
            //         }
            //     }
            // }

            // // We should also remove duplicate anim tracks and animation
        }

        static int roundUpTo4(int address)
        {
            return 4 * ((address + 3) / 4);
        }

    }
}
