// Ignore Spelling: Mtu Rssi Uuid Uuids

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Windows
{
    internal sealed class WinRTNativeInterfaceImpl : INativeInterfaceImpl
    {
        #region INativeDevice and INativePeripheralHandleImpl implementations

        sealed class NativeScannedPeripheral : INativeDevice
        {
            public ulong BluetoothAddress { get; }

            public string Name { get; }

            public bool IsValid => BluetoothAddress != 0;

            public NativeScannedPeripheral(ulong bluetoothAddress, string name)
                => (BluetoothAddress, Name) = (bluetoothAddress, name);
        }

        sealed class NativePeripheral : INativePeripheralHandleImpl
        {
            // Keep references to all callbacks so they are not reclaimed by the GC
            readonly PeripheralConnectionEventCallback _onPeripheralConnectionEvent;
            readonly HashSet<RequestStatusCallback> _requestStatusHandlers = new HashSet<RequestStatusCallback>();
            readonly Dictionary<string, ValueChangedCallback> _valueChangedHandlers = new Dictionary<string, ValueChangedCallback>();

            static readonly HashSet<NativePeripheral> _releasedPeripherals = new HashSet<NativePeripheral>();

            public NativePeripheral(ulong bluetoothAddress, string name, PeripheralConnectionEventCallback onPeripheralConnectionEvent)
                => (BluetoothAddress, Name, _onPeripheralConnectionEvent) = (bluetoothAddress, name, onPeripheralConnectionEvent);

            public ulong BluetoothAddress { get; }

            public string Name { get; }

            public bool IsValid => BluetoothAddress != 0;

            public void KeepRequestHandler(RequestStatusCallback onRequestStatus)
            {
                lock (_requestStatusHandlers)
                {
                    _requestStatusHandlers.Add(onRequestStatus);
                }
            }

            public void ForgetRequestHandler(RequestStatusCallback onRequestStatus)
            {
                lock (_requestStatusHandlers)
                {
                    Debug.Assert(_requestStatusHandlers.Contains(onRequestStatus));
                    _requestStatusHandlers.Remove(onRequestStatus);
                }
                CheckReleased();
            }

            public void KeepValueChangedHandler(string serviceUuid, string characteristicUuid, uint instanceIndex, ValueChangedCallback onValueChanged)
            {
                lock (_valueChangedHandlers)
                {
                    _valueChangedHandlers[$"{serviceUuid}:{characteristicUuid}#{instanceIndex}"] = onValueChanged;
                }
            }

            public void ForgetValueChangedHandler(string serviceUuid, string characteristicUuid, uint instanceIndex)
            {
                lock (_valueChangedHandlers)
                {
                    string key = $"{serviceUuid}:{characteristicUuid}#{instanceIndex}";
                    Debug.Assert(_valueChangedHandlers.ContainsKey(key));
                    _valueChangedHandlers.Remove(key);
                }
                CheckReleased();
            }

            //public void ForgetAllValueChangedHandlers()
            //{
            //    lock (_valueChangedHandlers)
            //    {
            //        _valueChangedHandlers.Clear();
            //    }
            //    CheckReleased();
            //}

            public void Release()
            {
                lock (_requestStatusHandlers)
                {
                    // Keep a reference to ourselves until all handlers have been cleared out
                    if ((_requestStatusHandlers.Count > 0) && _releasedPeripherals.Add(this))
                    {
                        Debug.Log($"[BLE:{Name}] Added to WinRT release list");
                    }
                }
            }

            void CheckReleased()
            {
                lock (_requestStatusHandlers)
                {
                    if ((_requestStatusHandlers.Count == 0) && _releasedPeripherals.Remove(this))
                    {
                        Debug.Log($"[BLE:{Name}] Removed from WinRT release list");
                    }
                }
            }

            //~NativePeripheral()
            //{
            //    Debug.LogError($"[BLE:{Name}] WinRT GC");
            //}
        }

        #endregion

        #region Native library bindings

        const string _libName =
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            "LibWinRTBle";
#else
            "unsupported";
#endif

        enum AdapterState { Unsupported, Unavailable, Disabled, Enabled };

        delegate void CentralStateUpdateCallback(AdapterState state);
        delegate void DiscoveredPeripheralCallback([MarshalAs(UnmanagedType.LPStr)] string advertisementDataJson);
        delegate void RequestStatusCallback(RequestStatus errorCode);
        delegate void PeripheralConnectionEventCallback(ulong peripheralId, int connectionEvent, int reason);
        delegate void ValueReadCallback(IntPtr data, UIntPtr length, RequestStatus errorCode);
        delegate void ValueChangedCallback(IntPtr data, UIntPtr length);

        [DllImport(_libName)]
        private static extern bool sgBleInitialize(bool apartmentSingleThreaded, CentralStateUpdateCallback onCentralStateUpdate);

        [DllImport(_libName)]
        private static extern void sgBleShutdown();

        [DllImport(_libName)]
        private static extern bool sgBleStartScan(string requiredServicesUuids, DiscoveredPeripheralCallback onDiscoveredPeripheral);

        [DllImport(_libName)]
        private static extern void sgBleStopScan();

        [DllImport(_libName)]
        private static extern bool sgBleCreatePeripheral(ulong peripheralId, PeripheralConnectionEventCallback onConnectionEvent);

        [DllImport(_libName)]
        private static extern void sgBleReleasePeripheral(ulong peripheralId);

        [DllImport(_libName)]
        private static extern void sgBleConnectPeripheral(ulong peripheralId, string requiredServicesUuids, bool autoReconnect, RequestStatusCallback onRequestStatus);

        [DllImport(_libName)]
        private static extern void sgBleDisconnectPeripheral(ulong peripheralId, RequestStatusCallback onRequestStatus);

        [DllImport(_libName)]
        private static extern string sgBleGetPeripheralName(ulong peripheralId);

        [DllImport(_libName)]
        private static extern int sgBleGetPeripheralMtu(ulong peripheralId);

        [DllImport(_libName)]
        private static extern string sgBleGetDiscoveredServices(ulong peripheralId);

        [DllImport(_libName)]
        private static extern string sgBleGetServiceCharacteristics(ulong peripheralId, string serviceUuid);

        [DllImport(_libName)]
        private static extern ulong sgBleGetCharacteristicProperties(ulong peripheralId, string serviceUuid, string characteristicUuid, uint instanceIndex);

        [DllImport(_libName)]
        private static extern void sgBleReadCharacteristic(ulong peripheralId, string serviceUuid, string characteristicUuid, uint instanceIndex, ValueReadCallback onValueRead);

        [DllImport(_libName)]
        private static extern void sgBleWriteCharacteristic(ulong peripheralId, string serviceUuid, string characteristicUuid, uint instanceIndex, IntPtr data, UIntPtr length, bool withoutResponse, RequestStatusCallback onRequestStatus);

        [DllImport(_libName)]
        private static extern void sgBleSetNotifyCharacteristic(ulong peripheralId, string serviceUuid, string characteristicUuid, uint instanceIndex, ValueChangedCallback onValueChanged, RequestStatusCallback onRequestStatus);

        #endregion

        // Keep a reference to state update and discovery callbacks so they are not reclaimed by the GC
        private static CentralStateUpdateCallback _onCentralStateUpdate;
        private static DiscoveredPeripheralCallback _onDiscoveredPeripheral;

        public bool Initialize(NativeBluetoothCallback onBluetoothEvent)
        {
            CentralStateUpdateCallback onCentralStateUpdate = state =>
            {
                if (onBluetoothEvent != null)
                {
                    var status = BluetoothStatus.Unknown;
                    switch (state)
                    {
                        case AdapterState.Unsupported:
                            status = BluetoothStatus.Unsupported;
                            break;
                        case AdapterState.Unavailable:
                            status = BluetoothStatus.Unavailable;
                            break;
                        case AdapterState.Disabled:
                            status = BluetoothStatus.Disabled;
                            break;
                        case AdapterState.Enabled:
                            status = BluetoothStatus.Ready;
                            break;
                    }
                    onBluetoothEvent(status);
                }
            };
            bool success = sgBleInitialize(true, onCentralStateUpdate);
            if (success)
            {
                _onCentralStateUpdate = onCentralStateUpdate;
            }
            return success;
        }

        public void Shutdown()
        {
            sgBleShutdown();
            // Keep callback _onCentralStateUpdate
        }

        public bool StartScan(string requiredServiceUuids, Action<INativeDevice, NativeAdvertisementDataJson> onScannedPeripheral)
        {
            DiscoveredPeripheralCallback onDiscoveredPeripheral = jsonStr =>
            {
                //Debug.Log($"[BLE] Scan ==> onScanResult: {jsonStr}");
                try
                {
                    var adv = JsonUtility.FromJson<NativeAdvertisementDataJson>(jsonStr);
                    onScannedPeripheral(new NativeScannedPeripheral(adv.address, adv.name), adv);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };
            // Starts a new scan if on is already in progress
            bool success = sgBleStartScan(requiredServiceUuids, onDiscoveredPeripheral);
            if (success)
            {
                // Store callback now that scan is started
                _onDiscoveredPeripheral = onDiscoveredPeripheral;
            }
            return success;
        }

        public void StopScan()
        {
            sgBleStopScan();
            _onDiscoveredPeripheral = null;
        }

        private INativePeripheralHandleImpl CreatePeripheral(ulong bluetoothAddress, string debugName, NativeConnectionEventCallback onConnectionEvent)
        {
            PeripheralConnectionEventCallback peripheralConnectionEventHandler = (ulong peripheralId, int connectionEvent, int reason) =>
            {
                try
                {
                    onConnectionEvent((ConnectionEvent)connectionEvent, (ConnectionEventReason)reason);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };

            bool success = sgBleCreatePeripheral(bluetoothAddress, peripheralConnectionEventHandler);
            return success ? new NativePeripheral(bluetoothAddress, debugName, peripheralConnectionEventHandler) : null;
        }

        public INativePeripheralHandleImpl CreatePeripheral(ulong bluetoothAddress, NativeConnectionEventCallback onConnectionEvent)
        {
            return CreatePeripheral(bluetoothAddress, bluetoothAddress.ToString(), onConnectionEvent);
        }

        public INativePeripheralHandleImpl CreatePeripheral(INativeDevice device, NativeConnectionEventCallback onConnectionEvent)
        {
            var p = (NativeScannedPeripheral)device;
            return CreatePeripheral(p.BluetoothAddress, p.Name, onConnectionEvent);
        }

        public void ReleasePeripheral(INativePeripheralHandleImpl peripheralHandle)
        {
            var periph = (NativePeripheral)peripheralHandle;
            periph.Release();
            sgBleReleasePeripheral(GetPeripheralAddress(peripheralHandle));
        }

        public void ConnectPeripheral(INativePeripheralHandleImpl peripheralHandle, string requiredServicesUuids, bool autoReconnect, NativeRequestResultCallback onResult)
        {
            sgBleConnectPeripheral(GetPeripheralAddress(peripheralHandle), requiredServicesUuids, autoReconnect,
                GetRequestStatusHandler(RequestOperation.ConnectPeripheral, peripheralHandle, onResult));
        }

        public void DisconnectPeripheral(INativePeripheralHandleImpl peripheralHandle, NativeRequestResultCallback onResult)
        {
            var periph = (NativePeripheral)peripheralHandle;
            sgBleDisconnectPeripheral(GetPeripheralAddress(peripheralHandle),
                GetRequestStatusHandler(RequestOperation.DisconnectPeripheral, peripheralHandle, onResult));
            //TODO use static C# callback that redirects to peripheral, we might still get a callback on a different thread...
            //periph.ForgetAllValueHandlers(); // We won't get such events anymore
        }

        public string GetPeripheralName(INativePeripheralHandleImpl peripheralHandle)
        {
            return sgBleGetPeripheralName(GetPeripheralAddress(peripheralHandle));
        }

        public int GetPeripheralMtu(INativePeripheralHandleImpl peripheralHandle)
        {
            return sgBleGetPeripheralMtu(GetPeripheralAddress(peripheralHandle));
        }

        public void RequestPeripheralMtu(INativePeripheralHandleImpl peripheralHandle, int mtu, NativeValueRequestResultCallback<int> onMtuResult)
        {
            // No support for MTU request with WinRT Bluetooth, we just return the automatically negotiated MTU
            onMtuResult(GetPeripheralMtu(peripheralHandle), RequestStatus.NotSupported);
        }

        public void ReadPeripheralRssi(INativePeripheralHandleImpl peripheralHandle, NativeValueRequestResultCallback<int> onRssiRead)
        {
            // No support for reading RSSI of connected device with WinRT Bluetooth
            onRssiRead(int.MinValue, RequestStatus.NotSupported);
        }

        public string GetDiscoveredServices(INativePeripheralHandleImpl peripheralHandle)
        {
            return sgBleGetDiscoveredServices(GetPeripheralAddress(peripheralHandle));
        }

        public string GetServiceCharacteristics(INativePeripheralHandleImpl peripheralHandle, string serviceUuid)
        {
            return sgBleGetServiceCharacteristics(GetPeripheralAddress(peripheralHandle), serviceUuid);
        }

        public CharacteristicProperties GetCharacteristicProperties(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex)
        {
            return (CharacteristicProperties)sgBleGetCharacteristicProperties(GetPeripheralAddress(peripheralHandle), serviceUuid, characteristicUuid, instanceIndex);
        }

        public void ReadCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeValueRequestResultCallback<byte[]> onValueRead)
        {
            var valueReadHandler = GetValueReadHandler(peripheralHandle, onValueRead);
            var periph = (NativePeripheral)peripheralHandle;
            //TODO store handler periph.KeepValueChangedHandler(serviceUuid, characteristicUuid, instanceIndex, valueReadHandler);
            sgBleReadCharacteristic(GetPeripheralAddress(peripheralHandle), serviceUuid, characteristicUuid, instanceIndex, valueReadHandler);
        }

        public void WriteCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, byte[] data, bool withoutResponse, NativeRequestResultCallback onResult)
        {
            var (ptr, length) = UnmanagedBuffer.AllocUnmanagedBuffer(data);
            try
            {
                sgBleWriteCharacteristic(GetPeripheralAddress(peripheralHandle), serviceUuid, characteristicUuid, instanceIndex, ptr, (UIntPtr)length, withoutResponse,
                    GetRequestStatusHandler(RequestOperation.WriteCharacteristic, peripheralHandle, onResult));
            }
            finally
            {
                UnmanagedBuffer.FreeUnmanagedBuffer(ptr);
            }
        }

        public void SubscribeCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeValueRequestResultCallback<byte[]> onValueChanged, NativeRequestResultCallback onResult)
        {
            var valueChangedHandler = GetValueChangedHandler(peripheralHandle, onValueChanged);
            var periph = (NativePeripheral)peripheralHandle;
            //TODO the call below will replace an previous handler => keep it until sgBleSetNotifyCharacteristic() has returned
            periph.KeepValueChangedHandler(serviceUuid, characteristicUuid, instanceIndex, valueChangedHandler);
            sgBleSetNotifyCharacteristic(GetPeripheralAddress(peripheralHandle), serviceUuid, characteristicUuid, instanceIndex, valueChangedHandler,
                GetRequestStatusHandler(RequestOperation.SubscribeCharacteristic, peripheralHandle, onResult));
        }

        public void UnsubscribeCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeRequestResultCallback onResult)
        {
            sgBleSetNotifyCharacteristic(GetPeripheralAddress(peripheralHandle), serviceUuid, characteristicUuid, instanceIndex, null,
                GetRequestStatusHandler(RequestOperation.UnsubscribeCharacteristic, peripheralHandle, onResult));
            var periph = (NativePeripheral)peripheralHandle;
            periph.ForgetValueChangedHandler(serviceUuid, characteristicUuid, instanceIndex);
        }

        private ulong GetPeripheralAddress(INativePeripheralHandleImpl peripheralHandle)
        {
            return ((NativePeripheral)peripheralHandle).BluetoothAddress;
        }

        private RequestStatusCallback GetRequestStatusHandler(RequestOperation operation, INativePeripheralHandleImpl peripheralHandle, NativeRequestResultCallback onResult)
        {
            var periph = (NativePeripheral)peripheralHandle;
            RequestStatusCallback onRequestStatus = null;
            onRequestStatus = errorCode =>
            {
                try
                {
                    // Log success or error
                    if (errorCode == RequestStatus.Success)
                    {
                        Debug.Log($"[BLE:{periph.Name}] {operation} ==> Request successful");
                    }
                    else
                    {
                        Debug.LogError($"[BLE:{periph.Name}] {operation} ==> Request failed: {errorCode}");
                    }

                    // We can forget about this handler instance, it won't be called anymore
                    periph.ForgetRequestHandler(onRequestStatus);

                    // Notify user code
                    onResult(errorCode);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };
            periph.KeepRequestHandler(onRequestStatus);
            return onRequestStatus;
        }

        private ValueReadCallback GetValueReadHandler(INativePeripheralHandleImpl peripheralHandle, NativeValueRequestResultCallback<byte[]> onValueRead)
        {
            var periph = (NativePeripheral)peripheralHandle;
            ValueReadCallback valueChangedHandler = (IntPtr data, UIntPtr length, RequestStatus status) =>
            {
                try
                {
                    onValueRead(UnmanagedBuffer.ToArray(data, length), status);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };

            return valueChangedHandler;
        }

        private ValueChangedCallback GetValueChangedHandler(INativePeripheralHandleImpl peripheralHandle, NativeValueRequestResultCallback<byte[]> onValueChanged)
        {
            var periph = (NativePeripheral)peripheralHandle;
            ValueChangedCallback valueChangedHandler = (IntPtr data, UIntPtr length) =>
            {
                try
                {
                    var array = UnmanagedBuffer.ToArray(data, length);
                    onValueChanged(array, array != null ? RequestStatus.Success : RequestStatus.Error);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };

            return valueChangedHandler;
        }
    }
}
