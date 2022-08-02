using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using UnityEngine;

namespace Systemic.Unity.Pixels.Messages
{
    public static class Marshaling
    {
        public const int MaxDataSize = 100;

        readonly static int MaxMessageType = (int)(System.Enum.GetValues(typeof(MessageType)) as MessageType[]).Last();

        public static IPixelMessage FromByteArray(byte[] data)
        {
            IPixelMessage ret = null;
            if (data?.Length == 0)
            {
                Debug.LogError("Got null or empty data for message marshaling");
            }
            else if (data[0] > MaxMessageType)
            {
                Debug.LogError($"Got unhandled message type {data[0]} for message marshaling");
            }
            else
            {
                var type = (MessageType)data[0];
                switch (type)
                {
                    case MessageType.RollState:
                        ret = FromByteArray<RollState>(data);
                        break;
                    case MessageType.WhoAreYou:
                        ret = FromByteArray<WhoAreYou>(data);
                        break;
                    case MessageType.IAmADie:
                        if (data.Length == Marshal.SizeOf<IAmADie>())
                        {
                            ret = FromByteArray<IAmADie>(data);
                        }
                        else
                        {
                            // This an older firwmare with the "versionInfo" string

                            // Get the message part before the versionInfo string
                            var baseData = new byte[Marshal.SizeOf<IAmADieMarshaledDataBeforeBuildTimestamp>()];
                            System.Array.Copy(data, baseData, baseData.Length);
                            var baseMsg = FromByteArray<IAmADieMarshaledDataBeforeBuildTimestamp>(baseData);

                            if (baseMsg != null)
                            {
                                // Get the versionInfo string
                                var versionInfo = BytesToString(data, baseData.Length, data.Length - baseData.Length);

                                // Convert to timestamp
                                uint timestamp = 0;
                                if (versionInfo?.Length > 0)
                                {
                                    var numbers = versionInfo.Split('_');
                                    if (numbers.Length >= 2)
                                    {
                                        try
                                        {
                                            int month = int.Parse(numbers[0]);
                                            int day = int.Parse(numbers[1]);
                                            int year = numbers.Length > 2 ? 2000 + int.Parse(numbers[2]) : (month < 7 ? 2021 : 2020);
                                            var dt = new System.DateTime(year, month, day);
                                            var epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
                                            timestamp = (uint)(dt - epoch).TotalSeconds;
                                        }
                                        catch (System.Exception ex)
                                        {
                                            Debug.LogError($"Error parsing versionInfo: {ex.Message}");
                                        }
                                    }
                                }

                                ret = new IAmADie
                                {
                                    ledCount = baseMsg.ledCount,
                                    designAndColor = baseMsg.designAndColor,
                                    padding = baseMsg.padding,
                                    dataSetHash = baseMsg.dataSetHash,
                                    pixelId = baseMsg.pixelId,
                                    availableFlashSize = baseMsg.flashSize,
                                    buildTimestamp = timestamp,
                                };
                            }
                        }
                        break;
                    case MessageType.Telemetry:
                        ret = FromByteArray<AccelerationState>(data);
                        break;
                    case MessageType.BulkSetup:
                        ret = FromByteArray<BulkSetup>(data);
                        break;
                    case MessageType.BulkData:
                        ret = FromByteArray<BulkData>(data);
                        break;
                    case MessageType.BulkSetupAck:
                        ret = FromByteArray<BulkSetupAck>(data);
                        break;
                    case MessageType.BulkDataAck:
                        ret = FromByteArray<BulkDataAck>(data);
                        break;
                    case MessageType.TransferAnimationSet:
                        ret = FromByteArray<TransferAnimationSet>(data);
                        break;
                    case MessageType.TransferAnimationSetAck:
                        ret = FromByteArray<TransferAnimationSetAck>(data);
                        break;
                    case MessageType.TransferAnimationSetFinished:
                        ret = FromByteArray<TransferAnimationSetFinished>(data);
                        break;
                    case MessageType.TransferTestAnimationSet:
                        ret = FromByteArray<TransferTestAnimationSet>(data);
                        break;
                    case MessageType.TransferTestAnimationSetAck:
                        ret = FromByteArray<TransferTestAnimationSetAck>(data);
                        break;
                    case MessageType.TransferTestAnimationSetFinished:
                        ret = FromByteArray<TransferTestAnimationSetFinished>(data);
                        break;
                    case MessageType.TransferSettings:
                        ret = FromByteArray<TransferSettings>(data);
                        break;
                    case MessageType.TransferSettingsAck:
                        ret = FromByteArray<TransferSettingsAck>(data);
                        break;
                    case MessageType.TransferSettingsFinished:
                        ret = FromByteArray<TransferSettingsFinished>(data);
                        break;
                    case MessageType.DebugLog:
                        ret = FromByteArray<DebugLog>(data);
                        break;
                    case MessageType.PlayAnimation:
                        ret = FromByteArray<PlayAnimation>(data);
                        break;
                    case MessageType.PlayAnimationEvent:
                        ret = FromByteArray<PlayAnimationEvent>(data);
                        break;
                    case MessageType.PlaySound:
                        ret = FromByteArray<PlaySound>(data);
                        break;
                    case MessageType.StopAnimation:
                        ret = FromByteArray<StopAnimation>(data);
                        break;
                    case MessageType.RequestRollState:
                        ret = FromByteArray<RequestRollState>(data);
                        break;
                    case MessageType.RequestAnimationSet:
                        ret = FromByteArray<RequestAnimationSet>(data);
                        break;
                    case MessageType.RequestSettings:
                        ret = FromByteArray<RequestSettings>(data);
                        break;
                    case MessageType.RequestTelemetry:
                        ret = FromByteArray<RequestTelemetry>(data);
                        break;
                    case MessageType.BlinkFinished:
                        ret = FromByteArray<BlinkFinished>(data);
                        break;
                    case MessageType.ProgramDefaultAnimationSetFinished:
                        ret = FromByteArray<ProgramDefaultAnimSetFinished>(data);
                        break;
                    case MessageType.DefaultAnimationSetColor:
                        ret = FromByteArray<DefaultAnimationSetColor>(data);
                        break;
                    case MessageType.BatteryLevel:
#if PLATFORM_ANDROID
                        var modifiedData = new byte[13];
                        modifiedData[0] = data[0];
                        System.Array.Copy(data, 1, modifiedData, 4, 9);
                        ret = FromByteArray<BatteryLevel>(modifiedData);
#else
                        ret = FromByteArray<BatteryLevel>(data);
#endif
                        break;
                    case MessageType.RequestBatteryLevel:
                        ret = FromByteArray<RequestBatteryLevel>(data);
                        break;
                    case MessageType.RequestRssi:
                        ret = FromByteArray<RequestRssi>(data);
                        break;
                    case MessageType.Rssi:
                        ret = FromByteArray<Rssi>(data);
                        break;
                    case MessageType.Calibrate:
                        ret = FromByteArray<Calibrate>(data);
                        break;
                    case MessageType.CalibrateFace:
                        ret = FromByteArray<CalibrateFace>(data);
                        break;
                    case MessageType.NotifyUser:
                        ret = FromByteArray<NotifyUser>(data);
                        break;
                    case MessageType.NotifyUserAck:
                        ret = FromByteArray<NotifyUserAck>(data);
                        break;
                    case MessageType.TestHardware:
                        ret = FromByteArray<TestHardware>(data);
                        break;
                    case MessageType.SetStandardState:
                        ret = FromByteArray<SetStandardState>(data);
                        break;
                    case MessageType.SetLEDAnimationState:
                        ret = FromByteArray<SetLEDAnimState>(data);
                        break;
                    case MessageType.SetBattleState:
                        ret = FromByteArray<SetBattleState>(data);
                        break;
                    case MessageType.ProgramDefaultParameters:
                        ret = FromByteArray<ProgramDefaultParameters>(data);
                        break;
                    case MessageType.ProgramDefaultParametersFinished:
                        ret = FromByteArray<ProgramDefaultParametersFinished>(data);
                        break;
                    case MessageType.AttractMode:
                        ret = FromByteArray<AttractMode>(data);
                        break;
                    case MessageType.PrintNormals:
                        ret = FromByteArray<PrintNormals>(data);
                        break;
                    case MessageType.SetDesignAndColor:
                        ret = FromByteArray<SetDesignAndColor>(data);
                        break;
                    case MessageType.SetDesignAndColorAck:
                        ret = FromByteArray<SetDesignAndColorAck>(data);
                        break;
                    case MessageType.SetCurrentBehavior:
                        ret = FromByteArray<SetCurrentBehavior>(data);
                        break;
                    case MessageType.SetCurrentBehaviorAck:
                        ret = FromByteArray<SetCurrentBehaviorAck>(data);
                        break;
                    case MessageType.SetName:
                        ret = FromByteArray<SetName>(data);
                        break;
                    case MessageType.SetNameAck:
                        ret = FromByteArray<SetNameAck>(data);
                        break;
                    case MessageType.DebugAnimationController:
                        ret = FromByteArray<DebugAnimationController>(data);
                        break;
                    default:
                        throw new System.Exception($"Unhandled DieMessage type {type} for marshaling");
                }
            }
            return ret;
        }

