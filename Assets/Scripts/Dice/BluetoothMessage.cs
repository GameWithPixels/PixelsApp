using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dice
{
    /// <summary>
    /// These message identifiers have to match up with the ones on the firmware of course!
    /// </summary>
    public enum DieMessageType : byte
    {
        None = 0,
        WhoAreYou,
        IAmADie,
        State,
        Telemetry,
        BulkSetup,
        BulkSetupAck,
        BulkData,
        BulkDataAck,
        TransferAnimSet,
        TransferAnimSetAck,
        TransferAnimSetFinished,
        TransferSettings,
        TransferSettingsAck,
        TransferSettingsFinished,
        TransferTestAnimSet,
        TransferTestAnimSetAck,
        TransferTestAnimSetFinished,
        DebugLog,
        PlayAnim,
        PlayAnimEvent,
        StopAnim,
        PlaySound,
        RequestState,
        RequestAnimSet,
        RequestSettings,
        RequestTelemetry,
        ProgramDefaultAnimSet,
        ProgramDefaultAnimSetFinished,
        Flash,
        FlashFinished,
        RequestDefaultAnimSetColor,
        DefaultAnimSetColor,
        RequestBatteryLevel,
        BatteryLevel,
        RequestRssi,
        Rssi,
        Calibrate,
        CalibrateFace,
        NotifyUser,
        NotifyUserAck,
        TestHardware,
        SetStandardState,
        SetLEDAnimState,
        SetBattleState,
        ProgramDefaultParameters,
        ProgramDefaultParametersFinished,
        SetDesignAndColor,
        SetDesignAndColorAck,
        SetCurrentBehavior,
        SetCurrentBehaviorAck,
        SetName,
        SetNameAck,

        // Testing
        TestBulkSend, 
        TestBulkReceive,
        SetAllLEDsToColor,
        AttractMode,
        PrintNormals,
        PrintA2DReadings,
        LightUpFace,
        SetLEDToColor,
        DebugAnimController,
    }

    public interface IDieMessage
    {
        DieMessageType type { get; set; }
    }

    public static class DieMessages
    {
        public const int maxDataSize = 100;

        public static IDieMessage FromByteArray(byte[] data)
        {
            IDieMessage ret = null;
            if (data.Length > 0)
            {
                DieMessageType type = (DieMessageType)data[0];
                switch (type)
                {
                    case DieMessageType.State:
                        ret = FromByteArray<DieMessageState>(data);
                        break;
                    case DieMessageType.WhoAreYou:
                        ret = FromByteArray<DieMessageWhoAreYou>(data);
                        break;
                    case DieMessageType.IAmADie:
                        {
                            var baseData = new byte[Marshal.SizeOf<DieMessageIAmADieMarshalledData>()];
                            if (data.Length > baseData.Length)
                            {
                                System.Array.Copy(data, baseData, baseData.Length);
                                var baseMsg = FromByteArray<DieMessageIAmADieMarshalledData>(baseData);
                                if (baseMsg != null)
                                {
                                    var strData = new byte[data.Length - baseData.Length - 1]; // It's ok if size is zero
                                    System.Array.Copy(data, baseData.Length, strData, 0, strData.Length);
                                    var str = Encoding.UTF8.GetString(strData);
                                    ret = new DieMessageIAmADie
                                    {
                                        faceCount = baseMsg.faceCount,
                                        designAndColor = baseMsg.designAndColor,
                                        padding = baseMsg.padding,
                                        dataSetHash = baseMsg.dataSetHash,
                                        deviceId = baseMsg.deviceId,
                                        flashSize = baseMsg.flashSize,
                                        versionInfo = str,
                                    };
                                }
                            }
                        }
                        break;
                    case DieMessageType.Telemetry:
                        ret = FromByteArray<DieMessageAcc>(data);
                        break;
                    case DieMessageType.BulkSetup:
                        ret = FromByteArray<DieMessageBulkSetup>(data);
                        break;
                    case DieMessageType.BulkData:
                        ret = FromByteArray<DieMessageBulkData>(data);
                        break;
                    case DieMessageType.BulkSetupAck:
                        ret = FromByteArray<DieMessageBulkSetupAck>(data);
                        break;
                    case DieMessageType.BulkDataAck:
                        ret = FromByteArray<DieMessageBulkDataAck>(data);
                        break;
                    case DieMessageType.TransferAnimSet:
                        ret = FromByteArray<DieMessageTransferAnimSet>(data);
                        break;
                    case DieMessageType.TransferAnimSetAck:
                        ret = FromByteArray<DieMessageTransferAnimSetAck>(data);
                        break;
                    case DieMessageType.TransferAnimSetFinished:
                        ret = FromByteArray<DieMessageTransferAnimSetFinished>(data);
                        break;
                    case DieMessageType.TransferTestAnimSet:
                        ret = FromByteArray<DieMessageTransferTestAnimSet>(data);
                        break;
                    case DieMessageType.TransferTestAnimSetAck:
                        ret = FromByteArray<DieMessageTransferTestAnimSetAck>(data);
                        break;
                    case DieMessageType.TransferTestAnimSetFinished:
                        ret = FromByteArray<DieMessageTransferTestAnimSetFinished>(data);
                        break;
                    case DieMessageType.TransferSettings:
                        ret = FromByteArray<DieMessageTransferSettings>(data);
                        break;
                    case DieMessageType.TransferSettingsAck:
                        ret = FromByteArray<DieMessageTransferSettingsAck>(data);
                        break;
                    case DieMessageType.TransferSettingsFinished:
                        ret = FromByteArray<DieMessageTransferSettingsFinished>(data);
                        break;
                    case DieMessageType.DebugLog:
                        ret = FromByteArray<DieMessageDebugLog>(data);
                        break;
                    case DieMessageType.PlayAnim:
                        ret = FromByteArray<DieMessagePlayAnim>(data);
                        break;
                    case DieMessageType.PlayAnimEvent:
                        ret = FromByteArray<DieMessagePlayAnimEvent>(data);
                        break;
                    case DieMessageType.PlaySound:
                        ret = FromByteArray<DieMessagePlaySound>(data);
                        break;
                    case DieMessageType.StopAnim:
                        ret = FromByteArray<DieMessageStopAnim>(data);
                        break;
                    case DieMessageType.RequestState:
                        ret = FromByteArray<DieMessageRequestState>(data);
                        break;
                    case DieMessageType.RequestAnimSet:
                        ret = FromByteArray<DieMessageRequestAnimSet>(data);
                        break;
                    case DieMessageType.RequestSettings:
                        ret = FromByteArray<DieMessageRequestSettings>(data);
                        break;
                    case DieMessageType.RequestTelemetry:
                        ret = FromByteArray<DieMessageRequestTelemetry>(data);
                        break;
                    case DieMessageType.FlashFinished:
                        ret = FromByteArray<DieMessageFlashFinished>(data);
                        break;
                    case DieMessageType.ProgramDefaultAnimSetFinished:
                        ret = FromByteArray<DieMessageProgramDefaultAnimSetFinished>(data);
                        break;
                    case DieMessageType.DefaultAnimSetColor:
                        ret = FromByteArray<DieMessageDefaultAnimSetColor>(data);
                        break;
                    case DieMessageType.BatteryLevel:
#if PLATFORM_ANDROID
                        var modifiedData = new byte[13];
                        modifiedData[0] = data[0];
                        System.Array.Copy(data, 1, modifiedData, 4, 9);
                        ret = FromByteArray<DieMessageBatteryLevel>(modifiedData);
#else
                        ret = FromByteArray<DieMessageBatteryLevel>(data);
#endif
                        break;
                    case DieMessageType.RequestBatteryLevel:
                        ret = FromByteArray<DieMessageRequestBatteryLevel>(data);
                        break;
                    case DieMessageType.RequestRssi:
                        ret = FromByteArray<DieMessageRequestRssi>(data);
                        break;
                    case DieMessageType.Rssi:
                        ret = FromByteArray<DieMessageRssi>(data);
                        break;
                    case DieMessageType.Calibrate:
                        ret = FromByteArray<DieMessageCalibrate>(data);
                        break;
                    case DieMessageType.CalibrateFace:
                        ret = FromByteArray<DieMessageCalibrateFace>(data);
                        break;
                    case DieMessageType.NotifyUser:
                        ret = FromByteArray<DieMessageNotifyUser>(data);
                        break;
                    case DieMessageType.NotifyUserAck:
                        ret = FromByteArray<DieMessageNotifyUserAck>(data);
                        break;
                    case DieMessageType.TestHardware:
                        ret = FromByteArray<DieMessageTestHardware>(data);
                        break;
                    case DieMessageType.SetStandardState:
                        ret = FromByteArray<DieMessageSetStandardState>(data);
                        break;
                    case DieMessageType.SetLEDAnimState:
                        ret = FromByteArray<DieMessageSetLEDAnimState>(data);
                        break;
                    case DieMessageType.SetBattleState:
                        ret = FromByteArray<DieMessageSetBattleState>(data);
                        break;
                    case DieMessageType.ProgramDefaultParameters:
                        ret = FromByteArray<DieMessageProgramDefaultParameters>(data);
                        break;
                    case DieMessageType.ProgramDefaultParametersFinished:
                        ret = FromByteArray<DieMessageProgramDefaultParametersFinished>(data);
                        break;
                    case DieMessageType.AttractMode:
                        ret = FromByteArray<DieMessageAttractMode>(data);
                        break;
                    case DieMessageType.PrintNormals:
                        ret = FromByteArray<DieMessagePrintNormals>(data);
                        break;
                    case DieMessageType.SetDesignAndColor:
                        ret = FromByteArray<DieMessageSetDesignAndColor>(data);
                        break;
                    case DieMessageType.SetDesignAndColorAck:
                        ret = FromByteArray<DieMessageSetDesignAndColorAck>(data);
                        break;
                    case DieMessageType.SetCurrentBehavior:
                        ret = FromByteArray<DieMessageSetCurrentBehavior>(data);
                        break;
                    case DieMessageType.SetCurrentBehaviorAck:
                        ret = FromByteArray<DieMessageSetCurrentBehaviorAck>(data);
                        break;
                    case DieMessageType.SetName:
                        ret = FromByteArray<DieMessageSetName>(data);
                        break;
                    case DieMessageType.SetNameAck:
                        ret = FromByteArray<DieMessageSetNameAck>(data);
                        break;
                    case DieMessageType.DebugAnimController:
                        ret = FromByteArray<DieMessageDebugAnimController>(data);
                        break;
                    default:
                        throw new System.Exception("Unhandled DieMessage type " + type.ToString() + " for marshaling");
                }
            }
            return ret;
        }

        static T FromByteArray<T>(byte[] data)
            where T : class, IDieMessage
        {
            int size = Marshal.SizeOf<T>();
            if (data.Length == size)
            {
                System.IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.Copy(data, 0, ptr, size);
                    return (T)Marshal.PtrToStructure(ptr, typeof(T));
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            else
            {
                Debug.LogError("Wrong message length for type " + typeof(T).Name);
                return null;
            }
        }

        // For virtual dice!
        public static byte[] ToByteArray<T>(T message)
            where T : IDieMessage
        {
            int size = Marshal.SizeOf(typeof(T));
            System.IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(message, ptr, false);
            byte[] ret = new byte[size];
            Marshal.Copy(ptr, ret, 0, size);
            Marshal.FreeHGlobal(ptr);
            return ret;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageWhoAreYou
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.WhoAreYou;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageIAmADieMarshalledData
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.IAmADie;

        public byte faceCount; // Which kind of dice this is
        public DesignAndColor designAndColor; // Physical look
        public byte padding;
        public uint dataSetHash;
        public uint deviceId; // A unique identifier
        public ushort flashSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageIAmADie : DieMessageIAmADieMarshalledData
    {
        public string versionInfo; // Firmware version string, i.e. "10_05_21", variable size
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageState
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.State;
        public Die.RollState state;
        public byte face;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageAcc
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.Telemetry;

        public AccelFrame data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageBulkSetup
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.BulkSetup;
        public short size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageBulkSetupAck
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.BulkSetupAck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageBulkData
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.BulkData;
        public byte size;
        public ushort offset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DieMessages.maxDataSize)]
        public byte[] data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageBulkDataAck
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.BulkDataAck;
        public ushort offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTransferAnimSet
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TransferAnimSet;
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
    public class DieMessageTransferAnimSetAck
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TransferAnimSetAck;
        public byte result;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTransferAnimSetFinished
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TransferAnimSetFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTransferTestAnimSet
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TransferTestAnimSet;

        public ushort paletteSize;
        public ushort rgbKeyFrameCount;
        public ushort rgbTrackCount;
        public ushort keyFrameCount;
        public ushort trackCount;
        public ushort animationSize;
        public uint hash;
    }

    public enum TransferTestAnimSetAckType : byte
    {
        Download = 0,
        UpToDate,
        NoMemory
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTransferTestAnimSetAck
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TransferTestAnimSetAck;
        public TransferTestAnimSetAckType ackType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTransferTestAnimSetFinished
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TransferTestAnimSetFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageRequestAnimSet
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.RequestAnimSet;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTransferSettings
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TransferSettings;
        public byte count;
        public short totalAnimationByteSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTransferSettingsAck
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TransferSettingsAck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTransferSettingsFinished
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TransferSettingsFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageRequestSettings
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.RequestSettings;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageRequestTelemetry
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.RequestTelemetry;
        public byte telemetry;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageDebugLog
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.DebugLog;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DieMessages.maxDataSize)]
        public byte[] data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessagePlayAnim
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.PlayAnim;
        public byte index;
        public byte remapFace;
        public byte loop;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessagePlayAnimEvent
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.PlayAnimEvent;
        public byte evt;
        public byte remapFace;
        public byte loop;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageStopAnim
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.StopAnim;
        public byte index;
        public byte remapFace;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessagePlaySound
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.PlaySound;
        public ushort clipId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageRequestState
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.RequestState;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageProgramDefaultAnimSet
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.ProgramDefaultAnimSet;
        public uint color;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageProgramDefaultAnimSetFinished
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.ProgramDefaultAnimSetFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageFlash
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.Flash;
        public byte flashCount;
        public uint color;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageFlashFinished
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.FlashFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageRequestDefaultAnimSetColor
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.RequestDefaultAnimSetColor;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageDefaultAnimSetColor
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.DefaultAnimSetColor;
        public uint color;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTestBulkSend
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TestBulkSend;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTestBulkReceive
        : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TestBulkReceive;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageSetAllLEDsToColor
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.SetAllLEDsToColor;
        public uint color;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageBatteryLevel
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.BatteryLevel;
#if PLATFORM_ANDROID
        // We need padding on ARMv7 to have 4 bytes alignment for float types
        private byte _padding1, _padding2, _padding3;
#endif
        public float level;
        public float voltage;
        public byte charging;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageRequestBatteryLevel
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.RequestBatteryLevel;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageRssi
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.Rssi;
        public short rssi;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageRequestRssi
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.RequestRssi;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageCalibrate
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.Calibrate;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageCalibrateFace
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.CalibrateFace;
        public byte face;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageNotifyUser
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.NotifyUser;
        public byte timeout_s;
        public byte ok; // Boolean
        public byte cancel; // Boolean
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DieMessages.maxDataSize - 4)]
        public byte[] data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageNotifyUserAck
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.NotifyUserAck;
        public byte okCancel; // Boolean
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageTestHardware
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.TestHardware;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageSetStandardState
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.SetStandardState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageSetLEDAnimState
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.SetLEDAnimState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageSetBattleState
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.SetBattleState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageProgramDefaultParameters
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.ProgramDefaultParameters;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageProgramDefaultParametersFinished
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.ProgramDefaultParametersFinished;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageAttractMode
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.AttractMode;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessagePrintNormals
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.PrintNormals;
        public byte face;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageSetDesignAndColor
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.SetDesignAndColor;
        public DesignAndColor designAndColor;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageSetDesignAndColorAck
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.SetDesignAndColorAck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageSetCurrentBehavior
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.SetCurrentBehavior;
        public byte currentBehaviorIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageSetCurrentBehaviorAck
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.SetCurrentBehaviorAck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageSetName
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.SetName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageSetNameAck
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.SetNameAck;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DieMessageDebugAnimController
    : IDieMessage
    {
        public DieMessageType type { get; set; } = DieMessageType.DebugAnimController;
    }
    
}

