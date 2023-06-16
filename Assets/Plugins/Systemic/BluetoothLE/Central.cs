// Ignore Spelling: Mtu Rssi Uuid Uuids

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//! \defgroup Unity_CSharp
//! @brief A collection of C# classes for the Unity game engine that provides a simplified access
//!        to Bluetooth Low Energy peripherals.
//! @see Systemic.Unity.BluetoothLE and Systemic.Unity.Pixels namespaces.

/// <summary>
/// Systemic Games base namespace.
/// </summary>
namespace Systemic { }

/// <summary>
/// A collection of C# classes for the Unity game engine provided by Systemic Games.
/// This open source package is available on GitHub at https://github.com/GameWithPixels/PixelsUnityPlugin.
/// </summary>
namespace Systemic.Unity { }

/// <summary>
/// A collection of C# classes for the Unity game engine that provides a simplified access to Bluetooth
/// Low Energy peripherals.
/// 
/// Unity plugins are used to access native Bluetooth APIs specific to each supported platform:
/// Windows 10 and above, iOS and Android.
///
/// The most useful class for Unity developers to access a Bluetooth peripheral is <see cref="Central"/>.
///
/// For communicating specifically with Pixel dices see <see cref="Systemic.Unity.Pixels.Pixel"/>.
///
/// For  <see cref="Central"/>.
/// </summary>
/// <remarks>
/// Some knowledge with Bluetooth Low Energy semantics is recommended for reading this documentation.
/// </remarks>
//! @ingroup Unity_CSharp
namespace Systemic.Unity.BluetoothLE
{
    /// <summary>
    /// A static class with methods for discovering, connecting to, and interacting with Bluetooth
    /// Low Energy (BLE) peripherals.
    /// 
    /// Use the <see cref="StartScanning"/> method to discover available BLE peripherals.
    /// Then connect to a scanned peripheral with a call to <see cref="ConnectPeripheralAsync"/>.
    /// Once connected, the peripheral can be queried for its name, MTU, RSSI, services and characteristics.
    /// Characteristics can be read, written and subscribed to.
    ///
    /// - Be sure to disconnect the peripheral once it is not needed anymore.
    /// - Calls from any thread other than the main thread throw an exception.
    /// - Any method ending by Async returns an enumerator which is meant to be run as a coroutine.
    /// - A <see cref="GameObject"/> named SystemicBleCentral is created upon calling <see cref="Initialize"/>
    /// and destroyed on calling <see cref="Shutdown"/>.
    ///
    /// For Android, be sure to update the application manifest with the necessary Bluetooth permissions
    /// listed in <c>Assets\Plugins\Systemic\Plugins\Android\AndroidManifest.xml</c>.
    /// If your application doesn't already have manifest, just copy the one provided in <c>Assets\Plugins\Android</c>.
    ///
    /// This class leverages <see cref="NativeInterface"/> to perform most of its operations.
    /// </remarks>
    /// <example>
    /// Here is a simplified example for scanning, connecting and sending a message to a peripheral.
    /// @code{.cs}
    /// // Scan for all peripherals (it's best to specify which services are required)
    /// Central.ScanForPeripheralsWithServices();
    ///
    /// // Wait until we get at least one peripheral
    /// yield return new WaitUntil(() => Central.ScannedPeripherals.Length > 0);
    ///
    /// // Stop scanning (saves battery life on mobile devices)
    /// Central.StopScan();
    ///
    /// // Select first peripheral
    /// var peripheral = Central.ScannedPeripherals[0];
    ///
    /// // And attempt to connect to it
    /// var request = Central.ConnectPeripheralAsync(peripheral, (_, connected)
    ///     => Debug.Log(connected ? "Connected!" : "Not connected!"));
    /// yield return request;
    ///
    /// // Check result
    /// if (request.IsSuccess)
    /// {
    ///     // And send some data
    ///     yield return Central.WriteCharacteristicAsync(
    ///         peripheral, aServiceUuid, aCharacteristicUuid, anArrayOfBytes);
    /// }
    /// @endcode
    /// </example>
    public static class Central
    {
        // Internal peripheral states
        enum PeripheralState
        {
            Disconnected, Connecting, Ready, Disconnecting,
        }

        // Keeps a bunch of information about a known peripheral
        class PeripheralInfo
        {
            public string Name => ScannedPeripheral?.Name;
            public ScannedPeripheral ScannedPeripheral;
            public PeripheralState State;
            public NativePeripheralHandle NativeHandle;
            public Guid[] RequiredServices;
            public Action<ScannedPeripheral, bool> ConnStatusChangedCallback;
        }

