using System;

namespace Systemic.Unity.BluetoothLE.Internal
{
    /// <summary>
    /// Interface to be implemented for each supported platform, used as an opaque object for a bluetooth device.
    /// </summary>
    internal interface INativeDevice
    {
        /// <summary>
        /// Indicates if the native device is valid.
        /// </summary>
        bool IsValid { get; }
    }

    /// <summary>
    /// Interface to be implemented for each supported platform, used as an opaque object for a BLE peripheral.
    /// </summary>
    internal interface INativePeripheralHandleImpl
    {
        /// <summary>
        /// Indicates if the native peripheral handle is valid.
        /// </summary>
        bool IsValid { get; }
    }

    /// <summary>
    /// The interface for the BLE operations to be implemented for each supported platform.
    /// See <see cref="NativeInterface"/> for more details.
    /// </summary>
    internal interface INativeInterfaceImpl
    {
        bool Initialize(NativeBluetoothCallback onBluetoothEvent);

        void Shutdown();

        bool StartScan(string requiredServiceUuids, Action<INativeDevice, NativeAdvertisementDataJson> onScannedPeripheral);

        void StopScan();

        INativePeripheralHandleImpl CreatePeripheral(ulong bluetoothAddress, NativeConnectionEventCallback onConnectionEvent);

        INativePeripheralHandleImpl CreatePeripheral(INativeDevice device, NativeConnectionEventCallback onConnectionEvent);

        void ReleasePeripheral(INativePeripheralHandleImpl peripheralHandle);

        void ConnectPeripheral(INativePeripheralHandleImpl peripheralHandle, string requiredServicesUuids, bool autoReconnect, NativeRequestResultCallback onResult);

        void DisconnectPeripheral(INativePeripheralHandleImpl peripheralHandle, NativeRequestResultCallback onResult);

        string GetPeripheralName(INativePeripheralHandleImpl peripheralHandle);

        int GetPeripheralMtu(INativePeripheralHandleImpl peripheralHandle);

        void RequestPeripheralMtu(INativePeripheralHandleImpl peripheralHandle, int mtu, NativeValueRequestResultCallback<int> onMtuResult);

        void ReadPeripheralRssi(INativePeripheralHandleImpl peripheralHandle, NativeValueRequestResultCallback<int> onRssiRead);

        string GetDiscoveredServices(INativePeripheralHandleImpl peripheralHandle);

        string GetServiceCharacteristics(INativePeripheralHandleImpl peripheralHandle, string serviceUuid);

        CharacteristicProperties GetCharacteristicProperties(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex);

        void ReadCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeValueRequestResultCallback<byte[]> onValueRead);

        void WriteCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, byte[] data, bool withoutResponse, NativeRequestResultCallback onResult);

        void SubscribeCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeValueRequestResultCallback<byte[]> onValueChanged, NativeRequestResultCallback onResult);

        void UnsubscribeCharacteristic(INativePeripheralHandleImpl peripheralHandle, string serviceUuid, string characteristicUuid, uint instanceIndex, NativeRequestResultCallback onResult);
    }
}
