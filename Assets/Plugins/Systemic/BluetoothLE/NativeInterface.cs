using System;
using System.Collections.Generic;
using System.Linq;
using Systemic.Unity.BluetoothLE.Internal;

namespace Systemic.Unity.BluetoothLE
{
    /// <summary>
    /// Opaque and read only class storing the native peripheral handle as used
    /// by the platform specific <see cref="INativeInterfaceImpl"/> implementation.
    /// </summary>
    public struct NativePeripheralHandle
    {
        /// <summary>
        /// Initializes an instance with the given native peripheral handle.
        /// </summary>
        /// <param name="client"></param>
        internal NativePeripheralHandle(INativePeripheralHandleImpl client) => NativePeripheral = client;

        /// <summary>
        /// Gets the native peripheral handle.
        /// </summary>
        internal INativePeripheralHandleImpl NativePeripheral { get; }

        /// <summary>
        /// Indicates whether the peripheral handle is valid.
        /// </summary>
        public bool IsValid => NativePeripheral?.IsValid ?? false;
    }

    /// <summary>
    /// A static class that abstracts each platform specific BLE support and offers a unified interface
    /// to the Unity programmer.
    /// 
    /// Each platform (Windows, iOS, Android) has specific APIs for managing Bluetooth Low Energy (BLE)
    /// peripherals and requires using their native language (respectively C++, Objective-C, Java)
    /// through Unity plugins.
    ///
    /// This static class selects the appropriate native implementation at runtime based on the platform
    /// it is running on. It abstracts away the marshaling specificities of the different platforms.
    ///
    /// Each native implementation wraps the platform specific BLE APIs around a unified architecture
    /// so they can be used in a similar manner. However differences, sometimes subtle, will always exist
    /// between those implementations.
    ///
    /// See the <see cref="Central"/> class for a higher level access to BLE peripherals.
    /// </summary>
    /// <remarks>
    /// In this context, the word "native" refers to the platform specific code and data for managing
    /// BLE peripherals.
    /// </remarks>
    public static class NativeInterface
    {
        #region The underlying INativeInterfaceImpl implementation

        static INativeInterfaceImpl _impl =
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            new Internal.Windows.WinRTNativeInterfaceImpl();
#elif UNITY_EDITOR_OSX || UNITY_IOS || UNITY_STANDALONE_OSX
            new Internal.Apple.AppleNativeInterfaceImpl();
#elif UNITY_ANDROID
            new Internal.Android.AndroidNativeInterfaceImpl();
#else
            null;
#endif
        #endregion

        // Indicates whether we are successfully initialized
        static bool _isInitialized;

        /// <summary>
        /// The lowest Maximum Transmission Unit (MTU) value allowed by the BLE standard.
        /// </summary>
        public const int MinMtu = 23;

        /// <summary>
        /// The highest Maximum Transmission Unit (MTU) value allowed by the BLE standard.
        /// </summary>
        public const int MaxMtu = 517;

        //! \name Static class life cycle
        //! @{

        /// <summary>
        /// Initializes the underlying platform native implementation.
        /// </summary>
        /// <param name="onBluetoothEvent">Invoked when the host device Bluetooth state changes.</param>
        /// <returns>Whether the initialization was successful.</returns>
        public static bool Initialize(NativeBluetoothCallback onBluetoothEvent)
        {
            if (onBluetoothEvent == null) throw new ArgumentNullException(nameof(onBluetoothEvent));

            return _isInitialized = _impl.Initialize(onBluetoothEvent);
        }

        /// <summary>
        /// Shuts down the underlying platform native implementation.
        ///
        /// Scanning is stopped and all peripherals are disconnected and removed.
        /// </summary>
        public static void Shutdown()
        {
            _isInitialized = false;
            _impl.Shutdown();
        }

        //! @}
        //! \name Peripherals scanning
        //! @{