        // All scanned peripherals, key is the peripheral SystemId, items are never removed except on shutdown
        static readonly Dictionary<string, PeripheralInfo> _peripheralsInfo = new Dictionary<string, PeripheralInfo>();

        /// <summary>
        /// The default timeout value (in seconds) for requests send to a BLE peripheral.
        /// </summary>
        public const int DefaultRequestTimeout = 10;


        /// <summary>
        /// Indicates whether <see cref="Central"/> is ready for scanning and connecting to peripherals.
        /// Reasons for not being ready are:
        /// - <see cref="Initialize"/> hasn't been called, or <see cref="Shutdown"/> was called afterwards.
        /// - Initialization is still on-going.
        /// - The host device either doesn't have a Bluetooth radio, doesn't have permission to use the radio
        ///   or it's radio is turned off.
        /// </summary>
        public static BluetoothStatus Status { get; private set; } = BluetoothStatus.Unknown;

        /// <summary>
        /// Indicates whether a Bluetooth scan is on-going.
        /// </summary>
        public static bool IsScanning { get; private set; } = false;

        /// <summary>
        /// Gets the list of all scanned peripherals since <see cref="Initialize"/> was called.
        /// The list is cleared on <see cref="Shutdown"/>.
        /// </summary>
        public static ScannedPeripheral[] ScannedPeripherals
        {
            get
            {
                EnsureRunningOnMainThread();

                return _peripheralsInfo.Values
                    .Select(pInf => pInf.ScannedPeripheral)
                    .ToArray();
            }
        }

        /// <summary>
        /// Gets the list of peripherals to which <see cref="Central"/> is connected
        /// and that ready to communicate with (meaning services and characteristics have been
        /// discovered and MTU has beens set).
        /// </summary>
        public static ScannedPeripheral[] ReadyPeripherals
        {
            get
            {
                EnsureRunningOnMainThread();

                return _peripheralsInfo.Values
                    .Where(pInf => pInf.State == PeripheralState.Ready)
                    .Select(pInf => pInf.ScannedPeripheral)
                    .ToArray();
            }
        }

        /// <summary>
        /// Occurs when <see cref="Status"/> changes.
        /// </summary>
        public static event Action<BluetoothStatus> StatusChanged;

        /// <summary>
        /// Occurs when <see cref="IsScanning"/> changes.
        /// </summary>
        public static event Action<bool> IsScanningChanged;

        /// <summary>
        /// Occurs when a peripheral is discovered or re-discovered.
        /// Discovery happens each time <see cref="Central"/> receives a discovery packet
        /// from a peripheral. This may happen at a frequency between several times per second
        /// and every few seconds.
        /// </summary>
        public static event Action<ScannedPeripheral> PeripheralDiscovered;

        //! \name Static class life cycle
        //! @{

        /// <summary>
        /// Initializes the static class.
        /// The <see cref="Status"/> property is updated to reflect the availability of Bluetooth.
        /// </summary>
        /// <returns>
        /// Indicates whether the call has succeeded.
        /// - If <c>true</c> is returned, the static class might not be ready yet.
        /// - If <c>false</c> is returned, there is probably something wrong with the platform specific native plugin.
        /// </returns>
        public static bool Initialize()
        {
            EnsureRunningOnMainThread();

            Debug.Log("[BLE] Initializing");

            InternalBehaviour.Create();

            // Initialize NativeInterface and subscribe to get notified when the Bluetooth radio status changes
            bool success = NativeInterface.Initialize(status =>
            {
                EnqueueAction(() =>
                {
                    UpdateStatus(status);
                    if (status != BluetoothStatus.Ready)
                    {
                        UpdateIsScanning(false);
                    }
                });
            });

            if (!success)
            {
                Debug.LogError("[BLE] Failed to initialize");
            }

            return success;
        }

        /// <summary>
        /// Shutdowns the static class.
        ///
        /// Scanning is stopped and all peripherals are disconnected and removed.
        /// </summary>
        public static void Shutdown()
        {
            EnsureRunningOnMainThread();

            Debug.Log("[BLE] Shutting down");

            // Reset states
            _peripheralsInfo.Clear();
            UpdateStatus(BluetoothStatus.Unknown);
            UpdateIsScanning(false);

            // Shutdown native interface and destroy companion mono behaviour
            NativeInterface.Shutdown();
            InternalBehaviour.ScheduleDestroy();
        }

