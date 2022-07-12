using System.Runtime.InteropServices;

namespace Systemic.Unity.Pixels.Profiles
{
    /// <summary>
    /// Defines the supported types of conditions.
    /// </summary>
    public enum ConditionType : byte
    {
        //TODO [SkipEnumValue]
        Unknown = 0,
        //TODO [Name("Pixel wakes up / sleeps")]
        HelloGoodbye,
        //TODO [Name("Pixel is picked up")]
        Handling,
        //TODO [Name("Pixel is rolling")]
        Rolling,
        //TODO [Name("Pixel roll is...")]
        FaceCompare,
        //TODO [Name("Pixel is crooked")]
        Crooked,
        //TODO [Name("Bluetooth Event...")]
        ConnectionState,
        //TODO [Name("Battery Event...")]
        BatteryState,
        //TODO [Name("Pixel is idle for...")]
        Idle,
    };

    /// <summary>
    /// The base struct for all conditions, stores a type identifier so we can tell the actual
    /// type of the condition and fetch the condition parameters correctly.
    /// </summary>
	public interface ICondition
    {
        ConditionType type { get; set; }
    };

    /// <summary>
    /// Condition that triggers when the Pixel is being handled
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class ConditionIdle
        : ICondition
    {
        public ConditionType type { get; set; } = ConditionType.Idle;
        public byte padding1;
        public ushort repeatPeriodMs;
    };

    /// <summary>
    /// Condition that triggers when the Pixel is being handled
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class ConditionHandling
        : ICondition
    {
        public ConditionType type { get; set; } = ConditionType.Handling;
        public byte padding1;
        public byte padding2;
        public byte padding3;
    };

    /// <summary>
    /// Condition that triggers when the Pixel is being rolled
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class ConditionRolling
        : ICondition
    {
        public ConditionType type { get; set; } = ConditionType.Rolling;
        public byte padding1;
        public ushort repeatPeriodMs; // 0 means do NOT repeat
    };

    /// <summary>
    /// Condition that triggers when the Pixel has landed by is crooked
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class ConditionCrooked
        : ICondition
    {
        public ConditionType type { get; set; } = ConditionType.Crooked;
        public byte padding1;
        public byte padding2;
        public byte padding3;
    };

    /// <summary>
    /// Flags used to indicate how we treat the face, whether we want to trigger if the
    /// value is greater than the parameter, less, or equal, or any combination
    /// </summary>
    [System.Flags]
    public enum FaceCompareFlags : byte
    {
        Less    = 1 << 0,
        Equal   = 1 << 1,
        Greater = 1 << 2
    };

    /// <summary>
    /// Condition that triggers when the Pixel has landed on a face
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class ConditionFaceCompare
        : ICondition
    {
        public ConditionType type { get; set; } = ConditionType.FaceCompare;
        public byte faceIndex;
        public FaceCompareFlags flags;
        public byte paddingFlags;
    };

    /// <summary>
    /// Indicate whether the condition should trigger on Hello, Goodbye or both
    /// </summary>
    [System.Flags]
    public enum HelloGoodbyeFlags : byte
    {
        Hello      = 1 << 0,
        Goodbye    = 1 << 1
    };

    /// <summary>
    /// Condition that triggers on a life state event
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class ConditionHelloGoodbye
        : ICondition
    {
        public ConditionType type { get; set; } = ConditionType.HelloGoodbye;
        public HelloGoodbyeFlags flags;
        public byte padding1;
        public byte padding2;
    };

    /// <summary>
    /// Indicates when the condition should trigger, connected!, disconnected! or both
    /// </summary>
    [System.Flags]
    public enum ConnectionStateFlags : byte
    {
        Connected      = 1 << 0,
        Disconnected   = 1 << 1,
    };

    /// <summary>
    /// Condition that triggers on connection events
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class ConditionConnectionState
        : ICondition
    {
        public ConditionType type { get; set; } = ConditionType.ConnectionState;
        public ConnectionStateFlags flags;
        public byte padding1;
        public byte padding2;
    };

    /// <summary>
    /// Indicates which battery event the condition should trigger on
    /// </summary>
    [System.Flags]
    public enum BatteryStateFlags : byte
    {
        Ok        = 1 << 0,
        Low       = 1 << 1,
		Charging  = 1 << 2,
		Done      = 1 << 3
    };

    /// <summary>
    /// Condition that triggers on battery state events
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [System.Serializable]
    public class ConditionBatteryState
        : ICondition
    {
        public ConditionType type { get; set; } = ConditionType.BatteryState;
        public BatteryStateFlags flags;
        public ushort repeatPeriodMs;
    };
}
