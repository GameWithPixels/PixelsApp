using AOT;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

using RequestIndex = System.Int32; // Can't have uint because Interlocked.Increment() only has support for signed integer in the version of .NET framework used by Unity at this time

//TODO at some point we want to marshal Guid values instead of strings, purely for optimization reasons
namespace Systemic.Unity.BluetoothLE.Internal.Apple
{
    internal sealed class AppleNativeInterfaceImpl : INativeInterfaceImpl
    {
        #region INativeDevice and INativePeripheralHandleImpl implementations

        sealed class NativeCBPeripheral : INativeDevice
        {
            public string PeripheralId { get; }

            public bool IsValid => PeripheralId != null;

            public NativeCBPeripheral(string peripheralId)
                => PeripheralId = peripheralId;
        }

        sealed class NativePxPeripheral : INativePeripheralHandleImpl
        {
            Dictionary<string, RequestIndex> _valueChangedRequestIndices = new Dictionary<string, RequestIndex>();

            public string PeripheralId { get; }

            public bool IsValid => PeripheralId != null;

            public RequestIndex ConnectionEventRequestIndex { get; }

            public NativePxPeripheral(string peripheralId, RequestIndex connectionEventRequestIndex)
                => (PeripheralId, ConnectionEventRequestIndex) = (peripheralId, connectionEventRequestIndex);

            public void AddValueChangedHandlerRequestIndex(string serviceUuid, string characteristicUuid, uint instanceIndex, RequestIndex valueChangedRequestIndex)
            {
                lock (_valueChangedRequestIndices)
                {
                    _valueChangedRequestIndices[$"{serviceUuid}:{characteristicUuid}#{instanceIndex}"] = valueChangedRequestIndex;
                }
            }

            public RequestIndex GetAndRemoveValueChangedHandlerRequestIndex(string serviceUuid, string characteristicUuid, uint instanceIndex)
            {
                RequestIndex index = 0;
                lock (_valueChangedRequestIndices)
                {
                    string key = $"{serviceUuid}:{characteristicUuid}#{instanceIndex}";
                    Debug.Assert(_valueChangedRequestIndices.ContainsKey(key));
                    if (_valueChangedRequestIndices.TryGetValue(key, out index))
                    {
                        _valueChangedRequestIndices.Remove(key);
                    }
                }
                return index;
            }

            public void ForgetAllValueHandlers()
            {
                lock (_valueChangedRequestIndices)
                {
                    _valueChangedRequestIndices.Clear();
                }
            }
        }

        #endregion

        #region Native library bindings

        const string _libName =
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            "PixelsLibraryMacOS";
#elif UNITY_IOS
            "__Internal";
#else
            "unsupported";
#endif

        delegate void CentralStateUpdateCallback(bool isAvailable);
        delegate void DiscoveredPeripheralCallback(string advertisementDataJson);
        delegate void PeripheralConnectionEventCallback(RequestIndex requestIndex, string peripheralId, int connectionEvent, int reason);
        delegate void RequestStatusCallback(RequestIndex requestIndex, int errorCode);
        delegate void RssiReadEventCallback(RequestIndex requestIndex, int rssi, int errorCode);
        delegate void ValueReadCallback(RequestIndex requestIndex, IntPtr data, UIntPtr length, int errorCode);

        [DllImport(_libName)]
        private static extern bool sgBleInitialize(CentralStateUpdateCallback onCentralStateUpdate);

        [DllImport(_libName)]
        private static extern void sgBleShutdown();

        [DllImport(_libName)]
        private static extern bool sgBleStartScan(string requiredServicesUuids, bool allowDuplicates, DiscoveredPeripheralCallback onDiscoveredPeripheral);

        [DllImport(_libName)]
        private static extern void sgBleStopScan();

        [DllImport(_libName)]
        private static extern bool sgBleCreatePeripheral(string peripheralId, PeripheralConnectionEventCallback onConnectionEvent, RequestIndex requestIndex);

        [DllImport(_libName)]
        private static extern void sgBleReleasePeripheral(string peripheralId);

        [DllImport(_libName)]
        private static extern void sgBleConnectPeripheral(string peripheralId, string requiredServicesUuids, RequestStatusCallback onRequestStatus, RequestIndex requestIndex);