        //! @}
        //! \name Peripherals scanning
        //! @{

        /// <summary>
        /// Starts scanning for BLE peripherals advertising the given list of services.
        ///
        /// If a scan is already running, it is updated to use the new list of required services.
        ///
        /// Specifying one more service required for the peripherals saves battery on mobile devices.
        /// </summary>
        /// <param name="serviceUuids">List of services that the peripheral should advertise, may be null or empty.</param>
        /// <returns>Indicates whether the call has succeeded. It fails if <see cref="Status"/> is not <c>Ready</c>.</returns>
        public static bool StartScanning(IEnumerable<Guid> serviceUuids = null)
        {
            EnsureRunningOnMainThread();

            // We must be ready
            if (Status != BluetoothStatus.Ready)
            {
                Debug.LogError("[BLE] Central not ready for scanning");
                return false;
            }

            // Make sure we don't have a null array
            var requiredServices = serviceUuids?.ToArray() ?? Array.Empty<Guid>();

            // Start scanning
            bool isScanning = NativeInterface.StartScan(serviceUuids, scannedPeripheral =>
            {
                EnqueueAction(() =>
                {
                    Debug.Log($"[BLE:{scannedPeripheral.Name}] Peripheral discovered with name={scannedPeripheral.Name}, RSSI={scannedPeripheral.Rssi}");

                    // Keep track of discovered peripherals
                    if (!_peripheralsInfo.TryGetValue(scannedPeripheral.SystemId, out PeripheralInfo pInf))
                    {
                        _peripheralsInfo[scannedPeripheral.SystemId] = pInf = new PeripheralInfo();
                    }
                    pInf.ScannedPeripheral = scannedPeripheral;
                    pInf.RequiredServices = requiredServices;
                    //TODO pInf.State = PeripheralState.Disconnected;

                    // Notify
                    PeripheralDiscovered?.Invoke(scannedPeripheral);
                });
            });

            if (isScanning)
            {
                Debug.Log($"[BLE] Started scan for BLE peripherals with services {serviceUuids?.Select(g => g.ToString()).Aggregate((a, b) => a + ", " + b)}");
            }
            else
            {
                Debug.LogError("[BLE] Failed to start scanning for peripherals");
            }

            UpdateIsScanning(isScanning);

            return isScanning;
        }

        /// <summary>
        /// Stops an on-going BLE scan.
        /// </summary>
        public static void StopScanning()
        {
            EnsureRunningOnMainThread();

            Debug.Log($"[BLE] Stopping scan");

            NativeInterface.StopScan();
            UpdateIsScanning(false);
        }

        //! @}
        //! \name Peripheral connection and disconnection
        //! @{

