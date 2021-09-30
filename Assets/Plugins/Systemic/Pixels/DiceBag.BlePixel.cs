using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using Systemic.Unity.Pixels.Messages;
using UnityEngine;

using Central = Systemic.Unity.BluetoothLE.Central;
using Peripheral = Systemic.Unity.BluetoothLE.ScannedPeripheral;

namespace Systemic.Unity.Pixels
{
    partial class DiceBag
    {
        /// <summary>
        /// Implementation of Pixel communicating over Bluetooth Low Energy.
        /// </summary>
        sealed class BlePixel : Pixel
        {
            /// <summary>
            /// This data structure mirrors the data in firmware/bluetooth/bluetooth_stack.cpp
            /// </sumary>
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            struct PixelAdvertisingData
            {
                // Pixel type identification
                public PixelDesignAndColor designAndColor; // Physical look, also only 8 bits
                public byte faceCount; // Which kind of dice this is

                // Device ID
                public uint deviceId;

                // Current state
                public PixelRollState rollState; // Indicates whether the dice is being shaken
                public byte currentFace; // Which face is currently up
                public byte batteryLevel; // 0 -> 255
            };

            // Error message on timeout
            const string _connectTimeoutErrorMessage = "Timeout trying to connect, Pixel may be out of range or turned off";

            // The underlying BLE device
            Peripheral _peripheral;

            // Count the number of connection requests and disconnect only after the same number of disconnection requests
            int _connectionCount;

            // Connection internal events
            ConnectionResultCallback _onConnectionResult;
            ConnectionResultCallback _onDisconnectionResult;

            /// <summary>
            /// Indicates whether the Pixel is either in the process of connecting and being ready, or ready to communicate.
            /// </summary>
            public bool isConnectingOrReady => (connectionState == PixelConnectionState.Connecting)
                    || (connectionState == PixelConnectionState.Identifying)
                    || (connectionState == PixelConnectionState.Ready);

            /// <summary>
            /// Event raised when a Pixel gets disconnected for other reasons than a call to Disconnect().
            /// Most likely the BLE device was turned off or got out of range.
            /// </summary>
            public event System.Action DisconnectedUnexpectedly;

            /// <summary>
            /// Gets the unique id assigned to the peripheral (platform dependent).
            /// </summary>
            public string SystemId => _peripheral?.SystemId;

