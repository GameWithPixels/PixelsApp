using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using BluetoothStatus = Systemic.Unity.BluetoothLE.BluetoothStatus;
using Central = Systemic.Unity.BluetoothLE.Central;
using Peripheral = Systemic.Unity.BluetoothLE.ScannedPeripheral;

namespace Systemic.Unity.Pixels
{
    /// <summary>
    /// Static class that scan for and connect to <see cref="Pixel"/> dice.
    /// Scan and connection requests are counted, meaning that the same number of respectively scan cancellation
    /// and disconnection requests must be made for them to effectively happen.
    ///
    /// This allows for different parts of the user code to work with this singleton without impacting each others.
    /// 
    /// <see cref="Central"/> must be initialized first before calling methods of this class.
    /// </summary>
    public static partial class DiceBag
    {
        /// <summary>
        /// Return status when starting a scan with <see cref="ScanForPixels"/>.
        /// In case of error, check <see cref="BluetoothStatus"/> for more information
        /// about the current Bluetooth state. Note that it is possible that the state
        /// changed since the attempt to start scanning.
        /// </summary>
        public enum ScanStatus
        {
            Started,
            NotReady,
            Error,
        }

        // Count the number of scan requests and cancel scanning only after the same number of stop scan requests
        static int _scanRequestCount;

        // List of known pixels
        static readonly HashSet<BlePixel> _pixels = new HashSet<BlePixel>();

        // Pixels to be destroyed in the next frame update
        static readonly HashSet<BlePixel> _pixelsToDestroy = new HashSet<BlePixel>();

        // Map of registered Pixels, the key is a Pixel system id and the value its name (may be empty)
        static readonly Dictionary<string, string> _registeredPixels = new Dictionary<string, string>();

        // Callbacks for notifying user code
        static NotifyUserCallback _notifyUser;
        static PlayAudioClipCallback _playAudioClip;

        /// <summary>
        /// Bluetooth status event, <see cref="Central.StatusChanged"/> event.
        /// </summary>
        public static event System.Action<BluetoothStatus> BluetoothStatusChanged;

        /// <summary>
        /// Bluetooth status, <see cref="Central.Status"/> property.
        /// </summary>
        public static BluetoothStatus BluetoothStatus => Central.Status;

        /// <summary>
        /// Indicates whether we are ready for scanning and connecting to peripherals.
        /// </summary>
        public static bool IsReady => Central.Status == BluetoothStatus.Ready;

        /// <summary>
        /// Indicates whether we are scanning for Pixel dice.
        /// </summary>
        public static bool IsScanning => _scanRequestCount > 0;

        /// <summary>
        /// Gets the list of all Pixels we know about.
        /// </summary>
        public static Pixel[] AllPixels => _pixels.ToArray();

        /// <summary>
        /// Gets the list of available (scanned but not connected) Pixel dice.
        /// </summary>
        public static Pixel[] AvailablePixels => _pixels.Where(p => p.isAvailable).ToArray();

        /// <summary>
        /// Gets the list of Pixel dice that are connected and ready to communicate.
        /// </summary>
        public static Pixel[] ConnectedPixels => _pixels.Where(p => p.isReady).ToArray();

        /// <summary>
        /// An event raised when a Pixel is discovered, may be raised multiple times for
        /// the same Pixel as it receives new advertisement packets from it.
        /// </summary>
        public static event System.Action<Pixel> PixelDiscovered;

        /// <summary>
        /// Initialize the instance and the Bluetooth stack.
        /// Be sure to call this method before any other one of this singleton.
        /// </summary>
        public static void Initialize()
        {
            InternalBehaviour.Create();
        }

        #region Scan for Pixels

        /// <summary>
        /// Starts scanning for Pixel dice.
        /// </summary>
        public static ScanStatus ScanForPixels()
        {
            InternalBehaviour.CheckValid();

            if (IsReady)
            {
                ++_scanRequestCount;
                Central.PeripheralDiscovered -= OnPeripheralDiscovered; // Prevents from subscribing twice
                Central.PeripheralDiscovered += OnPeripheralDiscovered;
                if (!Central.StartScanning(new[] { PixelBleUuids.Service }))
                {
                    StopScanning();
                    return ScanStatus.Error;
                }
                else
                {
                    return ScanStatus.Started;
                }
            }
            else
            {
                Debug.LogWarning($"Central not ready for scanning, status is {Central.Status}");
                return ScanStatus.NotReady;
            }
        }