        /// <summary>
        /// Asynchronously connects to a discovered peripheral.
        /// 
        /// The enumerator stops on either of these conditions:
        /// - The connection succeeded.
        /// - <see cref="DisconnectPeripheralAsync"/> was called for the given peripheral.
        /// - The connection didn't succeeded after the given timeout value.
        /// - An error occurred while trying to connect.
        ///
        /// Once connected to the peripheral, <see cref="Central"/> sends a request to change the peripheral's
        /// Maximum Transmission Unit (MTU) to the highest supported value.
        ///
        /// Once the MTU is changed, <see cref="Central"/> notifies the caller that the peripheral is ready to be used
        /// by invoking the <paramref name="onConnectionEvent"/> handler with the second argument set to <c>true</c>.
        ///
        /// Upon a disconnection (whichever the cause), <see cref="Central"/> notifies the caller by invoking
        /// the <paramref name="onConnectionEvent"/> handler with the second argument set to <c>false</c>.
        ///
        /// Check <see cref="RequestEnumerator"/> members for more details.
        /// </summary>
        /// <param name="peripheral">Scanned peripheral to connect to.</param>
        /// <param name="onConnectionEvent">Called each time the connection state changes, the peripheral is passed as
        ///                                 the first argument and the connection state as the second argument
        ///                                 (<c>true</c> means connected).</param>
        /// <param name="timeoutSec">The timeout value, in seconds.
        /// The default is zero in which case the request never times out.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="RequestEnumerator"/> properties to get the request status.
        /// </returns>
        /// <remarks>At the moment the Windows implementation timeouts at a maximum of 7 seconds.</remarks>
        public static RequestEnumerator ConnectPeripheralAsync(ScannedPeripheral peripheral, Action<ScannedPeripheral, bool> onConnectionEvent, float timeoutSec = 0)
        {
            if (timeoutSec < 0) throw new ArgumentException(nameof(timeoutSec) + " must be greater or equal to zero", nameof(timeoutSec));

            EnsureRunningOnMainThread();

            // Get peripheral state
            PeripheralInfo pInf = GetPeripheralInfo(peripheral);

            //TODO Native Android & Windows may timeout before the given timeout value
            //TODO Handle case when another connection request for the same device is already under way
            //Debug.Assert(pInf?.ConnStatusChangedCallback == null);

            // We need a valid peripheral
            if (!pInf.NativeHandle.IsValid)
            {
                // Create new native peripheral handle
                pInf.State = PeripheralState.Disconnected;
                pInf.NativeHandle = NativeInterface.CreatePeripheral(peripheral,
                    (connectionEvent, reason) => EnqueuePeripheralAction(pInf, () =>
                    {
                        Debug.Log($"[BLE {pInf.Name}] "
                            + $"Connection event `{connectionEvent}`"
                            + (reason == ConnectionEventReason.Success ? "" : $" with reason `{reason}`")
                            + $", state was `{pInf.State}`");
                        OnPeripheralConnectionEvent(pInf, connectionEvent, reason);
                    }));

                // Check that the above call worked
                if (pInf.NativeHandle.IsValid)
                {
                    Debug.Log($"[BLE {pInf.Name}] Native peripheral created");
                }
                else
                {
                    Debug.LogError($"[BLE {pInf.Name}] Failed to create native peripheral");
                }
            }

            // Attempt connecting until we got a success, a timeout or an unexpected error
            return new Internal.ConnectRequestEnumerator(pInf.NativeHandle, timeoutSec,
                (_, onResult) =>
                {
                    Debug.Assert(pInf.NativeHandle.IsValid); // Already checked by RequestEnumerator

                    Debug.Log($"[BLE {pInf.Name}] Connecting with {(timeoutSec == 0 ? "no timeout" : $"timeout of {timeoutSec}s")}, last known state is {pInf.State}");
                    pInf.ConnStatusChangedCallback = onConnectionEvent;
                    Connect(pInf, onResult);

                    static void Connect(PeripheralInfo pInf, NativeRequestResultCallback onResult)
                    {
                        pInf.State = PeripheralState.Connecting;

                        NativeInterface.ConnectPeripheral(
                            pInf.NativeHandle,
                            pInf.RequiredServices,
                            false, //TODO autoConnect
                            status => EnqueuePeripheralAction(pInf, () =>
                            {
                                Debug.Log($"[BLE {pInf.Name}] Connect result is `{status}`");

                                // We're either connected on in an invalid state, in both cases we stop trying to connect again
                                onResult(status);
                            }));
                    }
                },
                () => Debug.LogWarning($"[BLE {pInf.Name}] Connection timeout, canceling..."));

            // Connection event callback
            static void OnPeripheralConnectionEvent(PeripheralInfo pInf, ConnectionEvent connectionEvent, ConnectionEventReason reason)
            {
                bool ready = connectionEvent == ConnectionEvent.Ready;
                bool disconnected = connectionEvent == ConnectionEvent.Disconnected
                    || connectionEvent == ConnectionEvent.FailedToConnect;

                if (connectionEvent == ConnectionEvent.Disconnecting)
                {
                    pInf.State = PeripheralState.Disconnecting;
                }

                if (!(disconnected || ready))
                {
                    // Nothing to do
                    return;
                }

                if (ready)
                {
                    if (pInf.NativeHandle.IsValid && (NativeInterface.GetPeripheralMtu(pInf.NativeHandle) == NativeInterface.MinMtu))
                    {
                        // Change MTU to maximum (note: MTU can only be set once)
                        NativeInterface.RequestPeripheralMtu(pInf.NativeHandle, NativeInterface.MaxMtu,
                            (mtu, status) => EnqueuePeripheralAction(pInf, () =>
                            {
                                Debug.Log($"[BLE {pInf.Name}] MTU {(status == RequestStatus.Success ? "changed to" : "kept at")} {mtu} bytes");
                                if ((status != RequestStatus.Success) && (status != RequestStatus.NotSupported))
                                {
                                    Debug.LogError($"[BLE {pInf.Name}] Failed to change MTU, result is `{status}`");
                                }

                                SetReady(pInf);
                            }));
                    }
                    else
                    {
                        SetReady(pInf);
                    }
                }
                else if (pInf.State != PeripheralState.Disconnected)
                {
                    // We got disconnected
                    Debug.Log($"[BLE {pInf.Name}] Peripheral is disconnected, notifying");

                    // Update state
                    pInf.State = PeripheralState.Disconnected;

                    // Notify
                    pInf.ConnStatusChangedCallback?.Invoke(pInf.ScannedPeripheral, false);
                }
            }

            static void SetReady(PeripheralInfo pInf)
            {
                if (pInf.NativeHandle.IsValid && (pInf.State == PeripheralState.Connecting))
                {
                    // We're done and ready
                    Debug.Log($"[BLE {pInf.Name}] Peripheral is ready, notifying");

                    // Update state
                    pInf.State = PeripheralState.Ready;

                    // Notify
                    pInf.ConnStatusChangedCallback?.Invoke(pInf.ScannedPeripheral, true);
                }
                else if (pInf.NativeHandle.IsValid)
                {
                    Debug.LogWarning($"[BLE {pInf.Name}] Connection canceled before being ready");
                }
                else
                {
                    Debug.LogWarning($"[BLE {pInf.Name}] Peripheral became invalid before being ready");
                }
            }
        }

