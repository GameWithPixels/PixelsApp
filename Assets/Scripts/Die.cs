﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Animations;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

public partial class Die
	: MonoBehaviour
    , Central.IDie
{
	const float SCALE_2G = 2.0f;
	const float SCALE_4G = 4.0f;
	const float SCALE_8G = 8.0f;
	const float scale = SCALE_8G;

    public enum RollState : byte
    {
        Unknown = 0,
        OnFace,
        Handling,
        Rolling,
        Crooked
    };

    [System.Serializable]
    public struct Settings
    {
        string name;
        float sigmaDecayFast;
        float sigmaDecaySlow;
        int minRollTime; // ms
    }

    public enum ConnectionState
    {
        Disconnected = 0,
        Advertising,
        Connecting,
        Connected,
        FetchingId,
        FetchingState,
        Ready
    }

    ConnectionState _connectionState = ConnectionState.Disconnected;
    public ConnectionState connectionState
    {
        get { return _connectionState; }
        private set
        {
            if (value != _connectionState)
            {
                _connectionState = value;
                OnConnectionStateChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// This data structure mirrors the data in firmware/bluetooth/bluetooth_stack.cpp
    /// </sumary>
    [System.Serializable]
    public struct CustomAdvertisingData
    {
        // Die type identification
        public DiceVariants.DesignAndColor designAndColor; // Physical look, also only 8 bits
        public byte faceCount; // Which kind of dice this is

        // Current state
        public Die.RollState rollState; // Indicates whether the dice is being shaken
        public byte currentFace; // Which face is currently up
    };

    public int faceCount { get; private set; } = 0;
    public DiceVariants.DesignAndColor designAndColor { get; private set; } = DiceVariants.DesignAndColor.Unknown;
    public System.UInt64 deviceId { get; private set; } = 0;
    public string firmwareVersionId { get; private set; } = "Unknown";
    public string address { get; private set; } = ""; // name is stored on the gameObject itself

    public RollState state { get; private set; } = RollState.Unknown;
    public int face { get; private set; } = -1;

	public delegate void TelemetryEvent(Die die, AccelFrame frame);
    public TelemetryEvent _OnTelemetry;
    public event TelemetryEvent OnTelemetry
    {
        add
        {
            if (_OnTelemetry == null)
            {
                // The first time around, we make sure to request telemetry from the die
                RequestTelemetry(true);
            }
            _OnTelemetry += value;
        }
        remove
        {
            _OnTelemetry -= value;
            if (_OnTelemetry == null || _OnTelemetry.GetInvocationList().Length == 0)
            {
                if (connectionState == ConnectionState.Ready)
                {
                    // Deregister from the die telemetry
                    RequestTelemetry(false);
                }
                // Otherwise we can't send bluetooth packets to the die, can we?
            }
        }
    }

    public delegate void StateChangedEvent(Die die, RollState newState, int newFace);
    public StateChangedEvent OnStateChanged;

    public delegate void ConnectionStateChangedEvent(Die die, ConnectionState newConnectionState);
    public ConnectionStateChangedEvent OnConnectionStateChanged;

    public delegate void SettingsChangedEvent(Die die);
    public SettingsChangedEvent OnSettingsChanged;

	public delegate void AppearanceChangedEvent(Die die, int newFaceCount, DiceVariants.DesignAndColor newDesign);
    public AppearanceChangedEvent OnAppearanceChanged;

    // Lock so that only one 'operation' can happen at a time on a die
    // Note: lock is not a real multithreaded lock!
    bool bluetoothOperationInProgress = false;

    // Internal delegate per message type
    delegate void MessageReceivedDelegate(DieMessage msg);
    Dictionary<DieMessageType, MessageReceivedDelegate> messageDelegates;

    void Awake()
	{
        messageDelegates = new Dictionary<DieMessageType, MessageReceivedDelegate>();

        // Setup delegates for face and telemetry
        messageDelegates.Add(DieMessageType.State, OnStateMessage);
        messageDelegates.Add(DieMessageType.Telemetry, OnTelemetryMessage);
        messageDelegates.Add(DieMessageType.DebugLog, OnDebugLogMessage);
        messageDelegates.Add(DieMessageType.NotifyUser, OnNotifyUserMessage);
    }

    public void Setup(string name, string address, System.UInt64 deviceId, int faceCount, DiceVariants.DesignAndColor design)
    {
        bool appearanceChanged = faceCount != this.faceCount || design != this.designAndColor;
        this.name = name;
        this.address = address;
        this.deviceId = deviceId;
        this.faceCount = faceCount;
        this.designAndColor = design;
        this.connectionState = ConnectionState.Disconnected;
    }

    public void UpdateAddress(string address)
    {
        this.address = address;
    }

    public void UpdateAdvertisingData(CustomAdvertisingData newData)
    {
        bool appearanceChanged = faceCount != newData.faceCount || designAndColor != newData.designAndColor;
        bool rollStateChanged = state != newData.rollState || face != newData.currentFace;
        faceCount = newData.faceCount;
        designAndColor = newData.designAndColor;
        state = newData.rollState;
        face = newData.currentFace;
        if (appearanceChanged)
        {
            OnAppearanceChanged?.Invoke(this, faceCount, designAndColor);
        }
        if (rollStateChanged)
        {
            OnStateChanged?.Invoke(this, state, face);
        }
    }

    public void OnAdvertising()
    {
        connectionState = ConnectionState.Advertising;
    }

	public void OnConnected()
	{
        connectionState = ConnectionState.Connected;
	}

    public void UpdateInfo(System.Action<Die, bool> onInfoUpdatedCallback)
    {
        if (connectionState == ConnectionState.Connected)
        {
            StartCoroutine(UpdateInfoCr(onInfoUpdatedCallback));
        }
        else
        {
            onInfoUpdatedCallback?.Invoke(this, false);
        }
    }

    IEnumerator UpdateInfoCr(System.Action<Die, bool> onInfoUpdatedCallback)
    {
        // Ask the die who it is!
        connectionState = ConnectionState.FetchingId;
        yield return GetDieInfo();

        // Ping the die so we know its initial state
        connectionState = ConnectionState.FetchingState;
        yield return Ping();

        connectionState = ConnectionState.Ready;

        onInfoUpdatedCallback?.Invoke(this, true);
    }

    public void OnDisconnected()
    {
        connectionState = ConnectionState.Disconnected;
    }

    public void OnData(byte[] data)
    {
        // Process the message coming from the actual die!
        var message = DieMessages.FromByteArray(data);
        MessageReceivedDelegate del;
        if (messageDelegates.TryGetValue(message.type, out del))
        {
            del.Invoke(message);
        }
    }

}
