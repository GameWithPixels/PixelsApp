using System.Collections.Generic;
using Systemic.Unity.Pixels.Messages;
using UnityEngine;

/// <summary>
/// A collection of C# classes for the Unity game engine that enables communications with Pixel dice.
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
                    ConnectionStateChanged?.Invoke(this, oldState, value);
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
        /// Gets the number of faces for the Pixel.
        /// </summary>
        public int faceCount { get; protected set; }

        /// <summary>
        /// Gets the Pixel combination of design and color.
        /// </summary>
        public PixelDesignAndColor designAndColor { get; protected set; } = PixelDesignAndColor.Unknown;

        /// <summary>
        /// Get the version id of the firmware running on the Pixel.
        /// </summary>
        public string firmwareVersionId { get; protected set; } = "Unknown";

        /// <summary>
        /// Get the hash value of the animation data loaded on the Pixel.
        /// </summary>
        public uint dataSetHash { get; protected set; }

        /// <summary>
        /// Get the size of memory that can be used to store animation data on the Pixel.
        /// </summary>
        public uint flashSize { get; protected set; }

        /// <summary>
        /// Gets the Pixel current roll state.
        /// </summary>
        public PixelRollState rollState { get; protected set; } = PixelRollState.Unknown;

        /// <summary>
        /// Gets Pixel the current face that is up.
        /// </summary>
        public int face { get; protected set; } = -1; //TODO change to face number rather than index

        /// <summary>
        /// Gets the Pixel last read battery level.
        /// The value is normalized between 0 and 1 included.
        /// </summary>
        public float batteryLevel { get; protected set; }

        /// <summary>
        /// Indicates whether or not the Pixel was last reported as charging.
        /// </summary>
        public bool isCharging { get; protected set; }

        /// <summary>
        /// Gets the Pixel last read Received Signal Strength Indicator (RSSI) value.
        /// </summary>
        public int rssi { get; protected set; }

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
        public RssiChangedEventHandler RssiChanged;

        /// <summary>
        /// Event raised when telemetry data is received.
        /// </summary>
        public event TelemetryEventHandler TelemetryReceived
        {
            add
            {
                if (_notifyTelemetry == null)
                {
                    // The first time around, we make sure to request telemetry from the Pixel
                    RequestTelemetry(true);
                }
                _notifyTelemetry += value;
            }
            remove
            {
                _notifyTelemetry -= value;
                if (_notifyTelemetry == null || _notifyTelemetry.GetInvocationList().Length == 0)
                {
                    if (connectionState == PixelConnectionState.Ready)
                    {
                        // Unregister from the Pixel telemetry
                        RequestTelemetry(false);
                    }
                    // Otherwise we can't send bluetooth packets to the Pixel, can we?
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
            _messageHandlers.Add(MessageType.Telemetry, msg => ProcessTelemetryMessage((AccelerationState)msg));
            _messageHandlers.Add(MessageType.DebugLog, msg => ProcessDebugLogMessage((DebugLog)msg));
            _messageHandlers.Add(MessageType.NotifyUser, msg => ProcessNotifyUserMessage((NotifyUser)msg));
            _messageHandlers.Add(MessageType.PlaySound, msg => ProcessPlayAudioClip((PlaySound)msg));

            void ProcessIAmADieMessage(IAmADie message)
            {
                Debug.Log($"Pixel {SafeName}: {message.flashSize} bytes available for data,"
                    + $" current dataset hash {message.dataSetHash:X08}, firmware version is {message.versionInfo}");

                // Update instance
                bool appearanceChanged = faceCount != message.faceCount || designAndColor != message.designAndColor;
                faceCount = message.faceCount;
                designAndColor = message.designAndColor;
                dataSetHash = message.dataSetHash;
                flashSize = message.flashSize;
                firmwareVersionId = message.versionInfo;

                if (appearanceChanged)
                {
                    // Notify
                    AppearanceChanged?.Invoke(this, faceCount, designAndColor);
                }
            }

            void ProcessRollStateMessage(RollState message)
            {
                Debug.Log($"Pixel {SafeName}: State is {message.state}, {message.face}");

                if ((message.state != rollState) || (message.face != face))
                {
                    // Update instance
                    rollState = message.state;
                    face = message.face;

                    // Notify
                    RollStateChanged?.Invoke(this, rollState, face + 1);
                }
            }

            void ProcessTelemetryMessage(AccelerationState message)
            {
                // Notify
                _notifyTelemetry?.Invoke(this, message.data);
            }

            void ProcessDebugLogMessage(DebugLog message)
            {
                string text = System.Text.Encoding.UTF8.GetString(message.data, 0, message.data.Length);
                Debug.Log($"Pixel {SafeName}: {text}");
            }

            void ProcessNotifyUserMessage(NotifyUser message)
            {
                //bool ok = message.ok != 0;
                bool cancel = message.cancel != 0;
                //float timeout = message.timeout_s;
                string text = System.Text.Encoding.UTF8.GetString(message.data, 0, message.data.Length);
                _notifyUser?.Invoke(this, text, cancel,
                    res => PostMessage(new NotifyUserAck() { okCancel = (byte)(res ? 1 : 0) }));
            }

            void ProcessPlayAudioClip(PlaySound message)
            {
                _playAudioClip?.Invoke(this, message.clipId);
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
    }
}