        /// <summary>
        /// Disconnects a peripheral.
        /// </summary>
        /// <param name="peripheral">Scanned peripheral to disconnect from.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="RequestEnumerator"/> properties to get the request status.
        /// </returns>
        public static RequestEnumerator DisconnectPeripheralAsync(ScannedPeripheral peripheral)
        {
            EnsureRunningOnMainThread();

            var pInf = GetPeripheralInfo(peripheral);
            Debug.Log($"[BLE {pInf.Name}] Disconnecting{(pInf.NativeHandle.IsValid ? "" : " invalid peripheral")}, last known state is {pInf.State}");

            return new Internal.DisconnectRequestEnumerator(pInf.NativeHandle, (nativeHandle) =>
            {
                //TODO releasing the peripheral now will break the next attempt to connect
                //     if done before this callback runs Also an unexpected disconnect won't
                //     release the peripheral.
                // Release peripheral even if the disconnect might have failed
                //NativeInterface.ReleasePeripheral(nativeHandle);
                //pInf.NativeHandle = new NativePeripheralHandle();
            });
        }

        //! @}
        //! \name Peripheral operations
        //! Only valid once a peripheral is connected.
        //! @{

        /// <summary>
        /// Gets the name of the given peripheral.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <returns>The peripheral name.</returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static string GetPeripheralName(ScannedPeripheral peripheral)
        {
            EnsureRunningOnMainThread();

            //TODO check peripheral?

            var nativeHandle = GetPeripheralInfo(peripheral).NativeHandle;
            return nativeHandle.IsValid ? NativeInterface.GetPeripheralName(nativeHandle) : null;
        }

        /// <summary>
        /// Gets the Maximum Transmission Unit (MTU) for the given peripheral.
        /// 
        /// The MTU is the maximum length of a packet that can be send to the BLE peripheral.
        /// However the BLE protocol uses 3 bytes, so the maximum data size that can be given
        /// to <see cref="WriteCharacteristicAsync"/> is 3 bytes less than the MTU.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <returns>The peripheral MTU.</returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static int GetPeripheralMtu(ScannedPeripheral peripheral)
        {
            //TODO check if MTU is 23 or 20 (and update comment if it's the later)
            EnsureRunningOnMainThread();

            var nativeHandle = GetPeripheralInfo(peripheral).NativeHandle;
            return nativeHandle.IsValid ? NativeInterface.GetPeripheralMtu(nativeHandle) : 0;
        }

        /// <summary>
        /// Asynchronously reads the Received Signal Strength Indicator (RSSI) for the given peripheral.
        /// 
        /// It gives an indication of the connection quality.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="timeoutSec">The maximum allowed time for the request, in seconds.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="ValueRequestEnumerator<>"/> properties to get the RSSI value and the request status.
        /// </returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static ValueRequestEnumerator<int> ReadPeripheralRssi(ScannedPeripheral peripheral, float timeoutSec = DefaultRequestTimeout)
        {
            EnsureRunningOnMainThread();

            var nativeHandle = GetPeripheralInfo(peripheral).NativeHandle;
            return new ValueRequestEnumerator<int>(
                RequestOperation.ReadPeripheralRssi, nativeHandle, timeoutSec,
                (p, onResult) => NativeInterface.ReadPeripheralRssi(p, onResult));
        }

