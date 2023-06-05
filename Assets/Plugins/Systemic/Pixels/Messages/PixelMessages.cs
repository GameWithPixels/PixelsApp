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
        public byte ledCount; // Which kind of dice this is
        public PixelDesignAndColor designAndColor; // Physical look
        public byte padding;
        public uint dataSetHash;
        public uint pixelId; // A unique identifier
        public ushort flashSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class IAmADie : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.IAmADie;

        public byte ledCount; // Which kind of dice this is
        public PixelDesignAndColor designAndColor; // Physical look
        public byte padding;
        public uint dataSetHash;
        public uint pixelId; // A unique identifier
        public ushort availableFlashSize; // Available flash memory size for storing settings
        public uint buildTimestamp; // Firmware build timestamp

        // Roll state
        public PixelRollState rollState;
        public byte rollFaceIndex;

        // Battery level
        public byte batteryLevelPercent;
        public PixelBatteryState batteryState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RollState : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RollState;
        public PixelRollState state;
        public byte faceIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Telemetry : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.Telemetry;

        public AccelerationFrame accelFrame;

        // Battery and power
        public byte batteryLevelPercent;
        public PixelBatteryState batteryState;
        public byte voltageTimes50;
        public byte vCoilTimes50;

        // RSSI
        public sbyte rssi;
        public byte channelIndex;

        // Temperature
        public short mcuTemperatureTimes100;
        public short batteryTemperatureTimes100;
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

    public enum TelemetryRequestMode : byte
    {
        Off = 0,
        Once = 1,
        Repeat = 2,
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RequestTelemetry : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RequestTelemetry;
        public TelemetryRequestMode requestMode;
        public ushort minInterval; // Milliseconds, 0 for no cap on rate
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
        public ushort duration;
        public uint color;
        public uint faceMask;
        public byte fade;
        public byte loop;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BlinkAck : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.BlinkAck;
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
        public byte levelPercent;
        public PixelBatteryState batteryState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class RequestRssi : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RequestRssi;
        public TelemetryRequestMode requestMode;
        public ushort minInterval; // Milliseconds, 0 for no cap on rate
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Rssi : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.Rssi;
        public sbyte value;
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
        public byte faceIndex;
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
    public class TestLedLoopback : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.TestLedLoopback;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class LedLoopback : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.LedLoopback;
        public byte value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SetTopLevelState : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.SetTopLevelState;
        public byte state; // See TopLevelState enumeration
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
        public byte faceIndex;
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
    public class RequestTemperature : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.RequestTemperature;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Temperature : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.Temperature;

        public short mcuTemperatureTimes100;
        public short batteryTemperatureTimes100;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PrintAnimControllerState : IPixelMessage
    {
        public MessageType type { get; set; } = MessageType.PrintAnimControllerState;
    }
}