        /// <summary>
        /// Starts scanning for BLE peripherals advertising the given list of services.
        ///
        /// If a scan is already running, it is updated with the new parameters.
        ///
        /// Specifying one more service required for the peripherals saves battery on mobile devices.
        /// </summary>
        /// <param name="requiredServices">List of services that the peripheral should advertise, may be null or empty.</param>
        /// <param name="onScannedPeripheral">Invoked every time an advertisement packet with the required services is received.</param>
        /// <returns>Whether the scan was successfully started.</returns>
        public static bool StartScan(IEnumerable<Guid> requiredServices, Action<ScannedPeripheral> onScannedPeripheral)
        {
            if (onScannedPeripheral == null) throw new ArgumentNullException(nameof(onScannedPeripheral));

            SanityCheck();

            return _impl.StartScan(UuidsToString(requiredServices),
                (device, advData) => onScannedPeripheral(new ScannedPeripheral(device, advData)));
        }

        /// <summary>
        /// Stops an on-going BLE scan.
        /// </summary>
        public static void StopScan()
        {
            _impl.StopScan();
        }

        //! @}
        //! \name Peripherals life cycle
        //! @{

        /// <summary>
        /// Requests the native implementation to create an object for the BLE peripheral with the given Bluetooth address.
        ///
        /// This method doesn't initiate a connection.
        /// </summary>
        /// <param name="bluetoothAddress">The BLE peripheral address.</param>
        /// <param name="onConnectionEventChanged">Invoked when the peripheral connection state changes.</param>
        /// <returns>
        /// Handle to the native object for the BLE peripheral.
        /// Returns <c>null</c> if an object was already returned for this peripheral, and not yet released.
        /// </returns>
        public static NativePeripheralHandle CreatePeripheral(ulong bluetoothAddress, NativeConnectionEventCallback onConnectionEventChanged)
        {
            if (bluetoothAddress == 0) throw new ArgumentException("Empty bluetooth address", nameof(bluetoothAddress));
            if (onConnectionEventChanged == null) throw new ArgumentNullException(nameof(onConnectionEventChanged));

            SanityCheck();

            return new NativePeripheralHandle(
                _impl.CreatePeripheral(bluetoothAddress, onConnectionEventChanged));
        }

        /// <summary>
        /// Requests the native implementation to create an object for the BLE peripheral associated
        /// with the given advertisement data passed with the <paramref name="scannedPeripheral"/> parameter.
        ///
        /// This method doesn't initiate a connection.
        /// </summary>
        /// <param name="scannedPeripheral">Some advertisement data for the BLE peripheral.</param>
        /// <param name="onConnectionEvent">Invoked when the peripheral connection state changes.</param>
        /// <returns>
        /// Handle to the native object for the BLE peripheral.
        /// Returns <c>null</c> if an object was already returned for this peripheral, and not yet released.
        /// </returns>
        public static NativePeripheralHandle CreatePeripheral(ScannedPeripheral scannedPeripheral, NativeConnectionEventCallback onConnectionEvent)
        {
            if (scannedPeripheral == null) throw new ArgumentNullException(nameof(scannedPeripheral));
            if (scannedPeripheral.NativeDevice == null) throw new ArgumentException("Invalid ScannedPeripheral", nameof(scannedPeripheral));
            if (onConnectionEvent == null) throw new ArgumentNullException(nameof(onConnectionEvent));

            SanityCheck();

            return new NativePeripheralHandle(
                _impl.CreatePeripheral(scannedPeripheral.NativeDevice, onConnectionEvent));
        }

        /// <summary>
        /// Requests the native implementation to release the underlying native object for the BLE peripheral.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        public static void ReleasePeripheral(NativePeripheralHandle nativePeripheralHandle)
        {
            SanityCheck();

            if (nativePeripheralHandle.IsValid)
            {
                _impl.ReleasePeripheral(nativePeripheralHandle.NativePeripheral);
            }
        }

        //! @}
        //! \name Peripheral connection and disconnection
        //! @{