        //! @}
        //! \name Services operations
        //! Valid only for connected peripherals.
        //! @{

        /// <summary>
        /// Gets the list of discovered services for the given peripheral.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <returns>The list of discovered services.</returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static Guid[] GetDiscoveredServices(ScannedPeripheral peripheral)
        {
            EnsureRunningOnMainThread();

            var nativeHandle = GetPeripheralInfo(peripheral).NativeHandle;
            return nativeHandle.IsValid ? NativeInterface.GetDiscoveredServices(nativeHandle) : null;
        }

        /// <summary>
        /// Gets the list of discovered characteristics of the given peripheral's service.
        /// 
        /// The same characteristic may be listed several times according to the peripheral's configuration.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="serviceUuid">The service UUID for which to retrieve the characteristics.</param>
        /// <returns>The list of discovered characteristics of a service.</returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static Guid[] GetServiceCharacteristics(ScannedPeripheral peripheral, Guid serviceUuid)
        {
            EnsureRunningOnMainThread();

            var nativeHandle = GetPeripheralInfo(peripheral).NativeHandle;
            return nativeHandle.IsValid ? NativeInterface.GetServiceCharacteristics(nativeHandle, serviceUuid) : null;
        }

        //! @}
        //! \name Characteristics operations
        //! Valid only for connected peripherals.
        //! @{

        /// <summary>
        /// Gets the BLE properties of the specified service's characteristic for the given peripheral.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="instanceIndex">The instance index of the characteristic if listed more than once for the service, otherwise zero.</param>
        /// <returns>The BLE properties of a service's characteristic.</returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static CharacteristicProperties GetCharacteristicProperties(ScannedPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, uint instanceIndex = 0)
        {
            EnsureRunningOnMainThread();

            var nativeHandle = GetPeripheralInfo(peripheral).NativeHandle;
            return nativeHandle.IsValid ? NativeInterface.GetCharacteristicProperties(nativeHandle, serviceUuid, characteristicUuid, instanceIndex) : CharacteristicProperties.None;
        }

        /// <summary>
        /// Asynchronously reads the value of the specified service's characteristic for the given peripheral.
        /// 
        /// The call fails if the characteristic is not readable.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="timeoutSec">The maximum allowed time for the request, in seconds.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="RequestEnumerator"/> properties to get the request status.
        /// </returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static ValueRequestEnumerator<byte[]> ReadCharacteristicAsync(ScannedPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, float timeoutSec = DefaultRequestTimeout)
        {
            return ReadCharacteristicAsync(peripheral, serviceUuid, characteristicUuid, 0, timeoutSec);
        }

        /// <summary>
        /// Asynchronously reads the value of the specified service's characteristic for the given peripheral.
        /// 
        /// The call fails if the characteristic is not readable.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="instanceIndex">The instance index of the characteristic if listed more than once for the service.</param>
        /// <param name="timeoutSec">The maximum allowed time for the request, in seconds.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="ValueRequestEnumerator<>"/> properties to get the characteristic's value and the request status.
        /// </returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static ValueRequestEnumerator<byte[]> ReadCharacteristicAsync(ScannedPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, uint instanceIndex, float timeoutSec = DefaultRequestTimeout)
        {
            EnsureRunningOnMainThread();

            var pInf = GetPeripheralInfo(peripheral);
            return new ValueRequestEnumerator<byte[]>(
                RequestOperation.ReadCharacteristic, pInf.NativeHandle, timeoutSec,
                (p, onResult) => NativeInterface.ReadCharacteristic(
                   p, serviceUuid, characteristicUuid, instanceIndex,
                   onValueReadResult: onResult));
        }

        /// <summary>
        /// Asynchronously writes to the specified service's characteristic for the given peripheral
        /// and waits for the peripheral to respond.
        /// 
        /// The call fails if the characteristic is not writable.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="data">The data to write to the characteristic (may be empty).</param>
        /// <param name="timeoutSec">The maximum allowed time for the request, in seconds.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="RequestEnumerator"/> properties to get the request status.
        /// </returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static RequestEnumerator WriteCharacteristicAsync(ScannedPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, byte[] data, float timeoutSec = DefaultRequestTimeout)
        {
            return WriteCharacteristicAsync(peripheral, serviceUuid, characteristicUuid, 0, data, false, timeoutSec);
        }