        static T FromByteArray<T>(byte[] data)
            where T : class
        {
            int size = Marshal.SizeOf<T>();
            if (data?.Length == size)
            {
                var ptr = Marshal.AllocHGlobal(size);
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
                Debug.LogError($"Incorrect data size ${data?.Length ?? -1} != ${size} for marshaling to message of type {typeof(T).Name}");
                return null;
            }
        }

        public static byte[] ToByteArray<T>(T message)
            where T : IPixelMessage
        {
            int size = Marshal.SizeOf(typeof(T));
            System.IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(message, ptr, false);
            byte[] ret = new byte[size];
            Marshal.Copy(ptr, ret, 0, size);
            Marshal.FreeHGlobal(ptr);
            return ret;
        }

        public static string BytesToString(byte[] buffer, int startIndex = 0, int strSize = -1)
        {
            if (strSize < 0)
            {
                strSize = buffer.Length - startIndex;
            }
            var strData = new byte[strSize]; // It's ok if size is zero
            System.Array.Copy(buffer, startIndex, strData, 0, strData.Length);
            int zeroIndex = System.Array.IndexOf<byte>(strData, 0);
            return Encoding.UTF8.GetString(strData, 0, zeroIndex >= 0 ? zeroIndex : strSize);
        }

        public static byte[] StringToBytes(string str, int arraySize, bool withZeroTerminator = false)
        {
            if (withZeroTerminator)
            {
                str += "\0";
            }
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            byte[] outArray = new byte[arraySize];
            System.Array.Copy(bytes, outArray, Mathf.Min(bytes.Length, arraySize - (withZeroTerminator ? 1 : 0)));
            return outArray;
        }

        static readonly Dictionary<System.Type, MessageType> _messageTypes = new Dictionary<System.Type, MessageType>();

        public static MessageType GetMessageType<T>()
            where T : IPixelMessage, new()
        {
            if (!_messageTypes.TryGetValue(typeof(T), out MessageType type))
            {
                type = (new T()).type;
                _messageTypes.Add(typeof(T), type);
            }
            return type;
        }
    }
}