        [DllImport(_libName)]
        private static extern void sgBleDisconnectPeripheral(string peripheralId, RequestStatusCallback onRequestStatus, RequestIndex requestIndex);

        [DllImport(_libName)]
        private static extern string sgBleGetPeripheralName(string peripheralId);

        [DllImport(_libName)]
        private static extern int sgBleGetPeripheralMtu(string peripheralId);

        [DllImport(_libName)]
        private static extern void sgBleReadPeripheralRssi(string peripheralId, RssiReadEventCallback onRssiRead, RequestIndex requestIndex);

        [DllImport(_libName)]
        private static extern string sgBleGetDiscoveredServices(string peripheralId);

        [DllImport(_libName)]
        private static extern string sgBleGetServiceCharacteristics(string peripheralId, string serviceUuid);

        [DllImport(_libName)]
        private static extern ulong sgBleGetCharacteristicProperties(string peripheralId, string serviceUuid, string characteristicUuid, uint instanceIndex);

        [DllImport(_libName)]
        private static extern void sgBleReadCharacteristic(string peripheralId, string serviceUuid, string characteristicUuid, uint instanceIndex, ValueReadCallback onReadChanged, RequestIndex requestIndex);

        [DllImport(_libName)]
        private static extern void sgBleWriteCharacteristic(string peripheralId, string serviceUuid, string characteristicUuid, uint instanceIndex, IntPtr data, UIntPtr length, bool withoutResponse, RequestStatusCallback onRequestStatus, RequestIndex requestIndex);

        [DllImport(_libName)]
        private static extern void sgBleSetNotifyCharacteristic(string peripheralId, string serviceUuid, string characteristicUuid, uint instanceIndex, ValueReadCallback onValueChanged, RequestStatusCallback onRequestStatus, RequestIndex requestIndex);

        #endregion

        #region Handlers management

        static NativeBluetoothCallback _onBluetoothEvent;
        static DiscoveredPeripheralCallback _onDiscoveredPeripheral; // We can have only one scan at a time, so one handler
        static Dictionary<RequestIndex, NativeConnectionEventCallback> _onConnectionEventHandlers = new Dictionary<RequestIndex, NativeConnectionEventCallback>();
        static Dictionary<RequestIndex, (RequestOperation, NativeRequestResultCallback)> _onRequestStatusHandlers = new Dictionary<RequestIndex, (RequestOperation, NativeRequestResultCallback)>();
        static Dictionary<RequestIndex, NativeValueRequestResultCallback<int>> _onRssiReadHandlers = new Dictionary<RequestIndex, NativeValueRequestResultCallback<int>>();
        static Dictionary<RequestIndex, NativeValueRequestResultCallback<byte[]>> _onValueReadHandlers = new Dictionary<RequestIndex, NativeValueRequestResultCallback<byte[]>>();
        static volatile RequestIndex _requestIndex;

