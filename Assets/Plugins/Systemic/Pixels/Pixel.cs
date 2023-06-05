using System.Collections.Generic;
using Systemic.Unity.Pixels.Messages;
using UnityEngine;

/// <summary>
/// A collection of C# classes for the Unity game engine that enables communications with Pixels dice.
/// The <see cref="Pixel"/> class represents a die peripheral and the <see cref="DiceBag"/> static class
/// implements methods for scanning for and connecting to Pixels.
/// </summary>
//! @ingroup Unity_CSharp
namespace Systemic.Unity.Pixels
{
    /// <summary>
    /// Represents a Pixel die.
    ///
    /// This class offers access to many settings and features of a Pixel.
    /// This abstract class does not implement a specific communication protocol with the dice, leaving the door
    /// open to have multiple implementations including a virtual die.
    /// Currently only Bluetooth communications are supported, see <see cref="DiceBag"/> to connect to and communicate
    /// with Bluetooth Low Energy Pixel dice.
    /// </summary>
    /// <remarks>
    /// The Pixel name is given by the parent class <see cref="MonoBehaviour"/> name property.
    /// </remarks>
    public abstract partial class Pixel : MonoBehaviour
    {
        //TODO upper case fields?

        // Use property to change value so it may properly raise the corresponding event
        PixelConnectionState _connectionState = PixelConnectionState.Invalid;

        // Events and callbacks
        RssiChangedEventHandler _notifyRssi;
        TelemetryEventHandler _notifyTelemetry;
        NotifyUserCallback _notifyUser;
        PlayAudioClipCallback _playAudioClip;

        // Maps a message type an event handler
        readonly Dictionary<MessageType, MessageReceivedEventHandler> _messageHandlers = new Dictionary<MessageType, MessageReceivedEventHandler>();

        #region Public properties