        /// <summary>
        /// Requests the native implementation to connect to the given peripheral.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <param name="requiredServices">List of services that the peripheral should support, may be null or empty.</param>
        /// <param name="autoReconnect">Whether the native implementation should attempt to automatically reconnect
        ///                             after an unexpected disconnection (i.e. not requested by a call
        ///                             to <see cref="DisconnectPeripheral"/>).</param>
        /// <param name="onResult">Invoked when the request has completed (successfully or not).</param>
        /// <remarks>
        /// The exact behavior may vary between platforms.
        /// Windows and Android implementations time out after a short delay (8 and 30 seconds respectively)
        /// whereas iOS never times out.
        /// </remarks>
        public static void ConnectPeripheral(NativePeripheralHandle nativePeripheralHandle, IEnumerable<Guid> requiredServices, bool autoReconnect, NativeRequestResultCallback onResult)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));
            if (onResult == null) throw new ArgumentNullException(nameof(onResult));

            SanityCheck();

            _impl.ConnectPeripheral(
                nativePeripheralHandle.NativePeripheral,
                UuidsToString(requiredServices),
                autoReconnect,
                onResult);
        }

        /// <summary>
        /// Requests the native implementation to disconnect the given peripheral.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <param name="onResult">Invoked when the request has completed (successfully or not).</param>
        public static void DisconnectPeripheral(NativePeripheralHandle nativePeripheralHandle, NativeRequestResultCallback onResult)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));
            if (onResult == null) throw new ArgumentNullException(nameof(onResult));

            SanityCheck();

            _impl.DisconnectPeripheral(nativePeripheralHandle.NativePeripheral, onResult);
        }

        //! @}
        //! \name Peripheral operations
        //! Valid only for connected peripherals.
        //! @{

        /// <summary>
        /// Gets the name of the given peripheral.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <returns>The peripheral name.</returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static string GetPeripheralName(NativePeripheralHandle nativePeripheralHandle)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));

            SanityCheck();

            return _impl.GetPeripheralName(nativePeripheralHandle.NativePeripheral);
        }

        /// <summary>
        /// Gets the Maximum Transmission Unit (MTU) for the given peripheral.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <returns>The peripheral MTU.</returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static int GetPeripheralMtu(NativePeripheralHandle nativePeripheralHandle)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));

            SanityCheck();

            return _impl.GetPeripheralMtu(nativePeripheralHandle.NativePeripheral);
        }

        /// <summary>
        /// Request the given peripheral to change its MTU to the given value (Android only).
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <param name="mtu">The requested MTU, see <see cref="MinMtu"/> and <see cref="MaxMtu"/> for the legal range of values.</param>
        /// <param name="onMtuResult">Invoked when the request has completed (successfully or not), with the updated MTU value.</param>
        /// <remarks>The peripheral must be connected.</remarks>
        public static void RequestPeripheralMtu(NativePeripheralHandle nativePeripheralHandle, int mtu, NativeValueRequestResultCallback<int> onMtuResult)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));
            if ((mtu < MinMtu) || (mtu > MaxMtu)) throw new ArgumentException($"MTU must be between {MinMtu} and {MaxMtu}", nameof(mtu));
            if (onMtuResult == null) throw new ArgumentNullException(nameof(onMtuResult));

            SanityCheck();

            _impl.RequestPeripheralMtu(nativePeripheralHandle.NativePeripheral, mtu, onMtuResult);
        }

        /// <summary>
        /// Request to read the Received Signal Strength Indicator (RSSI) of the given peripheral.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <param name="onRssiResult">Invoked when the request has completed (successfully or not), with the read RSSI value.</param>
        /// <remarks>The peripheral must be connected.</remarks>
        public static void ReadPeripheralRssi(NativePeripheralHandle nativePeripheralHandle, NativeValueRequestResultCallback<int> onRssiResult)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));
            if (onRssiResult == null) throw new ArgumentNullException(nameof(onRssiResult));

            SanityCheck();

            _impl.ReadPeripheralRssi(nativePeripheralHandle.NativePeripheral, onRssiResult);
        }

        //! @}
        //! \name Services operations
        //! Valid only for connected peripherals.
        //! @{

        /// <summary>
        /// Gets the list of discovered services for the given peripheral.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <returns>The list of discovered services.</returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static Guid[] GetPeripheralDiscoveredServices(NativePeripheralHandle nativePeripheralHandle)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));

            return StringToUuids(
                _impl.GetPeripheralDiscoveredServices(nativePeripheralHandle.NativePeripheral));
        }

        /// <summary>
        /// Gets the list of discovered characteristics for the given peripheral's service.
        /// 
        /// The same characteristic may be listed several times according to the peripheral's configuration.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <param name="serviceUuid">The service UUID for which to retrieve the characteristics.</param>
        /// <returns>The list of discovered characteristics of a service.</returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static Guid[] GetPeripheralServiceCharacteristics(NativePeripheralHandle nativePeripheralHandle, Guid serviceUuid)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));
            if (serviceUuid == Guid.Empty) throw new ArgumentException("Empty service UUID", nameof(serviceUuid));

            return StringToUuids(
                _impl.GetPeripheralServiceCharacteristics(nativePeripheralHandle.NativePeripheral, serviceUuid.ToString()));
        }

        //! @}
        //! \name Characteristics operations
        //! Valid only for connected peripherals.
        //! @{

        /// <summary>
        /// Gets the standard BLE properties of the specified service's characteristic for the given peripheral.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="instanceIndex">The instance index of the characteristic if listed more than once for the service, otherwise zero.</param>
        /// <returns>The standard BLE properties of a service's characteristic.</returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static CharacteristicProperties GetCharacteristicProperties(NativePeripheralHandle nativePeripheralHandle, Guid serviceUuid, Guid characteristicUuid, uint instanceIndex)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));
            if (serviceUuid == Guid.Empty) throw new ArgumentException("Empty service UUID", nameof(serviceUuid));
            if (characteristicUuid == Guid.Empty) throw new ArgumentException("Empty characteristic UUID", nameof(characteristicUuid));

            SanityCheck();

            return _impl.GetCharacteristicProperties(
                nativePeripheralHandle.NativePeripheral,
                serviceUuid.ToString(),
                characteristicUuid.ToString(),
                instanceIndex);
        }

        /// <summary>
        /// Requests to read the value of the specified service's characteristic for the given peripheral.
        /// 
        /// The call fails if the characteristic is not readable.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="instanceIndex">The instance index of the characteristic if listed more than once for the service, default is zero.</param>
        /// <param name="onValueReadResult">Invoked when the request has completed (successfully or not) and with the characteristic's read value on success.</param>
        /// <remarks>The peripheral must be connected.</remarks>
        public static void ReadCharacteristic(NativePeripheralHandle nativePeripheralHandle, Guid serviceUuid, Guid characteristicUuid, uint instanceIndex, NativeValueRequestResultCallback<byte[]> onValueReadResult)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));
            if (serviceUuid == Guid.Empty) throw new ArgumentException("Empty service UUID", nameof(serviceUuid));
            if (characteristicUuid == Guid.Empty) throw new ArgumentException("Empty characteristic UUID", nameof(characteristicUuid));
            if (onValueReadResult == null) throw new ArgumentNullException(nameof(onValueReadResult));

            SanityCheck();

            _impl.ReadCharacteristic(nativePeripheralHandle.NativePeripheral, serviceUuid.ToString(), characteristicUuid.ToString(), instanceIndex, onValueReadResult);
        }

        /// <summary>
        /// Requests to write to the specified service's characteristic for the given peripheral.
        /// 
        /// The call fails if the characteristic is not writable.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="instanceIndex">The instance index of the characteristic if listed more than once for the service, default is zero.</param>
        /// <param name="data">The data to write to the characteristic (may be empty).</param>
        /// <param name="withoutResponse">Whether to wait for the peripheral to respond that it has received the data.
        ///                               It's usually best to request a response.</param>
        /// <param name="onResult">Invoked when the request has completed (successfully or not).</param>
        /// <remarks>The peripheral must be connected.</remarks>
        public static void WriteCharacteristic(NativePeripheralHandle nativePeripheralHandle, Guid serviceUuid, Guid characteristicUuid, uint instanceIndex, byte[] data, bool withoutResponse, NativeRequestResultCallback onResult)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));
            if (serviceUuid == Guid.Empty) throw new ArgumentException("Empty service UUID", nameof(serviceUuid));
            if (characteristicUuid == Guid.Empty) throw new ArgumentException("Empty characteristic UUID", nameof(characteristicUuid));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (onResult == null) throw new ArgumentNullException(nameof(onResult));

            SanityCheck();

            _impl.WriteCharacteristic(
                nativePeripheralHandle.NativePeripheral,
                serviceUuid.ToString(),
                characteristicUuid.ToString(),
                instanceIndex,
                data,
                withoutResponse,
                onResult);
        }

        /// <summary>
        /// Requests to subscribe for value changes of the specified service's characteristic for the given peripheral.
        ///
        /// Replaces a previously registered value change handler for the same characteristic.
        /// The call fails if the characteristic doesn't support notifications.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="instanceIndex">The instance index of the characteristic if listed more than once for the service, default is zero.</param>
        /// <param name="onValueChanged">Invoked when the value of the characteristic changes.</param>
        /// <param name="onResult">Invoked when the request has completed (successfully or not).</param>
        /// <remarks>The peripheral must be connected.</remarks>
        public static void SubscribeCharacteristic(NativePeripheralHandle nativePeripheralHandle, Guid serviceUuid, Guid characteristicUuid, uint instanceIndex, NativeValueRequestResultCallback<byte[]> onValueChanged, NativeRequestResultCallback onResult)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));
            if (serviceUuid == Guid.Empty) throw new ArgumentException("Empty service UUID", nameof(serviceUuid));
            if (characteristicUuid == Guid.Empty) throw new ArgumentException("Empty characteristic UUID", nameof(characteristicUuid));
            if (onValueChanged == null) throw new ArgumentNullException(nameof(onValueChanged));
            if (onResult == null) throw new ArgumentNullException(nameof(onResult));

            SanityCheck();

            _impl.SubscribeCharacteristic(
                nativePeripheralHandle.NativePeripheral,
                serviceUuid.ToString(),
                characteristicUuid.ToString(),
                instanceIndex,
                onValueChanged,
                onResult);
        }

        /// <summary>
        /// Requests to unsubscribe from the specified service's characteristic for the given peripheral.
        /// </summary>
        /// <param name="nativePeripheralHandle">Handle to the native object for the BLE peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="instanceIndex">The instance index of the characteristic if listed more than once for the service, default is zero.</param>
        /// <param name="onResult">Invoked when the request has completed (successfully or not).</param>
        /// <remarks>The peripheral must be connected.</remarks>
        public static void UnsubscribeCharacteristic(NativePeripheralHandle nativePeripheralHandle, Guid serviceUuid, Guid characteristicUuid, uint instanceIndex, NativeRequestResultCallback onResult)
        {
            if (!nativePeripheralHandle.IsValid) throw new ArgumentException("Invalid NativePeripheralHandle", nameof(nativePeripheralHandle));
            if (serviceUuid == Guid.Empty) throw new ArgumentException("Empty service UUID", nameof(serviceUuid));
            if (characteristicUuid == Guid.Empty) throw new ArgumentException("Empty characteristic UUID", nameof(characteristicUuid));
            if (onResult == null) throw new ArgumentNullException(nameof(onResult));

            SanityCheck();

            _impl.UnsubscribeCharacteristic(
                nativePeripheralHandle.NativePeripheral,
                serviceUuid.ToString(),
                characteristicUuid.ToString(),
                instanceIndex,
                onResult);
        }

        //! @}

        // Check the static class is a valid state to access BLE peripherals
        private static void SanityCheck()
        {
            if (_impl == null) throw new InvalidOperationException("Platform not supported: " + UnityEngine.Application.platform);
            if (!_isInitialized) throw new InvalidOperationException($"{nameof(NativeInterface)} not initialized");
        }

        // Converts a list of UUIDs to a string representation as expected by the native implementation
        private static string UuidsToString(IEnumerable<Guid> uuids)
        {
            string str = null;
            if (uuids != null)
            {
                str = string.Join(",", uuids.Select(s => s.ToString()));
                if (str.Length == 0)
                {
                    str = null;
                }
            }
            return str;
        }

        // The reverse of UuidsToString
        private static Guid[] StringToUuids(string uuids)
        {
            return uuids?.Split(',').Select(BleUuid.StringToGuid).ToArray() ?? Array.Empty<Guid>();
        }
    }
}
