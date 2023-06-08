// Ignore Spelling: Mtu Rssi Uuid Uuids

using System;
using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Android
{
    internal enum AndroidRequestStatus : int
    {
        // From android.bluetooth.BluetoothGatt 
        GATT_SUCCESS = 0,                   // A GATT operation completed successfully
        GATT_READ_NOT_PERMITTED = 2,        // GATT read operation is not permitted
        GATT_WRITE_NOT_PERMITTED = 3,       // GATT write operation is not permitted
        GATT_INSUFFICIENT_AUTHENTICATION =5,// Insufficient authentication for a given operation
        GATT_REQUEST_NOT_SUPPORTED = 6,     // The given request is not supported
        GATT_INVALID_OFFSET = 7,            // A read or write operation was requested with an invalid offset
        GATT_INVALID_ATTRIBUTE_LENGTH = 13, //  A write operation exceeds the maximum length of the attribute
        GATT_INSUFFICIENT_ENCRYPTION = 15,  // Insufficient encryption for a given operation
        GATT_ERROR = 133,                   // (0x85) Generic error
        GATT_CONNECTION_CONGESTED = 143,    // (0x8f) A remote device connection is congested.
        GATT_FAILURE = 257,                 // (0x101) A GATT operation failed, errors other than the above

        // Other GATT errors not in the Android doc
        GATT_InvalidHandle = 1,
        //GATT_ReadNotPermitted = 2,
        //GATT_WriteNotPermitted = 3,
        GATT_InvalidPdu = 4,
        //GATT_InsufficientAuthentication = 5,
        //GATT_RequestNotSupported = 6,
        //GATT_InvalidOffset = 7,
        GATT_InsufficientAuthorization = 8,
        GATT_PrepareQueueFull = 9,
        GATT_AttributeNotFound = 10,
        GATT_AttributeNotLong = 11,
        GATT_InsufficientEncryptionKeySize = 12,
        //GATT_InvalidAttributeValueLength = 13,
        GATT_UnlikelyError = 14,
        //GATT_InsufficientEncryption = 15,
        GATT_UnsupportedGroupType = 16,
        GATT_InsufficientResources = 17,

        // From Nordic's FailCallback
        REASON_DEVICE_DISCONNECTED = -1,
        REASON_DEVICE_NOT_SUPPORTED = -2,
        REASON_NULL_ATTRIBUTE = -3,
        REASON_REQUEST_FAILED = -4,
        REASON_TIMEOUT = -5,
        REASON_VALIDATION = -6,
        REASON_CANCELLED = -7,
        REASON_BLUETOOTH_DISABLED = -100,

        // From Nordic's RequestCallback
        REASON_REQUEST_INVALID = -1000000,
    }

    internal enum AndroidConnectionEventReason
    {
        REASON_UNKNOWN = -1,
        REASON_SUCCESS = 0,             // Disconnection was initiated by the user
        REASON_TERMINATE_LOCAL_HOST = 1,// Host device initiated disconnection
        REASON_TERMINATE_PEER_USER = 2, // Remote device initiated graceful disconnection
        REASON_LINK_LOSS = 3,           // This reason is only reported when autoConnect=true,
                                        // and connection to the device was lost for any reason other than
                                        // graceful disconnection initiated by peer user,
                                        // in this case Android tries to reconnect automatically
        REASON_NOT_SUPPORTED = 4,       // Device doesn't have the required services
        REASON_CANCELLED = 5,           // Connection attempt was canceled
        REASON_TIMEOUT = 10,            // Connection timed out
    }

    internal sealed class AndroidNativeInterfaceImpl : INativeInterfaceImpl
    {
        #region INativeDevice and INativePeripheralHandleImpl implementations

        sealed class NativeBluetoothDevice : INativeDevice, IDisposable
        {
            public AndroidJavaObject JavaDevice { get; private set; }

            public bool IsValid => JavaDevice != null;

            public NativeBluetoothDevice(AndroidJavaObject device) { JavaDevice = device; }

            public void Dispose() { JavaDevice = null; }
        }

        sealed class NativePeripheral : INativePeripheralHandleImpl, IDisposable
        {
            public AndroidJavaObject JavaPeripheral { get; private set; }

            public bool IsValid => JavaPeripheral != null;

            public NativePeripheral(AndroidJavaObject peripheral) { JavaPeripheral = peripheral; }

            public void Dispose() { JavaPeripheral = null; }
        }

        #endregion

        const string PeripheralClassName = "com.systemic.bluetoothle.Peripheral";
        readonly AndroidJavaClass _scannerClass = new AndroidJavaClass("com.systemic.bluetoothle.Scanner");
        readonly AndroidJavaClass _peripheralClass = new AndroidJavaClass(PeripheralClassName);

        public bool Initialize(NativeBluetoothCallback onBluetoothEvent)
        {
#if UNITY_2018_3_OR_NEWER && UNITY_ANDROID
            bool is31OrAbove = false;
            try
            {
                int start = 4 + SystemInfo.operatingSystem.IndexOf("API-");
                int end = SystemInfo.operatingSystem.IndexOf(" ", start + 1);
                int apiLevel = int.Parse(SystemInfo.operatingSystem.Substring(start, end - start));
                is31OrAbove = apiLevel >= 31;
            }
            catch (Exception e)
            {
                Debug.LogError($"Got error while parsing operatingSystem = {SystemInfo.operatingSystem}");
                Debug.LogException(e);

            }
            if (is31OrAbove)
            {
                string scan = "android.permission.BLUETOOTH_SCAN";
                string connect = "android.permission.BLUETOOTH_CONNECT";
                if (!Permission.HasUserAuthorizedPermission(scan) || !Permission.HasUserAuthorizedPermission(connect))
                {
                    Debug.Log("Requesting scan & connect permission");
                    Permission.RequestUserPermissions(new string[] { scan, connect });
                }
            }
            else if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Debug.Log("Requesting fine location permission");
                Permission.RequestUserPermission(Permission.FineLocation);
            }
#endif
            //TODO bluetooth availability events
            onBluetoothEvent((_scannerClass != null) && (_peripheralClass != null) ? BluetoothStatus.Ready : BluetoothStatus.Disabled);
            return true;
        }

        public void Shutdown()
        {
        }

        public bool StartScan(string requiredServiceUuids, Action<INativeDevice, NativeAdvertisementDataJson> onScannedPeripheral)
        {
            var callback = new ScannerCallback((device, scanResult)
                => onScannedPeripheral(new NativeBluetoothDevice(device), scanResult));
                //TODO try/catch?

            _scannerClass.CallStatic(
                "startScan",
                requiredServiceUuids,
                callback);

            return true;
        }

        public void StopScan()
        {
            _scannerClass.CallStatic("stopScan");
        }

        public INativePeripheralHandleImpl CreatePeripheral(ulong bluetoothAddress, NativeConnectionEventCallback onConnectionEvent)
        {
            var device = _peripheralClass.CallStatic<AndroidJavaObject>(
                "getDeviceFromAddress",
                (long)bluetoothAddress);
            return device == null ? null :
                new NativePeripheral(new AndroidJavaObject(
                    PeripheralClassName,
                    device,
                    new ConnectionObserver(onConnectionEvent)));
        }

        public INativePeripheralHandleImpl CreatePeripheral(INativeDevice device, NativeConnectionEventCallback onConnectionEvent)
        {
            var javaDevice = ((NativeBluetoothDevice)device)?.JavaDevice;
            return javaDevice == null ? null :
                new NativePeripheral(new AndroidJavaObject(
                    PeripheralClassName,
                    javaDevice,
                    new ConnectionObserver(onConnectionEvent)));
        }

        public void ReleasePeripheral(INativePeripheralHandleImpl peripheralHandle)
        {
            ((NativePeripheral)peripheralHandle).Dispose();
        }

        public void ConnectPeripheral(INativePeripheralHandleImpl peripheralHandle, string requiredServicesUuids, bool autoReconnect, NativeRequestResultCallback onResult)
        {
            GetJavaPeripheral(peripheralHandle, onResult)?.Call(
                "connect",
                requiredServicesUuids,
                autoReconnect,
                new RequestCallback(RequestOperation.ConnectPeripheral, onResult));
        }

        public void DisconnectPeripheral(INativePeripheralHandleImpl peripheralHandle, NativeRequestResultCallback onResult)
        {
            GetJavaPeripheral(peripheralHandle, onResult)?.Call(
                "disconnect",
                new RequestCallback(RequestOperation.DisconnectPeripheral, onResult));
        }

        public string GetPeripheralName(INativePeripheralHandleImpl peripheralHandle)
        {
            return GetJavaPeripheral(peripheralHandle)?.Call<string>("getName");
        }

        public int GetPeripheralMtu(INativePeripheralHandleImpl peripheralHandle)
        {
            return GetJavaPeripheral(peripheralHandle)?.Call<int>("getMtu") ?? 0;
        }

        public void RequestPeripheralMtu(INativePeripheralHandleImpl peripheralHandle, int mtu, NativeValueRequestResultCallback<int> onMtuResult)
        {
            GetJavaPeripheral(peripheralHandle, status => onMtuResult(0, status))?.Call(
                "requestMtu",
                mtu,
                new MtuRequestCallback(onMtuResult));
        }

        public void ReadPeripheralRssi(INativePeripheralHandleImpl peripheralHandle, NativeValueRequestResultCallback<int> onRssiRead)
        {
            GetJavaPeripheral(peripheralHandle, status => onRssiRead(int.MinValue, status))?.Call(
                "readRssi",
                new ReadRssiRequestCallback(onRssiRead));
        }

        public string GetDiscoveredServices(INativePeripheralHandleImpl peripheralHandle)
        {
            return GetJavaPeripheral(peripheralHandle)?.Call<string>("getDiscoveredServices");
        }

        public string GetServiceCharacteristics(INativePeripheralHandleImpl peripheralHandle, string serviceUuid)
        {
            return GetJavaPeripheral(peripheralHandle)?.Call<string>(
                "getServiceCharacteristics",
                serviceUuid);
        }

        public CharacteristicProperties GetCharacteristicProperties(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex)
        {
            return (CharacteristicProperties)GetJavaPeripheral(peripheralHandle)?.Call<int>(
                "getCharacteristicProperties",
                serviceUuid,
                characteristicUuid,
                (int)instanceIndex);
        }

        public void ReadCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeValueRequestResultCallback<byte[]> onValueRead)
        {
            GetJavaPeripheral(peripheralHandle, err => onValueRead(null, err))?.Call(
                "readCharacteristic",
                serviceUuid,
                characteristicUuid,
                (int)instanceIndex,
                new ReadValueRequestCallback(onValueRead));
        }

        public void WriteCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, byte[] data, bool withoutResponse, NativeRequestResultCallback onResult)
        {
            GetJavaPeripheral(peripheralHandle, onResult)?.Call(
                "writeCharacteristic",
                serviceUuid,
                characteristicUuid,
                (int)instanceIndex,
                JavaUtils.ToSignedArray(data),
                withoutResponse,
                new RequestCallback(RequestOperation.WriteCharacteristic, onResult));
        }

        // No notification with error on Android
        public void SubscribeCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeValueRequestResultCallback<byte[]> onValueChanged, NativeRequestResultCallback onResult)
        {
            GetJavaPeripheral(peripheralHandle, onResult)?.Call(
                "subscribeCharacteristic",
                serviceUuid,
                characteristicUuid,
                (int)instanceIndex,
                new DataReceivedCallback(onValueChanged),
                new RequestCallback(RequestOperation.SubscribeCharacteristic, onResult));
        }

        public void UnsubscribeCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeRequestResultCallback onResult)
        {
            GetJavaPeripheral(peripheralHandle, onResult)?.Call(
                "unsubscribeCharacteristic",
                serviceUuid,
                characteristicUuid,
                (int)instanceIndex,
                new RequestCallback(RequestOperation.UnsubscribeCharacteristic, onResult));
        }

        AndroidJavaObject GetJavaPeripheral(INativePeripheralHandleImpl peripheralHandle, NativeRequestResultCallback onResult = null)
        {
            var javaPeripheral = ((NativePeripheral)peripheralHandle).JavaPeripheral;
            if (javaPeripheral == null)
            {
                onResult?.Invoke(RequestStatus.InvalidPeripheral);
            }
            return javaPeripheral;
        }

        public static RequestStatus ToRequestStatus(int androidStatus)
        {
            if ((androidStatus > 0) && (androidStatus < 50))
            {
                return RequestStatus.ProtocolError;
            }
            else return androidStatus switch
            {
                (int)AndroidRequestStatus.GATT_SUCCESS => RequestStatus.Success,
                //(int)AndroidRequestStatus.GATT_ERROR => RequestStatus.Error,
                //(int)AndroidRequestStatus.GATT_CONNECTION_CONGESTED => RequestStatus.Error,
                //(int)AndroidRequestStatus.GATT_FAILURE => RequestStatus.Error,
                (int)AndroidRequestStatus.REASON_DEVICE_DISCONNECTED => RequestStatus.Disconnected,
                //(int)AndroidRequestStatus.REASON_DEVICE_NOT_SUPPORTED => RequestStatus.Error,
                (int)AndroidRequestStatus.REASON_NULL_ATTRIBUTE => RequestStatus.InvalidParameters,
                //(int)AndroidRequestStatus.REASON_REQUEST_FAILED => RequestStatus.Error,
                (int)AndroidRequestStatus.REASON_TIMEOUT => RequestStatus.Timeout,
                //(int)AndroidRequestStatus.REASON_VALIDATION => RequestStatus.Error,
                (int)AndroidRequestStatus.REASON_CANCELLED => RequestStatus.Canceled,
                (int)AndroidRequestStatus.REASON_BLUETOOTH_DISABLED => RequestStatus.AdapterOff,
                (int)AndroidRequestStatus.REASON_REQUEST_INVALID => RequestStatus.InvalidCall,
                _ => RequestStatus.Error
            };
        }

        public static ConnectionEventReason ToConnectionEventReason(int androidReason)
        {
            return androidReason switch
            {
                (int)AndroidConnectionEventReason.REASON_SUCCESS => ConnectionEventReason.Success,
                (int)AndroidConnectionEventReason.REASON_TERMINATE_LOCAL_HOST => ConnectionEventReason.AdapterOff,
                (int)AndroidConnectionEventReason.REASON_TERMINATE_PEER_USER => ConnectionEventReason.Peripheral,
                (int)AndroidConnectionEventReason.REASON_LINK_LOSS => ConnectionEventReason.LinkLoss,
                (int)AndroidConnectionEventReason.REASON_NOT_SUPPORTED => ConnectionEventReason.NotSupported,
                (int)AndroidConnectionEventReason.REASON_CANCELLED => ConnectionEventReason.Canceled,
                (int)AndroidConnectionEventReason.REASON_TIMEOUT => ConnectionEventReason.Timeout,
                _ => ConnectionEventReason.Unknown,
            };
        }
    }
}