        /// <summary>
        /// Asynchronously writes to the specified service's characteristic for the given peripheral.
        /// 
        /// The call fails if the characteristic is not writable.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="data">The data to write to the characteristic (may be empty).</param>
        /// <param name="withoutResponse">Whether to wait for the peripheral to respond that it has received the data.
        ///                               It's usually best to request a response.</param>
        /// <param name="timeoutSec">The maximum allowed time for the request, in seconds.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="RequestEnumerator"/> properties to get the request status.
        /// </returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static RequestEnumerator WriteCharacteristicAsync(ScannedPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, byte[] data, bool withoutResponse, float timeoutSec = DefaultRequestTimeout)
        {
            return WriteCharacteristicAsync(peripheral, serviceUuid, characteristicUuid, 0, data, withoutResponse, timeoutSec);
        }

        /// <summary>
        /// Asynchronously writes to the specified service's characteristic for the given peripheral.
        /// 
        /// The call fails if the characteristic is not writable.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="instanceIndex">The instance index of the characteristic if listed more than once for the service.</param>
        /// <param name="data">The data to write to the characteristic (may be empty).</param>
        /// <param name="withoutResponse">Whether to wait for the peripheral to respond that it has received the data.
        ///                               It's usually best to request a response.</param>
        /// <param name="timeoutSec">The maximum allowed time for the request, in seconds.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="RequestEnumerator"/> properties to get the request status.
        /// </returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static RequestEnumerator WriteCharacteristicAsync(ScannedPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, uint instanceIndex, byte[] data, bool withoutResponse = false, float timeoutSec = DefaultRequestTimeout)
        {
            EnsureRunningOnMainThread();

            var nativeHandle = GetPeripheralInfo(peripheral).NativeHandle;
            return new RequestEnumerator(
                RequestOperation.WriteCharacteristic, nativeHandle, timeoutSec,
                (p, onResult) => NativeInterface.WriteCharacteristic(
                    p, serviceUuid, characteristicUuid, instanceIndex, data, withoutResponse, onResult));
        }

        /// <summary>
        /// Asynchronously subscribes for value changes of the specified service's characteristic for the given peripheral.
        ///
        /// Replaces a previously registered value change handler for the same characteristic.
        /// The call fails if the characteristic doesn't support notifications.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="onValueChanged">Invoked when the value of the characteristic changes.</param>
        /// <param name="timeoutSec">The maximum allowed time for the request, in seconds.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="RequestEnumerator"/> properties to get the request status.
        /// </returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static RequestEnumerator SubscribeCharacteristicAsync(ScannedPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, Action<byte[]> onValueChanged, float timeoutSec = DefaultRequestTimeout)
        {
            return SubscribeCharacteristicAsync(peripheral, serviceUuid, characteristicUuid, 0, onValueChanged, timeoutSec);
        }