            /// <summary>
            /// Setup this instance for the given peripheral, may be called multiple times.
            /// </summary>
            /// <param name="peripheral">The peripheral to use.</param>
            public void Setup(Peripheral peripheral)
            {
                EnsureRunningOnMainThread();

                if (peripheral == null) throw new System.ArgumentNullException(nameof(peripheral));

                if (_peripheral == null)
                {
                    Debug.Assert(connectionState == PixelConnectionState.Invalid);
                    connectionState = PixelConnectionState.Available;
                }
                else if (_peripheral.SystemId != peripheral.SystemId)
                {
                    throw new System.InvalidOperationException("Trying to assign another peripheral to Pixel");
                }

                _peripheral = peripheral;
                systemId = _peripheral.SystemId;
                name = _peripheral.Name;

                if (_peripheral.ManufacturerData?.Count > 0)
                {
                    // Marshall the data into the struct we expect
                    int size = Marshal.SizeOf(typeof(PixelAdvertisingData));
                    if (_peripheral.ManufacturerData.Count == size)
                    {
                        System.IntPtr ptr = Marshal.AllocHGlobal(size);
                        Marshal.Copy(_peripheral.ManufacturerData.ToArray(), 0, ptr, size);
                        var advData = Marshal.PtrToStructure<PixelAdvertisingData>(ptr);
                        Marshal.FreeHGlobal(ptr);

                        // Update Pixel data
                        bool appearanceChanged = faceCount != advData.faceCount || designAndColor != advData.designAndColor;
                        bool rollStateChanged = rollState != advData.rollState || face != advData.currentFace;
                        faceCount = advData.faceCount;
                        designAndColor = advData.designAndColor;
                        rollState = advData.rollState;
                        face = advData.currentFace;

                        float newBatteryLevel = advData.batteryLevel / 255f;
                        bool batteryLevelChanged = batteryLevel != newBatteryLevel;
                        batteryLevel = newBatteryLevel;

                        bool rssiChanged = rssi != _peripheral.Rssi;
                        rssi = _peripheral.Rssi;

                        // Run callbacks
                        if (appearanceChanged)
                        {
                            AppearanceChanged?.Invoke(this, faceCount, designAndColor);
                        }
                        if (rollStateChanged)
                        {
                            RollStateChanged?.Invoke(this, rollState, face);
                        }
                        if (batteryLevelChanged)
                        {
                            BatteryLevelChanged?.Invoke(this, batteryLevel, isCharging);
                        }
                        if (rssiChanged)
                        {
                            RssiChanged?.Invoke(this, rssi);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Pixel {name}: incorrect advertising data length {_peripheral.ManufacturerData.Count}, expected: {size}");
                    }
                }
            }

            /// <summary>
            /// Clear the <see cref="Pixel.lastError"/>.
            /// </summary>
            public void ResetLastError()
            {
                EnsureRunningOnMainThread();

                lastError = PixelError.None;
            }

            /// <summary>
            /// Request to connect the Pixel.
            ///
            /// If called while connecting or being connected, the connection counter is increased and the same
            /// number of calls to <see cref="Disconnect(ConnectionResultCallback, bool)"/> must be made
            /// to disconnect the Pixel.
            /// </summary>
            /// <param name="timeout">Timeout in seconds.</param>
            /// <param name="onResult">Optional callback that is called once the connection has succeeded or timed-out.</param>
            public void Connect(float timeout, ConnectionResultCallback onResult = null)
            {
                EnsureRunningOnMainThread();

                void IncrementConnectCount()
                {
                    ++_connectionCount;
                    Debug.Log($"Pixel {SafeName}: Connecting, counter={_connectionCount}");
                }

                switch (connectionState)
                {
                    default:
                        string error = $"Unexpected Pixel connection state while attempting to connect: {connectionState}";
                        Debug.LogError($"Pixel {SafeName}: {error}");
                        onResult?.Invoke(this, false, error);
                        break;
                    case PixelConnectionState.Available:
                        IncrementConnectCount();
                        Debug.Assert(_connectionCount == 1);
                        _onConnectionResult += onResult;
                        DoConnect(timeout);
                        break;
                    case PixelConnectionState.Connecting:
                    case PixelConnectionState.Identifying:
                        // Already in the process of connecting, just add the callback and wait
                        IncrementConnectCount();
                        _onConnectionResult += onResult;
                        break;
                    case PixelConnectionState.Ready:
                        // Run the callback immediately
                        IncrementConnectCount();
                        onResult?.Invoke(this, true, null);
                        break;
                }
            }

            /// <summary>
            /// Request to disconnect the Pixel.
            ///
            /// An actual disconnection won't happen until this method is called as many time as
            /// <see cref="Connect(float, ConnectionResultCallback)"/> or <paramref name="forceDisconnect"/> is true.
            /// </summary>
            /// <param name="onConnectionResult">Optional callback that is called once the disconnection has succeeded or failed.</param>
            /// <param name="forceDisconnect">Disconnect regardless of the number of calls previously made to
            ///                               <see cref="Connect(float, ConnectionResultCallback)"/></param>
            /// <returns>Whether an actual disconnection will happen.</returns>
            public bool Disconnect(ConnectionResultCallback onResult = null, bool forceDisconnect = false)
            {
                EnsureRunningOnMainThread();

                bool willDisconnect = false;

                switch (connectionState)
                {
                    default:
                        // Pixel not connected
                        onResult?.Invoke(this, true, null);
                        break;
                    case PixelConnectionState.Ready:
                    case PixelConnectionState.Connecting:
                    case PixelConnectionState.Identifying:
                        Debug.Assert(_connectionCount > 0);
                        _connectionCount = forceDisconnect ? 0 : Mathf.Max(0, _connectionCount - 1);

                        Debug.Log($"Pixel {SafeName}: Disconnecting, counter={_connectionCount}, forceDisconnect={forceDisconnect}");

                        if (_connectionCount == 0)
                        {
                            // Register to be notified when disconnection is complete
                            _onDisconnectionResult += onResult;
                            willDisconnect = true;
                            DoDisconnect();
                        }
                        else
                        {
                            // Run the callback immediately
                            onResult(this, true, null);
                        }
                        break;
                }

                return willDisconnect;
            }

            // Connect with a timeout in seconds
            void DoConnect(float connectionTimeout)
            {
                Debug.Assert(connectionState == PixelConnectionState.Available);
                if (connectionState == PixelConnectionState.Available)
                {
                    connectionState = PixelConnectionState.Connecting;
                    StartCoroutine(ConnectAsync());

                    IEnumerator ConnectAsync()
                    {
                        Debug.Log($"Pixel {SafeName}: Connecting...");
                        BluetoothLE.RequestEnumerator connectRequest = null;
                        connectRequest = Central.ConnectPeripheralAsync(
                            _peripheral,
                            // Forward connection event it our behavior is still valid and the request hasn't timed-out
                            // (in which case the disconnect event is already taken care by the code following the yield below)
                            (p, connected) => { if ((this != null) && (!connectRequest.IsTimeout)) OnConnectionEvent(p, connected); },
                            connectionTimeout);

                        yield return connectRequest;
                        string lastRequestError = connectRequest.Error;

                        bool canceled = connectionState != PixelConnectionState.Connecting;
                        if (!canceled)
                        {
                            string error = null;
                            if (connectRequest.IsSuccess)
                            {
                                // Now connected to a Pixel, get characteristics and subscribe before switching to Identifying state
                                var pixelService = BleUuids.ServiceUuid;
                                var subscribeCharacteristic = BleUuids.NotifyCharacteristicUuid;
                                var writeCharacteristic = BleUuids.WriteCharacteristicUuid;

                                var characteristics = Central.GetPeripheralServiceCharacteristics(_peripheral, pixelService);
                                if ((characteristics != null) && characteristics.Contains(subscribeCharacteristic) && characteristics.Contains(writeCharacteristic))
                                {
                                    var subscribeRequest = Central.SubscribeCharacteristicAsync(
                                        _peripheral, pixelService, subscribeCharacteristic,
                                        // Forward value change event if our behavior is still valid
                                        data => { if (this != null) { OnValueChanged(data); } });

                                    yield return subscribeRequest;
                                    lastRequestError = subscribeRequest.Error;

                                    if (subscribeRequest.IsTimeout)
                                    {
                                        error = _connectTimeoutErrorMessage;
                                    }
                                    else if (!subscribeRequest.IsSuccess)
                                    {
                                        error = $"Subscribe request failed, {subscribeRequest.Error}";
                                    }
                                }
                                else if (characteristics == null)
                                {
                                    error = $"Characteristics request failed";
                                }
                                else
                                {
                                    error = "Missing required characteristics";
                                }
                            }
                            else if (connectRequest.IsTimeout)
                            {
                                error = _connectTimeoutErrorMessage;
                            }
                            else
                            {
                                error = $"Connection failed: {connectRequest.Error}";
                            }

                            // Check that we are still in the connecting state
                            canceled = connectionState != PixelConnectionState.Connecting;
                            if ((!canceled) && (error == null))
                            {
                                // Move on to identification
                                yield return DoIdentifyAsync(req =>
                                {
                                    lastRequestError = req.Error;
                                    error = req.IsTimeout ? _connectTimeoutErrorMessage : req.Error;
                                });

                                // Check connection state
                                canceled = connectionState != PixelConnectionState.Identifying;
                                //TODO we need a counter, in case another connect is already going on
                            }

                            if (!canceled)
                            {
                                if (error == null)
                                {
                                    // Pixel is finally ready, awesome!
                                    connectionState = PixelConnectionState.Ready;

                                    // Notify success
                                    NotifyConnectionResult();
                                }
                                else
                                {
                                    // Run callback
                                    NotifyConnectionResult(error);

                                    // Updating info didn't work, disconnect the Pixel
                                    DoDisconnect(PixelError.ConnectionError);
                                }
                            }
                        }

                        if (canceled)
                        {
                            // Wrong state => we got canceled, just abort without notifying
                            Debug.LogWarning($"Pixel {SafeName}: Connect sequence interrupted, last request error is: {lastRequestError}");
                        }
                    }
                }

                IEnumerator DoIdentifyAsync(System.Action<IOperationEnumerator> onResult)
                {
                    Debug.Assert(connectionState == PixelConnectionState.Connecting);

                    // We're going to identify the Pixel
                    connectionState = PixelConnectionState.Identifying;

                    // Reset error
                    SetLastError(PixelError.None);

                    // Ask the Pixel who it is!
                    var request = new SendMessageAndWaitForResponseEnumerator<WhoAreYou, IAmADie>(this) as IOperationEnumerator;
                    yield return request;

                    // Continue identification if we are still in the identify state
                    if (request.IsSuccess && (connectionState == PixelConnectionState.Identifying))
                    {
                        // Get the Pixel initial state
                        request = new SendMessageAndWaitForResponseEnumerator<RequestRollState, RollState>(this);
                        yield return request;
                    }

                    // Report result
                    onResult(request);
                }

                void OnConnectionEvent(Peripheral p, bool connected)
                {
                    Debug.Assert(_peripheral.SystemId == p.SystemId);

                    Debug.Log($"Pixel {SafeName}: {(connected ? "Connected" : "Disconnected")}");

                    if ((!connected) && (connectionState != PixelConnectionState.Disconnecting))
                    {
                        if ((connectionState == PixelConnectionState.Connecting) || (connectionState == PixelConnectionState.Identifying))
                        {
                            NotifyConnectionResult("Disconnected unexpectedly");
                        }
                        else
                        {
                            Debug.LogError($"Pixel {SafeName}: Got disconnected unexpectedly while in state {connectionState}");
                        }

                        // Reset connection count
                        _connectionCount = 0;

                        connectionState = PixelConnectionState.Available;
                        SetLastError(PixelError.Disconnected);

                        DisconnectedUnexpectedly?.Invoke();
                    }
                }

                void OnValueChanged(byte[] data)
                {
                    Debug.Assert(data != null);

                    // Process the message coming from the actual Pixel!
                    var message = Marshaling.FromByteArray(data);
                    if (message != null)
                    {
                        Debug.Log($"Pixel {SafeName}: Received message of type {message.GetType()}");
                        NotifyMessageHandler(message);
                    }
                }

                void NotifyConnectionResult(string error = null)
                {
                    if (error != null)
                    {
                        Debug.LogError($"Pixel {SafeName}: {error}");
                    }

                    var callbackCopy = _onConnectionResult;
                    _onConnectionResult = null;
                    callbackCopy?.Invoke(this, error == null, error);
                }
            }

            // Disconnect the Pixel, an error might be given as the reason for disconnecting
            void DoDisconnect(PixelError error = PixelError.None)
            {
                if (error != PixelError.None)
                {
                    // We're disconnecting because of an error
                    SetLastError(error);
                }

                Debug.Assert(isConnectingOrReady);
                if (isConnectingOrReady)
                {
                    _connectionCount = 0;
                    connectionState = PixelConnectionState.Disconnecting;
                    StartCoroutine(DisconnectAsync());

                    IEnumerator DisconnectAsync()
                    {
                        Debug.Log($"Pixel {SafeName}: Disconnecting...");
                        yield return Central.DisconnectPeripheralAsync(_peripheral);

                        Debug.Assert(_connectionCount == 0);
                        connectionState = PixelConnectionState.Available;

                        var callbackCopy = _onDisconnectionResult;
                        _onDisconnectionResult = null;
                        callbackCopy?.Invoke(this, true, null); // Always return a success
                    }
                }
            }

            // Set the last error
            void SetLastError(PixelError newError)
            {
                lastError = newError;
                if (lastError != PixelError.None)
                {
                    ErrorEncountered?.Invoke(this, newError);
                }
            }

            // Used by SendMessageAsync
            class WriteDataEnumerator : IOperationEnumerator
            {
                readonly BluetoothLE.RequestEnumerator _request;

                public bool IsDone => _request.IsDone;

                public bool IsTimeout => _request.IsTimeout;

                public bool IsSuccess => _request.IsSuccess;

                public string Error => _request.Error;

                public object Current => _request.Current;

                public WriteDataEnumerator(Peripheral peripheral, byte[] bytes, float timeout)
                {
                    var pixelService = BleUuids.ServiceUuid;
                    var writeCharacteristic = BleUuids.WriteCharacteristicUuid;
                    _request = Central.WriteCharacteristicAsync(peripheral, pixelService, writeCharacteristic, bytes, timeout);
                }

                public bool MoveNext()
                {
                    return _request.MoveNext();
                }

                public void Reset()
                {
                    _request.Reset();
                }
            }

            // Send the given message to the Pixel, with a timeout in seconds
            protected override IOperationEnumerator SendMessageAsync(byte[] bytes, float timeout = 0)
            {
                EnsureRunningOnMainThread();

                Debug.Log($"Pixel {SafeName}: Sending message {(MessageType)bytes?.FirstOrDefault()}");

                return new WriteDataEnumerator(_peripheral, bytes, timeout);

            }

            // Called when the behaviour will be destroyed by Unity
            protected override void OnDestroy()
            {
                base.OnDestroy();

                DisconnectedUnexpectedly = null;
                _onConnectionResult = null;
                _onDisconnectionResult = null;

                bool disconnect = isConnectingOrReady;
                _connectionCount = 0;

                Debug.Log($"Pixel {name}: Got destroyed (was connecting or connected: {disconnect})");

                if (disconnect)
                {
                    Debug.Assert(_peripheral != null);

                    // Start Disconnect coroutine on DiceBag since we are getting destroyed
                    var diceBag = DiceBag.Instance;
                    if (diceBag && diceBag.gameObject.activeInHierarchy)
                    {
                        diceBag.StartCoroutine(Central.DisconnectPeripheralAsync(_peripheral));
                    }
                }
            }
        }
    }
}
