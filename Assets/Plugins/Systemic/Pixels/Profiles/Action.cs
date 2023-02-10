using System.Runtime.InteropServices;

namespace Systemic.Unity.Pixels.Profiles
{
    /// <summary>
    /// Defines the supported types of actions.
    /// </summary>
    public enum ActionType : byte
    {
        None = 0,
        PlayAnimation,
        PlayAudioClip,
    };

    /// <summary>
    /// Base interface for Actions. Stores the actual type so that we can cast the data
    /// to the proper derived type and access the parameters.
    /// </summary>
    public interface IAction
    {
        ActionType type { get; set; }
    };

    /// <summary>
    /// Action to play an animation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class ActionPlayAnimation
        : IAction
    {
        public ActionType type { get; set; } = ActionType.PlayAnimation;
        public byte animIndex;
        public byte faceIndex;
        public byte loopCount;
    };


    /// <summary>
    /// Action to play a sound! 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class ActionPlayAudioClip
        : IAction
    {
        public ActionType type { get; set; } = ActionType.PlayAudioClip;
        public byte paddingType;
        public ushort clipId;
    };
}