        /// <summary>
        /// Asynchronously subscribe for value changes of the specified service's characteristic for the given peripheral.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="instanceIndex">The instance index of the characteristic if listed more than once for the service.</param>
        /// <param name="onValueChanged">Invoked when the value of the characteristic changes.</param>
        /// <param name="timeoutSec">The maximum allowed time for the request, in seconds.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="RequestEnumerator"/> properties to get the request status.
        /// </returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static RequestEnumerator SubscribeCharacteristicAsync(ScannedPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, uint instanceIndex, Action<byte[]> onValueChanged, float timeoutSec = DefaultRequestTimeout)
        {
            EnsureRunningOnMainThread();

            //TODO it doesn't seem correct to call onResult!
            NativeValueRequestResultCallback<byte[]> GetNativeValueChangedHandler(PeripheralInfo pInf, Action<byte[]> onValueChanged, NativeRequestResultCallback onResult)
            {
                return (data, status) =>
                {
                    try
                    {
                        if (status == RequestStatus.Success)
                        {
                            Debug.Assert(data != null);
                            EnqueuePeripheralAction(pInf, () => onValueChanged(data));
                        }
                        else
                        {
                            Debug.Assert(data == null);
                            onResult(status);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                };
            }

            var pInf = GetPeripheralInfo(peripheral);
            return new RequestEnumerator(
                RequestOperation.SubscribeCharacteristic, pInf.NativeHandle, timeoutSec,
                (p, onResult) => NativeInterface.SubscribeCharacteristic(
                    p, serviceUuid, characteristicUuid, instanceIndex,
                    onValueChanged: GetNativeValueChangedHandler(pInf, onValueChanged, onResult),
                    onResult: onResult));
        }

        /// <summary>
        /// Asynchronously unsubscribe from the specified service's characteristic for the given peripheral.
        /// </summary>
        /// <param name="peripheral">The connected peripheral.</param>
        /// <param name="serviceUuid">The service UUID.</param>
        /// <param name="characteristicUuid">The characteristic UUID.</param>
        /// <param name="instanceIndex">The instance index of the characteristic if listed more than once for the service.</param>
        /// <param name="timeoutSec">The maximum allowed time for the request, in seconds.</param>
        /// <returns>
        /// An enumerator meant to be run as a coroutine.
        /// See <see cref="RequestEnumerator"/> properties to get the request status.
        /// </returns>
        /// <remarks>The peripheral must be connected.</remarks>
        public static RequestEnumerator UnsubscribeCharacteristicAsync(ScannedPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, uint instanceIndex = 0, float timeoutSec = DefaultRequestTimeout)
        {
            EnsureRunningOnMainThread();

            var nativeHandle = GetPeripheralInfo(peripheral).NativeHandle;
            return new RequestEnumerator(
                RequestOperation.UnsubscribeCharacteristic, nativeHandle, timeoutSec,
                (p, onResult) => NativeInterface.UnsubscribeCharacteristic(
                    p, serviceUuid, characteristicUuid, instanceIndex, onResult));
        }

        //! @}

        private static void UpdateStatus(BluetoothStatus status)
        {
            if (Status != status)
            {
                Debug.Log($"[BLE] Bluetooth status changed from {Status} to {status}");
                Status = status;
                StatusChanged?.Invoke(status);
            }
        }

        private static void UpdateIsScanning(bool isScanning)
        {
            if (IsScanning != isScanning)
            {
                Debug.Log($"[BLE] IsScanning changed from {IsScanning} to {isScanning}");
                IsScanning = isScanning;
                IsScanningChanged?.Invoke(isScanning);
            }
        }

        // Throws an exception if we are not running on the main thread
        private static void EnsureRunningOnMainThread()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != 1)
            {
                throw new InvalidOperationException($"Methods of type {nameof(Central)} can only be called from the main thread");
            }
        }

        // Retrieves the stored peripheral state for the given scanned peripheral
        private static PeripheralInfo GetPeripheralInfo(ScannedPeripheral peripheral)
        {
            if (peripheral == null) throw new ArgumentNullException(nameof(peripheral));

            _peripheralsInfo.TryGetValue(peripheral.SystemId, out PeripheralInfo pInf);
            return pInf ?? throw new ArgumentException(nameof(peripheral), $"No peripheral found with SystemId={peripheral.SystemId}");
        }

        #region PersistentMonoBehaviourSingleton

        /// <summary>
        /// Internal <see cref="MonoBehaviour"/> that runs queued <see cref="Action"/> on each
        /// Unity's call to <see cref="Update"/>.
        /// </summary>
        sealed class InternalBehaviour :
            Internal.PersistentMonoBehaviourSingleton<InternalBehaviour>,
            Internal.IPersistentMonoBehaviourSingleton
        {
            // Our action queue
            readonly ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();

            // Instance name
            string Internal.IPersistentMonoBehaviourSingleton.GameObjectName => "SystemicBleCentral";

            /// <summary>
            /// Queues an action to be invoked on the next frame update.
            /// </summary>
            /// <param name="action">The action to be invoked on the next update.</param>
            public void EnqueueAction(Action action)
            {
                _actionQueue.Enqueue(action);
            }

            // Update is called once per frame
            protected override void Update()
            {
                while (_actionQueue.TryDequeue(out Action act))
                {
                    try
                    {
                        act?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                base.Update();
            }

            // Called when the instance will be destroyed
            void OnDestroy()
            {
                if (!AutoDestroy)
                {
                    Central.Shutdown();
                }
            }
        }

        // Queues an action to be invoked on the next frame update
        static void EnqueueAction(Action action)
        {
            var instance = InternalBehaviour.Instance;
            if (instance)
            {
                instance.EnqueueAction(action);
            }
        }

        // Queues an action to be invoked on the next frame update but only if the peripheral is still in our list
        static void EnqueuePeripheralAction(PeripheralInfo pInf, Action action)
        {
            var instance = InternalBehaviour.Instance;
            if (instance)
            {
                instance.EnqueueAction(() =>
                {
                    Debug.Assert(pInf.ScannedPeripheral != null);
                    if (_peripheralsInfo.ContainsKey(pInf.ScannedPeripheral?.SystemId))
                    {
                        action();
                    }
                });
            }
        }

        #endregion
    }
}