        [MonoPInvokeCallback(typeof(CentralStateUpdateCallback))]
        static void OnCentralStateUpdate(bool available)
        {
            try
            {
                _onBluetoothEvent(available ? BluetoothStatus.Enabled : BluetoothStatus.Disabled);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(DiscoveredPeripheralCallback))]
        static void OnDiscoveredPeripheral(string advertisementDataJson)
        {
            try
            {
                _onDiscoveredPeripheral(advertisementDataJson);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(PeripheralConnectionEventCallback))]
        static void OnPeripheralConnectionEvent(RequestIndex requestIndex, string peripheralId, int connectionEvent, int reason)
        {
            try
            {
                // Get C# callback
                NativeConnectionEventCallback handler;
                lock (_onConnectionEventHandlers)
                {
                    _onConnectionEventHandlers.TryGetValue(requestIndex, out handler);
                }

                if (handler != null)
                {
                    // Notify user code
                    handler((ConnectionEvent)connectionEvent, (ConnectionEventReason)reason);
                }
                else
                {
                    // Callback not found
                    Debug.LogError($"[BLE] ConnectionEvent handler #{requestIndex} not found, request connection event and reason: {(ConnectionEvent)connectionEvent}, {(ConnectionEventReason)reason}");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(RequestStatusCallback))]
        static void OnRequestStatus(RequestIndex requestIndex, int errorCode)
        {
            try
            {
                var err = (AppleBluetoothError)errorCode;

                // Get C# callback
                RequestOperation op;
                NativeRequestResultCallback handler;
                lock (_onConnectionEventHandlers)
                {
                    if (_onRequestStatusHandlers.TryGetValue(requestIndex, out (RequestOperation, NativeRequestResultCallback) item))
                    {
                        _onRequestStatusHandlers.Remove(requestIndex);
                    }
                    (op, handler) = item;
                }

                if (handler != null)
                {
                    // Log success or error
                    if (err == AppleBluetoothError.Success)
                    {
                        Debug.Log($"[BLE] {op} ==> Request successful");
                    }
                    else
                    {
                        Debug.LogError($"[BLE] {op} ==> Request failed: {err} ({errorCode})");
                    }

                    // Notify user code
                    handler(ToRequestStatus(errorCode));
                }
                else
                {
                    // Callback not found
                    Debug.LogError($"[BLE] RequestStatus handler #{requestIndex} not found, request error code: {err} (0x{errorCode:X})");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(RssiReadEventCallback))]
        static void OnRssiReadHandler(RequestIndex requestIndex, int rssi, int errorCode)
        {
            try
            {
                var err = (AppleBluetoothError)errorCode;

                // Get C# callback
                NativeValueRequestResultCallback<int> handler;
                lock (_onRssiReadHandlers)
                {
                    _onRssiReadHandlers.TryGetValue(requestIndex, out handler);
                }

                if (handler != null)
                {
                    // Log success or error
                    if (err == AppleBluetoothError.Success)
                    {
                        Debug.Log($"[BLE] {RequestOperation.ReadPeripheralRssi} ==> Request successful");
                    }
                    else
                    {
                        Debug.LogError($"[BLE] {RequestOperation.ReadPeripheralRssi} ==> Request failed: {err} ({errorCode})");
                    }

                    // Notify user code
                    handler(rssi, ToRequestStatus(errorCode));
                }
                else
                {
                    // Callback not found
                    Debug.LogError($"[BLE] RssiReadHandler handler #{requestIndex} not found, request error code: {err} (0x{errorCode:X})");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MonoPInvokeCallback(typeof(ValueReadCallback))]
        static void OnValueReadHandler(RequestIndex requestIndex, IntPtr data, UIntPtr length, int errorCode)
        {
            try
            {
                // Get C# callback
                NativeValueRequestResultCallback<byte[]> handler;
                lock (_onValueReadHandlers)
                {
                    _onValueReadHandlers.TryGetValue(requestIndex, out handler);
                }

                if (handler != null)
                {
                    // Notify user code
                    handler(UnmanagedBuffer.ToArray(data, length), ToRequestStatus(errorCode));
                }
                else
                {
                    Debug.LogError($"[BLE] ValueRead handler #{requestIndex} not found, request error code: {(AppleBluetoothError)errorCode} (0x{errorCode:X})");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        #endregion

        public bool Initialize(NativeBluetoothCallback onBluetoothEvent)
        {
            _onBluetoothEvent = onBluetoothEvent;
            return sgBleInitialize(OnCentralStateUpdate);
        }

        public void Shutdown()
        {
            sgBleShutdown();
        }

        public bool StartScan(string requiredServiceUuids, Action<INativeDevice, NativeAdvertisementDataJson> onScannedPeripheral)
        {
            _onDiscoveredPeripheral = jsonStr =>
            {
                //Debug.Log($"[BLE] Scan ==> onScanResult: {jsonStr}");
                try
                {
                    var adv = JsonUtility.FromJson<NativeAdvertisementDataJson>(jsonStr);
                    onScannedPeripheral(new NativeCBPeripheral(adv.systemId), adv);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };

            bool allowDuplicates = true; //TODO not good for battery life
            return sgBleStartScan(requiredServiceUuids, allowDuplicates, OnDiscoveredPeripheral);
        }

        public void StopScan()
        {
            sgBleStopScan();
        }

        // Not available on Apple systems
        public INativePeripheralHandleImpl CreatePeripheral(ulong bluetoothAddress, NativeConnectionEventCallback onConnectionEvent)
        {
            return null;
        }

        public INativePeripheralHandleImpl CreatePeripheral(INativeDevice device, NativeConnectionEventCallback onConnectionEvent)
        {
            var requestIndex = Interlocked.Increment(ref _requestIndex);
            lock (_onConnectionEventHandlers)
            {
                _onConnectionEventHandlers.Add(requestIndex, onConnectionEvent);
            }

            string peripheralId = ((NativeCBPeripheral)device).PeripheralId;
            bool success = sgBleCreatePeripheral(peripheralId, OnPeripheralConnectionEvent, requestIndex);
            return success ? new NativePxPeripheral(peripheralId, requestIndex) : null;
        }

        public void ReleasePeripheral(INativePeripheralHandleImpl peripheralHandle)
        {
            var pxPeripheral = (NativePxPeripheral)peripheralHandle;
            sgBleReleasePeripheral(pxPeripheral.PeripheralId);
            lock (_onConnectionEventHandlers)
            {
                _onConnectionEventHandlers.Remove(pxPeripheral.ConnectionEventRequestIndex);
            }
        }

        //TODO on iOS connect waits indefinitely and autoReconnect is ignored
        public void ConnectPeripheral(INativePeripheralHandleImpl peripheralHandle, string requiredServicesUuids, bool autoReconnect, NativeRequestResultCallback onResult)
        {
            var requestIndex = SetRequestHandler(RequestOperation.ConnectPeripheral, onResult);

            sgBleConnectPeripheral(GetPeripheralId(peripheralHandle), requiredServicesUuids, OnRequestStatus, requestIndex);
        }

        public void DisconnectPeripheral(INativePeripheralHandleImpl peripheralHandle, NativeRequestResultCallback onResult)
        {
            var pxPeripheral = (NativePxPeripheral)peripheralHandle;
            var requestIndex = SetRequestHandler(RequestOperation.DisconnectPeripheral, onResult);

            sgBleDisconnectPeripheral(pxPeripheral.PeripheralId, OnRequestStatus, requestIndex);
            pxPeripheral.ForgetAllValueHandlers();
        }

        public string GetPeripheralName(INativePeripheralHandleImpl peripheralHandle)
        {
            return sgBleGetPeripheralName(GetPeripheralId(peripheralHandle));
        }

        public int GetPeripheralMtu(INativePeripheralHandleImpl peripheralHandle)
        {
            return sgBleGetPeripheralMtu(GetPeripheralId(peripheralHandle));
        }

        public void RequestPeripheralMtu(INativePeripheralHandleImpl peripheralHandle, int mtu, NativeValueRequestResultCallback<int> onMtuResult)
        {
            // No support for MTU request with Apple Core Bluetooth, we just return the automatically negotiated MTU
            onMtuResult(GetPeripheralMtu(peripheralHandle), RequestStatus.NotSupported);
        }

        public void ReadPeripheralRssi(INativePeripheralHandleImpl peripheralHandle, NativeValueRequestResultCallback<int> onRssiRead)
        {
            var requestIndex = Interlocked.Increment(ref _requestIndex);
            lock (_onRssiReadHandlers)
            {
                _onRssiReadHandlers.Add(requestIndex, onRssiRead);
            }

            sgBleReadPeripheralRssi(GetPeripheralId(peripheralHandle), OnRssiReadHandler, requestIndex);
        }

        public string GetDiscoveredServices(INativePeripheralHandleImpl peripheralHandle)
        {
            return sgBleGetDiscoveredServices(GetPeripheralId(peripheralHandle));
        }

        public string GetServiceCharacteristics(INativePeripheralHandleImpl peripheralHandle, string serviceUuid)
        {
            return sgBleGetServiceCharacteristics(GetPeripheralId(peripheralHandle), serviceUuid);
        }

        public CharacteristicProperties GetCharacteristicProperties(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex)
        {
            return (CharacteristicProperties)sgBleGetCharacteristicProperties(GetPeripheralId(peripheralHandle), serviceUuid, characteristicUuid, instanceIndex);
        }

        public void ReadCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeValueRequestResultCallback<byte[]> onValueRead)
        {
            var requestIndex = Interlocked.Increment(ref _requestIndex);
            lock (_onValueReadHandlers)
            {
                _onValueReadHandlers.Add(requestIndex, onValueRead);
            }

            sgBleReadCharacteristic(GetPeripheralId(peripheralHandle), serviceUuid, characteristicUuid, instanceIndex, OnValueReadHandler, requestIndex);
            //TODO remove handler once value is read
        }

        public void WriteCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, byte[] data, bool withoutResponse, NativeRequestResultCallback onResult)
        {
            var requestIndex = SetRequestHandler(RequestOperation.WriteCharacteristic, onResult);

            var (ptr, length) = UnmanagedBuffer.AllocUnmanagedBuffer(data);
            try
            {
                sgBleWriteCharacteristic(GetPeripheralId(peripheralHandle), serviceUuid, characteristicUuid, instanceIndex, ptr, (UIntPtr)length, withoutResponse, OnRequestStatus, requestIndex);
            }
            finally
            {
                UnmanagedBuffer.FreeUnmanagedBuffer(ptr);
            }
        }

        public void SubscribeCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeValueRequestResultCallback<byte[]> onValueChanged, NativeRequestResultCallback onResult)
        {
            var pxPeripheral = (NativePxPeripheral)peripheralHandle;
            var requestIndex = SetRequestHandler(RequestOperation.SubscribeCharacteristic, onResult);
            lock (_onValueReadHandlers)
            {
                _onValueReadHandlers.Add(requestIndex, onValueChanged);
            }
            pxPeripheral.AddValueChangedHandlerRequestIndex(serviceUuid, characteristicUuid, instanceIndex, requestIndex);

            sgBleSetNotifyCharacteristic(pxPeripheral.PeripheralId, serviceUuid, characteristicUuid, instanceIndex, OnValueReadHandler, OnRequestStatus, requestIndex);
        }

        public void UnsubscribeCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeRequestResultCallback onResult)
        {
            var pxPeripheral = (NativePxPeripheral)peripheralHandle;
            var requestIndex = SetRequestHandler(RequestOperation.UnsubscribeCharacteristic, onResult);

            sgBleSetNotifyCharacteristic(pxPeripheral.PeripheralId, serviceUuid, characteristicUuid, instanceIndex, null, OnRequestStatus, requestIndex);
            requestIndex = pxPeripheral.GetAndRemoveValueChangedHandlerRequestIndex(serviceUuid, characteristicUuid, instanceIndex);
            lock (_onValueReadHandlers)
            {
                _onValueReadHandlers.Remove(requestIndex);
            }
        }

        private string GetPeripheralId(INativePeripheralHandleImpl peripheralHandle)
        {
            return ((NativePxPeripheral)peripheralHandle).PeripheralId;
        }

        private RequestIndex SetRequestHandler(RequestOperation operation, NativeRequestResultCallback onResult)
        {
            var requestIndex = Interlocked.Increment(ref _requestIndex);
            lock (_onRequestStatusHandlers)
            {
                _onRequestStatusHandlers.Add(requestIndex, (operation, onResult));
            }
            return requestIndex;
        }

        enum AppleBluetoothError : int
        {
            CBErrorUnknown = -1,                // An unknown error occurred.
            CBErrorInvalidParameters = -2,      // The specified parameters are invalid.
            CBErrorInvalidHandle = -3,          // The specified attribute handle is invalid.
            CBErrorNotConnected = -4,           // The device isn't currently connected.
            CBErrorOutOfSpace = -5,             // The device has run out of space to complete the intended operation.
            CBErrorOperationCancelled = -6,     // The error represents a canceled operation.
            CBErrorConnectionTimeout = -7,      // The connection timed out.
            CBErrorPeripheralDisconnected = -8, // The peripheral disconnected.
            CBErrorUUIDNotAllowed = -9,         // The specified UUID isn't permitted.
            CBErrorAlreadyAdvertising = -10,    // The peripheral is already advertising.
            CBErrorConnectionFailed = -11,      // The connection failed.
            CBErrorConnectionLimitReached = -12,// The device already has the maximum number of connections.
            CBErrorUnknownDevice = -13,         // The device is unknown.
            CBErrorOperationNotSupported = -14, // The operation isn't supported.

            CBATTErrorSuccess = 0x00,                       // The ATT command or request successfully completed.
            CBATTErrorInvalidHandle = 0x01,                 // The attribute handle is invalid on this peripheral.
            CBATTErrorReadNotPermitted = 0x02,              // The permissions prohibit reading the attribute?s value.
            CBATTErrorWriteNotPermitted = 0x03,             // The permissions prohibit writing the attribute?s value.
            CBATTErrorInvalidPdu = 0x04,                    // The attribute Protocol Data Unit (PDU) is invalid.
            CBATTErrorInsufficientAuthentication = 0x05,    // Reading or writing the attribute?s value failed for lack of authentication.
            CBATTErrorRequestNotSupported = 0x06,           // The attribute server doesn't support the request received from the client.
            CBATTErrorInvalidOffset = 0x07,                 // The specified offset value was past the end of the attribute?s value.
            CBATTErrorInsufficientAuthorization = 0x08,     // Reading or writing the attribute?s value failed for lack of authorization.
            CBATTErrorPrepareQueueFull = 0x09,              // The prepare queue is full, as a result of there being too many write requests in the queue.
            CBATTErrorAttributeNotFound = 0x0A,             // The attribute wasn't found within the specified attribute handle range.
            CBATTErrorAttributeNotLong = 0x0B,              // The ATT read blob request can?t read or write the attribute.
            CBATTErrorInsufficientEncryptionKeySize = 0x0C, // The encryption key size used for encrypting this link is insufficient.
            CBATTErrorInvalidAttributeValueLength = 0x0D,   // The length of the attribute?s value is invalid for the intended operation.
            CBATTErrorUnlikelyError = 0x0E,                 // The ATT request encountered an unlikely error and wasn't completed.
            CBATTErrorInsufficientEncryption = 0x0F,        // Reading or writing the attribute?s value failed for lack of encryption.
            CBATTErrorUnsupportedGroupType = 0x10,          // The attribute type isn't a supported grouping attribute as defined by a higher-layer specification.
            CBATTErrorInsufficientResources = 0x11,         // Resources are insufficient to complete the ATT request.

            Success = 0,
            InvalidPeripheralId = unchecked((int)0x80000001),
            Disconnected = unchecked((int)0x80000100),
            InvalidCall = unchecked((int)0x80000101),
            InvalidParameters = unchecked((int)0x80000102),
            Canceled = unchecked((int)0x80000103),
        }

        static RequestStatus ToRequestStatus(int errorCode)
        {
            if (errorCode > 0)
            {
                return RequestStatus.ProtocolError;
            }
            else return errorCode switch
            {
                0 => RequestStatus.Success,

                //(int)AppleBluetoothError.UnknownError => RequestStatus.Error,
                (int)AppleBluetoothError.InvalidPeripheralId => RequestStatus.InvalidPeripheral,
                (int)AppleBluetoothError.Disconnected => RequestStatus.Disconnected,
                (int)AppleBluetoothError.InvalidCall => RequestStatus.InvalidCall,
                (int)AppleBluetoothError.InvalidParameters => RequestStatus.InvalidParameters,
                (int)AppleBluetoothError.Canceled => RequestStatus.Canceled,

                (int)AppleBluetoothError.CBErrorInvalidParameters => RequestStatus.InvalidParameters,
                (int)AppleBluetoothError.CBErrorInvalidHandle => RequestStatus.InvalidParameters,
                (int)AppleBluetoothError.CBErrorNotConnected => RequestStatus.Disconnected,
                //(int)AppleBluetoothError.CBErrorOutOfSpace => RequestStatus.Error,
                (int)AppleBluetoothError.CBErrorOperationCancelled => RequestStatus.Canceled,
                (int)AppleBluetoothError.CBErrorConnectionTimeout => RequestStatus.Timeout,
                (int)AppleBluetoothError.CBErrorPeripheralDisconnected => RequestStatus.Disconnected,
                //(int)AppleBluetoothError.CBErrorUUIDNotAllowed => RequestStatus.Error,
                //(int)AppleBluetoothError.CBErrorAlreadyAdvertising => RequestStatus.Error,
                //(int)AppleBluetoothError.CBErrorConnectionFailed => RequestStatus.Error,
                //(int)AppleBluetoothError.CBErrorConnectionLimitReached => RequestStatus.Error,
                //(int)AppleBluetoothError.CBErrorUnknownDevice => RequestStatus.Error,
                (int)AppleBluetoothError.CBErrorOperationNotSupported => RequestStatus.NotSupported,

                _ => RequestStatus.Error,
            };
        }
    }
}
