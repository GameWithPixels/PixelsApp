using System.Runtime.InteropServices;

namespace Systemic.Unity.Pixels.Messages
{
    public interface IPixelMessage
    {
        MessageType type { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class WhoAreYou : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.WhoAreYou;
    }

    // Represents the first part of IAmADie message (before versionInfo was replaced by buildTimestamp)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class IAmADieMarshaledDataBeforeBuildTimestamp
    {
        public MessageType type;
        public byte faceCount; // Which kind of dice this is
        public PixelDesignAndColor designAndColor; // Physical look
        public byte padding;
        public uint dataSetHash;
        public uint deviceId; // A unique identifier
        public ushort flashSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class IAmADie : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.IAmADie;

        public byte faceCount; // Which kind of dice this is
        public PixelDesignAndColor designAndColor; // Physical look
        public byte padding;
        public uint dataSetHash;
        public uint deviceId; // A unique identifier
        public ushort availableFlashSize; // Available flash memory size for storing settings
        public uint buildTimestamp; // Firmware build timestamp
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RollState : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RollState;
        public PixelRollState state;
        public byte face;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AccelerationState : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.Telemetry;

        public AccelFrame data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BulkSetup : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.BulkSetup;
        public short size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BulkSetupAck : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.BulkSetupAck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BulkData : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.BulkData;
        public byte size;
        public ushort offset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Marshaling.MaxDataSize)]
        public byte[] data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BulkDataAck : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.BulkDataAck;
        public ushort offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TransferAnimationSet : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TransferAnimationSet;
        public ushort paletteSize;
        public ushort rgbKeyFrameCount;
        public ushort rgbTrackCount;
        public ushort keyFrameCount;
        public ushort trackCount;
        public ushort animationCount;
        public ushort animationSize;
        public ushort conditionCount;
        public ushort conditionSize;
        public ushort actionCount;
        public ushort actionSize;
        public ushort ruleCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TransferAnimationSetAck : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TransferAnimationSetAck;
        public byte result;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TransferAnimationSetFinished : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TransferAnimationSetFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TransferTestAnimationSet : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TransferTestAnimationSet;

        public ushort paletteSize;
        public ushort rgbKeyFrameCount;
        public ushort rgbTrackCount;
        public ushort keyFrameCount;
        public ushort trackCount;
        public ushort animationSize;
        public uint hash;
    }

    public enum TransferTestAnimationSetAckType : byte
    {
        Download = 0,
        UpToDate,
        NoMemory
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TransferTestAnimationSetAck : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TransferTestAnimationSetAck;
        public TransferTestAnimationSetAckType ackType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TransferTestAnimationSetFinished : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TransferTestAnimationSetFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RequestAnimationSet : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RequestAnimationSet;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TransferSettings : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TransferSettings;
        public byte count;
        public short totalAnimationByteSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TransferSettingsAck : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TransferSettingsAck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TransferSettingsFinished : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TransferSettingsFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RequestSettings : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RequestSettings;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RequestTelemetry : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RequestTelemetry;
        public byte telemetry;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DebugLog : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.DebugLog;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Marshaling.MaxDataSize)]
        public byte[] data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PlayAnimation : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.PlayAnimation;
        public byte index;
        public byte remapFace;
        public byte loop;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PlayAnimationEvent : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.PlayAnimationEvent;
        public byte evt;
        public byte remapFace;
        public byte loop;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class StopAnimation : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.StopAnimation;
        public byte index;
        public byte remapFace;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PlaySound : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.PlaySound;
        public ushort clipId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RequestRollState : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RequestRollState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ProgramDefaultAnimationSet : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.ProgramDefaultAnimationSet;
        public uint color;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ProgramDefaultAnimSetFinished : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.ProgramDefaultAnimationSetFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Blink : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.Blink;
        public byte flashCount;
        public uint color;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BlinkFinished : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.BlinkFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RequestDefaultAnimationSetColor : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RequestDefaultAnimationSetColor;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DefaultAnimationSetColor : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.DefaultAnimationSetColor;
        public uint color;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TestBulkSend : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TestBulkSend;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TestBulkReceive : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TestBulkReceive;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetAllLEDsToColor : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.SetAllLEDsToColor;
        public uint color;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RequestBatteryLevel : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RequestBatteryLevel;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BatteryLevel : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.BatteryLevel;
#if PLATFORM_ANDROID
        // We need padding on ARMv7 to have 4 bytes alignment for float types
        private byte _padding1, _padding2, _padding3;
#endif
        public float level;
        public float voltage;
        public byte charging;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RequestRssi : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RequestRssi;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Rssi : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.Rssi;
        public short value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Calibrate : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.Calibrate;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CalibrateFace : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.CalibrateFace;
        public byte face;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class NotifyUser : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.NotifyUser;
        public byte timeout_s;
        public byte ok; // Boolean
        public byte cancel; // Boolean
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Marshaling.MaxDataSize - 4)]
        public byte[] data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class NotifyUserAck : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.NotifyUserAck;
        public byte okCancel; // Boolean
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TestHardware : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TestHardware;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetStandardState : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.SetStandardState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetLEDAnimState : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.SetLEDAnimationState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetBattleState : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.SetBattleState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ProgramDefaultParameters : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.ProgramDefaultParameters;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ProgramDefaultParametersFinished : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.ProgramDefaultParametersFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AttractMode : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.AttractMode;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PrintNormals : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.PrintNormals;
        public byte face;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetDesignAndColor : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.SetDesignAndColor;
        public PixelDesignAndColor designAndColor;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetDesignAndColorAck : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.SetDesignAndColorAck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetCurrentBehavior : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.SetCurrentBehavior;
        public byte currentBehaviorIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetCurrentBehaviorAck : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.SetCurrentBehaviorAck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetName : IPixelMessage
    {
        public const int NameMaxSize = 25; // Including zero terminating character

        public MessageType type { get; set; } = MessageType.SetName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NameMaxSize)]
        public byte[] name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetNameAck : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.SetNameAck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DebugAnimationController : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.DebugAnimationController;
    }
}