        /// <summary>
        /// Gets the connection state to the Pixel.
        /// </summary>
        public PixelConnectionState connectionState
        {
            get => _connectionState;
            protected set
            {
                EnsureRunningOnMainThread();

                if (value != _connectionState)
                {
                    Debug.Log($"Pixel {SafeName}: Connection state change, {_connectionState} => {value}");
                    var oldState = _connectionState;
                    _connectionState = value;
                    try
                    {
                        ConnectionStateChanged?.Invoke(this, oldState, value);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                if (connectionState == PixelConnectionState.Ready)
                {
                    if (_notifyRssi?.GetInvocationList().Length > 0)
                    {
                        ReportRssi(true);
                    }
                    if (_notifyTelemetry?.GetInvocationList().Length > 0)
                    {
                        ReportTelemetry(true);
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the connection state is set to available, meaning the Pixel can be connected to.
        /// </summary>
        public bool isAvailable => _connectionState == PixelConnectionState.Available;

        /// <summary>
        /// Indicates whether the connection state is set to ready, meaning the Pixel is connected and ready to communicate.
        /// </summary>
        public bool isReady => _connectionState == PixelConnectionState.Ready;

        /// <summary>
        /// Get the last error that happened during communications with the Pixel.
        /// </summary>
        public PixelError lastError { get; protected set; }

        /// <summary>
        /// Gets the unique system id assigned to the Pixel. This value is platform specific and may change over long periods of time.
        /// </summary>
        public string systemId { get; protected set; }

        /// <summary>
        /// Gets the Pixel unique device id.
        ///
        /// This value is set when the Pixel is being scanned or when connected.
        /// </summary>
        public uint pixelId { get; protected set; }

        /// <summary>
        /// Gets the number of LEDs for the Pixel.
        ///
        /// This value is set when the Pixel is being scanned or once when connected.
        /// </summary>
        public int ledCount { get; protected set; }

        /// <summary>
        /// Gets the Pixel combination of design and color.
        ///
        /// This value is set when the Pixel is being scanned or once when connected.
        /// </summary>
        public PixelDesignAndColor designAndColor { get; protected set; } = PixelDesignAndColor.Unknown;

        /// <summary>
        /// Gets the Pixel firmware build Unix timestamp.
        ///
        /// This value is set when the Pixel is being scanned or when connected.
        /// </summary>
        public uint buildTimestamp { get; protected set; }

        /// <summary>
        /// Gets the Pixel firmware build data/time.
        ///
        /// This value is the <see cref="buildTimestamp"/> converted to a DataTime value.
        /// </summary>
        public System.DateTime buildDateTime => UnixTimestampToDateTime(buildTimestamp);

        /// <summary>
        /// Get the hash value of the animation data loaded on the Pixel.
        ///
        /// This value is set once when the Pixel is being connected.
        /// </summary>
        public uint dataSetHash { get; protected set; }

        /// <summary>
        /// Get the size of memory that can be used to store animation data on the Pixel.
        ///
        /// This value is set once when the Pixel is being connected.
        /// </summary>
        public uint availableFlashSize { get; protected set; }

        /// <summary>
        /// Gets the Pixel current roll state.
        ///
        /// This value is set when the Pixel is being scanned or when connected.
        /// </summary>
        public PixelRollState rollState { get; private set; } = PixelRollState.Unknown;

        /// <summary>
        /// Gets Pixel the current face that is up.
        /// 
        /// This value is set when the Pixel is being scanned or when connected.
        /// </summary>
        public int currentFace { get; private set; }

        /// <summary>
        /// Gets the Pixel last read battery level in percent.
        /// 
        /// This value is set when the Pixel is being scanned and
        /// <see cref="UpdateBatteryLevelAsync(OperationResultCallback)"/> is called while connected.
        /// </summary>
        public int batteryLevel { get; private set; }

        /// <summary>
        /// Indicates whether or not the Pixel was last reported as charging.
        ///
        /// This value is only set when
        /// <see cref="UpdateBatteryLevelAsync(OperationResultCallback)"/> is called while connected.
        /// </summary>
        public bool isCharging { get; private set; }

        /// <summary>
        /// Gets the Pixel last read Received Signal Strength Indicator (RSSI) value.
        ///
        /// This value is set when the Pixel is being scanned or when
        /// <see cref="UpdateRssiAsync(OperationResultCallback)"/> is called while connected.
        /// </summary>
        public int rssi { get; private set; }

        /// <summary>
        /// Pixel microcontroller temperature in degree Celsius.
        /// </summary>
        public float mcuTemperature { get; private set; }

        /// <summary>
        /// Pixel battery temperature in degree Celsius.
        /// </summary>
        public float batteryTemperature { get; private set; }

        #endregion

        #region Public events

        /// <summary>
        /// Event raised when the Pixel connection state changes.
        /// </summary>
        public ConnectionStateChangedEventHandler ConnectionStateChanged;

        /// <summary>
        /// Event raised when communications with the Pixel encountered an error.
        /// </summary>
        public ErrorRaisedEventHandler ErrorEncountered;

        /// <summary>
        /// Event raised when the Pixel appearance setting is changed.
        /// </summary>
        public AppearanceChangedEventHandler AppearanceChanged;

        /// <summary>
        /// Event raised when the Pixel roll state changes.
        /// </summary>
        public RollStateChangedEventHandler RollStateChanged;

        /// <summary>
        /// Event raised when the battery level reported by the Pixel changes.
        /// </summary>
        public BatteryLevelChangedEventHandler BatteryLevelChanged;

        /// <summary>
        /// Event raised when the RSSI value reported by the Pixel changes.
        /// </summary>
        public event RssiChangedEventHandler RssiChanged
        {
            add
            {
                if (_notifyRssi == null && connectionState == PixelConnectionState.Ready)
                {
                    // The first time around, we make sure to request RSSI from the Pixel
                        ReportRssi(true);
                }
                _notifyRssi += value;
            }
            remove
            {
                _notifyRssi -= value;
                if ((_notifyRssi == null || _notifyRssi.GetInvocationList().Length == 0)
                     && connectionState == PixelConnectionState.Ready)
                {
                    // Unregister from the Pixel telemetry
                    ReportRssi(false);
                }
            }
        }

        /// <summary>
        /// Event raised when the temperature reported by the Pixel changes.
        /// </summary>
        public TemperatureChangedEventHandler TemperatureChanged;

        /// <summary>
        /// Event raised when telemetry data is received.
        /// </summary>
        public event TelemetryEventHandler TelemetryReceived
        {
            add
            {
                if (_notifyTelemetry == null && connectionState == PixelConnectionState.Ready)
                {
                    // The first time around, we make sure to request telemetry from the Pixel
                    ReportTelemetry(true);
                }
                _notifyTelemetry += value;
            }
            remove
            {
                _notifyTelemetry -= value;
                if ((_notifyTelemetry == null || _notifyTelemetry.GetInvocationList().Length == 0)
                     && connectionState == PixelConnectionState.Ready)
                {
                    // Unregister from the Pixel telemetry
                    ReportTelemetry(false);
                }
            }
        }

        #endregion

        /// <summary>
        /// Subscribe to requests send by the Pixel to notify user.
        /// 
        /// Replaces the callback passed in a previous call to this method.
        /// </summary>
        /// <param name="notifyUserCallback">The callback to run, pass null to unsubscribe.</param>
        public void SubscribeToUserNotifyRequest(NotifyUserCallback notifyUserCallback)
        {
            _notifyUser = notifyUserCallback;
        }

        /// <summary>
        /// Subscribes to requests send by the Pixel to play an audio clip.
        /// 
        /// Replaces the callback passed in a previous call to this method.
        /// </summary>
        /// <param name="playAudioClipCallback">The callback to run, pass null to unsubscribe.</param>
        public void SubscribeToPlayAudioClipRequest(PlayAudioClipCallback playAudioClipCallback)
        {
            _playAudioClip = playAudioClipCallback;
        }

        /// <summary>
        /// Internal event handler for message notification.
        /// </summary>
        /// <param name="message">The message object.</param>
        protected delegate void MessageReceivedEventHandler(IPixelMessage message);

        /// <summary>
        /// Use this property to access the Pixel name without having to first check if the object
        /// is considered destroyed by Unity (to avoid generating an error).
        /// </summary>
        protected string SafeName => this != null ? name : "(destroyed)";

        /// <summary>
        /// Abstract method to send a message to the Pixel.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        protected abstract IOperationEnumerator SendMessageAsync(byte[] bytes, float timeout = 0);

        /// <summary>
        /// Helper to method to check if we are running on the main thread. Throws an exception if running on another thread.
        /// </summary>
        protected void EnsureRunningOnMainThread()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != 1)
            {
                throw new System.InvalidOperationException($"Methods of type {GetType()} can only be called from the main thread");
            }
        }

        /// <summary>
        /// Add an event hander for the given message type.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        /// <param name="eventHandler">The event handler to add.</param>
        protected void AddMessageHandler(MessageType messageType, MessageReceivedEventHandler eventHandler)
        {
            if (messageType == MessageType.None) throw new System.ArgumentException($"Message type can't be {messageType}", nameof(messageType));
            if (eventHandler == null) throw new System.ArgumentNullException(nameof(eventHandler));

            if (_messageHandlers.TryGetValue(messageType, out MessageReceivedEventHandler del))
            {
                del += eventHandler;
                _messageHandlers[messageType] = del;
            }
            else
            {
                _messageHandlers.Add(messageType, eventHandler);
            }
        }

        /// <summary>
        /// Remove an event hander for the given message type.
        /// </summary>
        /// <param name="messageType">The type of message to unsubscribe from.</param>
        /// <param name="eventHandler">The event handler to remove.</param>
        protected void RemoveMessageHandler(MessageType messageType, MessageReceivedEventHandler eventHandler)
        {
            if (messageType == MessageType.None) throw new System.ArgumentException($"Message type can't be {messageType}", nameof(messageType));
            if (eventHandler == null) throw new System.ArgumentNullException(nameof(eventHandler));

            if (_messageHandlers.TryGetValue(messageType, out MessageReceivedEventHandler del))
            {
                del -= eventHandler;
                if (del == null)
                {
                    _messageHandlers.Remove(messageType);
                }
                else
                {
                    _messageHandlers[messageType] = del;
                }
            }
        }

        /// <summary>
        /// Notify the event handlers for the given message, based on its type.
        /// </summary>
        /// <param name="message">The message object.</param>
        protected void NotifyMessageHandler(IPixelMessage message)
        {
            if (message == null) throw new System.ArgumentNullException(nameof(message));

            if (_messageHandlers.TryGetValue(message.type, out MessageReceivedEventHandler del))
            {
                del.Invoke(message);
            }
        }

        /// <summary>
        /// Starts a coroutine that sends a message to the Pixel.
        /// </summary>
        /// <typeparam name="T">Type of the message.</typeparam>
        /// <param name="message">The message instance to send.</param>
        protected void PostMessage<T>(T message)
            where T : IPixelMessage
        {
            EnsureRunningOnMainThread();

            Debug.Log($"Pixel {SafeName}: Posting message of type {message.GetType()}");

            StartCoroutine(SendMessageAsync(Marshaling.ToByteArray(message)));
        }

        /// <summary>
        /// Register the default message handlers, called once during instance initialization.
        /// </summary>
        protected virtual void RegisterDefaultMessageHandlers()
        {
            // Setup delegates for face and telemetry
            _messageHandlers.Add(MessageType.IAmADie, msg => ProcessIAmADieMessage((IAmADie)msg));
            _messageHandlers.Add(MessageType.RollState, msg => ProcessRollStateMessage((RollState)msg));
            _messageHandlers.Add(MessageType.BatteryLevel, msg => ProcessBatteryLevelMessage((BatteryLevel)msg));
            _messageHandlers.Add(MessageType.Rssi, msg => ProcessRssiMessage((Rssi)msg));
            _messageHandlers.Add(MessageType.Temperature, msg => ProcessTemperatureMessage((Temperature)msg));
            _messageHandlers.Add(MessageType.Telemetry, msg => ProcessTelemetryMessage((Telemetry)msg));
            _messageHandlers.Add(MessageType.DebugLog, msg => ProcessDebugLogMessage((DebugLog)msg));
            _messageHandlers.Add(MessageType.NotifyUser, msg => ProcessNotifyUserMessage((NotifyUser)msg));
            _messageHandlers.Add(MessageType.PlaySound, msg => ProcessPlayAudioClip((PlaySound)msg));

            void ProcessIAmADieMessage(IAmADie message)
            {
                Debug.Log($"Pixel {SafeName}: {message.availableFlashSize} bytes available for data,"
                    + $" current dataset hash {message.dataSetHash:X08}, firmware build is {UnixTimestampToDateTime(message.buildTimestamp)}");

                // Update instance
                bool appearanceChanged = ledCount != message.ledCount || designAndColor != message.designAndColor;
                ledCount = message.ledCount;
                designAndColor = message.designAndColor;
                dataSetHash = message.dataSetHash;
                availableFlashSize = message.availableFlashSize;
                pixelId = message.pixelId;
                buildTimestamp = message.buildTimestamp;

                // Roll state
                NotifyRollState(message.rollState, message.rollFaceIndex);

                // Battery level
                NotifyBatteryLevel(message.batteryLevelPercent, message.batteryState);

                if (appearanceChanged)
                {
                    // Notify
                    AppearanceChanged?.Invoke(this, ledCount, designAndColor);
                }
            }

            void ProcessRollStateMessage(RollState message)
            {
                NotifyRollState(message.state, message.faceIndex);
            }

            void ProcessBatteryLevelMessage(BatteryLevel message)
            {
                NotifyBatteryLevel(message.levelPercent, message.batteryState);
            }

            void ProcessRssiMessage(Rssi message)
            {
                NotifyRssi(message.value);
            }

            void ProcessTemperatureMessage(Temperature message)
            {
                NotifyTemperature(message.mcuTemperatureTimes100, message.batteryTemperatureTimes100);
            }

            void ProcessTelemetryMessage(Telemetry message)
            {
                NotifyRollState(message.accelFrame.rollState, message.accelFrame.faceIndex);
                NotifyBatteryLevel(message.batteryLevelPercent, message.batteryState >= PixelBatteryState.Charging);
                NotifyRssi(message.rssi);
                NotifyTemperature(message.mcuTemperatureTimes100, message.batteryTemperatureTimes100);

                // Notify
                _notifyTelemetry?.Invoke(this, message.accelFrame);
            }

            void ProcessDebugLogMessage(DebugLog message)
            {
                string text = Marshaling.BytesToString(message.data);
                Debug.Log($"Pixel {SafeName}: {text}");
            }

            void ProcessNotifyUserMessage(NotifyUser message)
            {
                //bool ok = message.ok != 0;
                bool cancel = message.cancel != 0;
                //float timeout = message.timeout_s;
                string text = Marshaling.BytesToString(message.data);
                _notifyUser?.Invoke(this, text, cancel,
                    res => PostMessage(new NotifyUserAck() { okCancel = (byte)(res ? 1 : 0) }));
            }

            void ProcessPlayAudioClip(PlaySound message)
            {
                _playAudioClip?.Invoke(this, message.clipId);
            }
        }

        protected void NotifyRollState(PixelRollState state, byte faceIndex)
        {
            int face = faceIndex + 1;
            if ((state != rollState) || (face != this.currentFace))
            {
                // Update instance
                rollState = state;
                currentFace = face;

                // Notify
                Debug.Log($"Pixel {SafeName}: Notifying roll state: {rollState}, face: {currentFace}");
                RollStateChanged?.Invoke(this, rollState, this.currentFace);
            }
        }

        protected void NotifyBatteryLevel(int level, PixelBatteryState state)
        {
            bool charging = state == PixelBatteryState.Charging || state == PixelBatteryState.Done;
            NotifyBatteryLevel(level, charging);
        }

        protected void NotifyBatteryLevel(int level, bool charging)
        {
            if ((batteryLevel != level) || (isCharging != charging))
            {
                batteryLevel = level;
                isCharging = charging;

                Debug.Log($"Pixel {SafeName}: Notifying battery level: {batteryLevel}, isCharging: {isCharging}");
                BatteryLevelChanged?.Invoke(this, batteryLevel, isCharging);
            }
        }

        protected void NotifyRssi(int newRssi)
        {
            if (newRssi == 0) Debug.LogError("ZERO RSSSSI!!!");

            if (rssi != newRssi)
            {
                rssi = newRssi;

                Debug.Log($"Pixel {SafeName}: Notifying RSSI: {rssi}");
                _notifyRssi?.Invoke(this, rssi);
            }
        }

        protected void NotifyTemperature(int newMcuTempTimes100, int newBatteryTempTimes100)
        {
            float newMcuTemp = newMcuTempTimes100 / 100f;
            float newBatteryTemp = newBatteryTempTimes100 / 100f;
            if (mcuTemperature != newMcuTemp || batteryTemperature != newBatteryTemp)
            {
                mcuTemperature = newMcuTemp;
                batteryTemperature = newBatteryTemp;

                Debug.Log($"Pixel {SafeName}: Notifying MCU temperature = {mcuTemperature} and battery temperature = {batteryTemperature}");
                TemperatureChanged?.Invoke(this, mcuTemperature, batteryTemperature);
            }
        }

        // Called when the behaviour will be destroyed by Unity
        protected virtual void OnDestroy()
        {
            connectionState = PixelConnectionState.Invalid;
        }

        // Awake is called when the behaviour is being loaded
        void Awake()
        {
            RegisterDefaultMessageHandlers();
        }

        // https://stackoverflow.com/a/250400
        static System.DateTime UnixTimestampToDateTime(uint unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        }
    }
}