        /// <summary>
        /// Stops scanning for Pixels when called as many times as <see cref="ScanForPixels"/>.
        /// </summary>
        /// <param name="forceStop">If true stops scanning regardless of the number of scan calls that were made.</param>
        public static void StopScanning(bool forceStop = false)
        {
            if (!InternalBehaviour.CheckValid(noThrow: true))
            {
                return;
            }

            if (_scanRequestCount > 0)
            {
                _scanRequestCount = forceStop ? 0 : Mathf.Max(0, _scanRequestCount - 1);

                if (_scanRequestCount == 0)
                {
                    Central.PeripheralDiscovered -= OnPeripheralDiscovered;
                    Central.StopScanning();
                }
            }
        }

        /// <summary>
        /// Removes all available (scanned but not connected) Pixel dice.
        /// </summary>
        public static void ClearAvailablePixels()
        {
            var pixelsCopy = new List<BlePixel>(_pixels);
            foreach (var pixel in pixelsCopy)
            {
                if (pixel.connectionState == PixelConnectionState.Available)
                {
                    DestroyPixel(pixel);
                }
            }
        }

        // Called by Central when a new Pixel is discovered
        static void OnPeripheralDiscovered(Peripheral peripheral)
        {
            InternalBehaviour.CheckValid();

            // Check if have already a Pixel object for this peripheral
            var pixel = _pixels.FirstOrDefault(d => peripheral.SystemId == d.SystemId);
            if (pixel == null)
            {
                // Never seen this Pixel before
                var dieObj = new GameObject(peripheral.Name);
                dieObj.transform.SetParent(InternalBehaviour.Instance.transform);

                pixel = dieObj.AddComponent<BlePixel>();
                pixel.DisconnectedUnexpectedly += () => DestroyPixel(pixel);
                pixel.SubscribeToUserNotifyRequest(_notifyUser);
                pixel.SubscribeToPlayAudioClipRequest(_playAudioClip);

                _pixels.Add(pixel);
            }

            // Discard discovery event if peripheral is not available anymore (it might just have started connecting)
            if (pixel.connectionState <= PixelConnectionState.Available)
            {
                Debug.Log($"Discovered Pixel {peripheral.Name}, systemId is {peripheral.SystemId}");

                // Update Pixel
                pixel.Setup(peripheral);

                // And notify
                PixelDiscovered?.Invoke(pixel);
            }
        }

        #endregion

        #region Subscriptions for Pixel to app requests

        /// <summary>
        /// Subscribe to requests from Pixel dice to notify user.
        /// </summary>
        /// <param name="notifyUserCallback">The callback to be called when a Pixel requires to notify the user.</param>
        public static void SubscribeToUserNotifyRequest(NotifyUserCallback notifyUserCallback)
        {
            _notifyUser = notifyUserCallback;
            foreach (var p in _pixels)
            {
                p.SubscribeToUserNotifyRequest(notifyUserCallback);
            }
        }

        /// <summary>
        /// Subscribe to requests from Pixel dice to play an audio clip.
        /// </summary>
        /// <param name="playAudioClipCallback">The callback to be called when a Pixel requires to play an audio clip.</param>
        public static void SubscribeToPlayAudioClipRequest(PlayAudioClipCallback playAudioClipCallback)
        {
            _playAudioClip = playAudioClipCallback;
            foreach (var p in _pixels)
            {
                p.SubscribeToPlayAudioClipRequest(playAudioClipCallback);
            }
        }

        #endregion

        #region Connect and communicate with Pixels

        /// <summary>
        /// Reset errors on all know Pixel dice.
        /// </summary>
        public static void ResetErrors()
        {
            foreach (var pixel in _pixels)
            {
                pixel.ResetLastError();
            }
        }

        /// <summary>
        /// Requests to connect to the given Pixel.
        ///
        /// Each Pixel object maintains a connection counter which is incremented for each connection request.
        /// and decremented for each disconnection request. The same number of disconnection requests than
        /// connection requests must be made to disconnect the Pixel.
        ///
        /// This allows for different parts of the user code to request a connection or a disconnection without
        /// impacting each others.
        /// </summary>
        /// <param name="pixel">The Pixel to connect to.</param>
        /// <param name="requestCancelFunc">A callback which is called for each frame during connection,
        ///                                 it may return true to cancel the connection request.</param>
        /// <param name="onResult">An optional callback that is called when the operation completes
        ///                        successfully (true) or not (false) with an error message.</param>
        /// <returns>The coroutine running the request.</returns>
        public static Coroutine ConnectPixel(Pixel pixel, System.Func<bool> requestCancelFunc, ConnectionResultCallback onResult = null)
        {
            InternalBehaviour.CheckValid();

            if (pixel == null) throw new System.ArgumentNullException(nameof(pixel));
            if (requestCancelFunc == null) throw new System.ArgumentNullException(nameof(requestCancelFunc));

            return ConnectPixels(new Pixel[] { pixel }, requestCancelFunc, onResult);
        }

        /// <summary>
        /// Requests to connect to the given list of Pixel dice.
        ///
        /// Each Pixel object maintains a connection counter which is incremented for each connection request.
        /// and decremented for each disconnection request. The same number of disconnection requests than
        /// connection requests must be made to disconnect the Pixel.
        ///
        /// This allows for different parts of the user code to request a connection or a disconnection without
        /// impacting each others.
        /// </summary>
        /// <param name="pixels">The list of Pixels dice to connect to.</param>
        /// <param name="requestCancelFunc">A callback which is called for each frame during connection,
        ///                                 it may return true to cancel the connection request.</param>
        /// <param name="onResult">An optional callback that is called for each Pixel after all the connection
        ///                        operations have completed, whether successfully (true) or not (false)
        ///                        along with a message when an error was encountered.</param>
        /// <returns>The coroutine running the request.</returns>
        public static Coroutine ConnectPixels(IEnumerable<Pixel> pixels, System.Func<bool> requestCancelFunc, ConnectionResultCallback onResult = null)
        {
            InternalBehaviour.CheckValid();

            if (pixels == null) throw new System.ArgumentNullException(nameof(pixels));
            if (requestCancelFunc == null) throw new System.ArgumentNullException(nameof(requestCancelFunc));

            var pixelsList = new List<BlePixel>();
            foreach (var p in pixels)
            {
                var blePixel = p as BlePixel;
                if ((blePixel == null) || (!_pixels.Contains(p)))
                {
                    Debug.LogError("Some Pixels requested to be connected are either null or not in the " + nameof(DiceBag));
                    return null;
                }
                pixelsList.Add(blePixel);
            }
            if (pixelsList.Count == 0)
            {
                Debug.LogWarning("Empty list of Pixels requested to be connected");
                return null;
            }

            // Connect
            return StartCoroutine(ConnectAsync());

            IEnumerator ConnectAsync()
            {
                // requestCancelFunc() only need to return true once to cancel the operation
                bool isCancelled = false;
                bool UpdateIsCancelled() => isCancelled |= requestCancelFunc();

                // Array of error message for each Pixel connection attempt
                // - if null: still connecting
                // - if empty string: successfully connected
                var results = new string[pixelsList.Count];
                for (int i = 0; i < pixelsList.Count; ++i)
                {
                    var pixel = pixelsList[i];
                    _registeredPixels[pixel.systemId] = pixel.name;

                    // We found the Pixel, try to connect
                    int index = i; // Capture the current value of i
                    pixel.Connect((_, res, error) => results[index] = res ? "" : error);
                }

                // Wait for all Pixels to connect
                yield return new WaitUntil(() => results.All(msg => msg != null) || UpdateIsCancelled());

                if (isCancelled)
                {
                    // Disconnect any Pixel that just successfully connected or that are still connecting
                    for (int i = 0; i < pixelsList.Count; ++i)
                    {
                        if (string.IsNullOrEmpty(results[i]))
                        {
                            var pixel = pixelsList[i];
                            pixel?.Disconnect();
                        }
                        onResult?.Invoke(pixelsList[i], false, "Connection to Pixel canceled by application");
                    }
                }
                else if (onResult != null)
                {
                    // Report connection result(s)
                    for (int i = 0; i < pixelsList.Count; ++i)
                    {
                        bool connected = results[i] == "";
                        Debug.Assert((!connected) || _pixels.Contains(pixelsList[i]));
                        onResult.Invoke(pixelsList[i], connected, connected ? null : results[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Requests to disconnect to the given Pixel.
        /// 
        /// If a connection was requested several times before disconnecting, the same number
        /// of calls must be made to this method for the disconnection to happen, unless
        /// <paramref name="forceDisconnect"/> is true.
        /// </summary>
        /// <param name="pixel">The Pixel to disconnect from.</param>
        /// <param name="forceDisconnect">Whether to disconnect even if there were more connection requests
        ///                               than calls to disconnect.</param>
        /// <returns>The coroutine running the request.</returns>
        public static Coroutine DisconnectPixel(Pixel pixel, bool forceDisconnect = false)
        {
            InternalBehaviour.CheckValid();

            var blePixel = (BlePixel)pixel;
            return StartCoroutine(DisconnectAsync());

            IEnumerator DisconnectAsync()
            {
                if (blePixel != null)
                {
                    if (!_pixels.Contains(blePixel))
                    {
                        Debug.LogError("The Pixel requested to be disconnected is either null or not in the " + nameof(DiceBag));
                    }
                    else
                    {
                        bool? res = null;
                        blePixel.Disconnect((d, r, s) => res = r, forceDisconnect);

                        yield return new WaitUntil(() => res.HasValue);
                    }
                }
            }
        }

        // Destroys a Pixel instance and remove it from internal lists
        static void DestroyPixel(BlePixel pixel)
        {
            Debug.Assert(pixel);
            if (pixel)
            {
                if (pixel.isConnectingOrReady)
                {
                    Debug.LogWarning($"Destroying Pixel in {pixel.connectionState} state");
                }
                GameObject.Destroy(pixel.gameObject);
            }
            _pixels.Remove(pixel);
            _pixelsToDestroy.Remove(pixel);
        }

        #endregion

        private static void OnBluetoothStatusChanged(BluetoothStatus status)
        {
            if (status != BluetoothStatus.Ready)
            {
                StopScanning(forceStop: true);
            }
            BluetoothStatusChanged?.Invoke(status);
        }

        #region PersistentMonoBehaviourSingleton

        /// <summary>
        /// Internal <see cref="MonoBehaviour"/> that runs coroutines and check for Pixel to destroy
        /// on each Unity's call to <see cref="Update"/>.
        /// </summary>
        sealed class InternalBehaviour :
            BluetoothLE.Internal.PersistentMonoBehaviourSingleton<InternalBehaviour>,
            BluetoothLE.Internal.IPersistentMonoBehaviourSingleton
        {
            // Instance name
            string BluetoothLE.Internal.IPersistentMonoBehaviourSingleton.GameObjectName => "SystemicPixelsDiceBag";

            protected override void OnEnable()
            {
                base.OnEnable();
                Central.StatusChanged += OnBluetoothStatusChanged;
                if (Central.Status != BluetoothStatus.Unknown)
                {
                    OnBluetoothStatusChanged(Central.Status);
                }
                Central.Initialize();
            }

            protected override void OnDisable()
            {
                Central.Shutdown();
                Central.StatusChanged -= OnBluetoothStatusChanged;
                base.OnDisable();
            }

            // Update is called once per frame
            protected override void Update()
            {
                // Destroys Pixels
                List<BlePixel> destroyNow = null;
                foreach (var pixel in _pixelsToDestroy)
                {
                    if (!pixel.isConnectingOrReady)
                    {
                        if (destroyNow == null)
                        {
                            destroyNow = new List<BlePixel>();
                        }
                        destroyNow.Add(pixel);
                    }
                }
                if (destroyNow != null)
                {
                    foreach (var pixel in destroyNow)
                    {
                        DestroyPixel(pixel);
                    }
                }

                base.Update();
            }
        }

        static Coroutine StartCoroutine(IEnumerator enumerator, bool noThrow = false)
        {
            if (InternalBehaviour.CheckValid(noThrow))
            {
                return InternalBehaviour.Instance.StartCoroutine(enumerator);
            }
            return null;
        }

        #endregion
    }
}
