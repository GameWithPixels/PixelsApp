using System;

namespace Systemic.Unity.BluetoothLE
{
    /// <summary>
    /// Bluetooth radio status.
    /// </summary>
    public enum BluetoothStatus
    {
        /// Bluetooth radio is disabled.
        Disabled,

        /// Bluetooth radio is enabled.
        Enabled,
    }

    /// <summary>
    /// Peripheral requests statuses.
    /// </summary>
    /// <remarks>
    /// Matches C++ enum Systemic::BluetoothLE::BleRequestStatus.
    /// </remarks>
    public enum RequestStatus
    {
        /// The request completed successfully.
        Success,

        /// The request completed with a non-specific error.
        Error,

        /// The request is still in progress.
        InProgress,

        /// The request was canceled.
        Canceled,

        /// The request was aborted because the peripheral got disconnected.
        Disconnected,

        /// The request did not run because the given peripheral is not valid.
        InvalidPeripheral,

        /// The request did not run because the operation is not valid or permitted.
        InvalidCall,

        /// The request did not run because some of its parameters are invalid.
        InvalidParameters,

        /// The request failed because of the operation is not supported by the peripheral.
        NotSupported,

        /// The request failed because of BLE protocol error.
        ProtocolError,

        /// The request failed because it was denied access.
        AccessDenied,

        /// The request failed because the Bluetooth radio is off.
        AdapterOff,

        /// The request did not succeed after the timeout period.
        Timeout,
    }

    /// <summary>
    /// Peripheral connection events.
    /// </summary>
    /// <remarks>
    /// Matches C++ enum Systemic::BluetoothLE::ConnectionEvent and Objective-C SGBleConnectionEvent.
    /// </remarks>
    public enum ConnectionEvent
    {
        /// Raised at the beginning of the connect sequence and is followed either by Connected or FailedToConnect.
        Connecting,

        /// Raised once the peripheral is connected, just before services are being discovered.
        Connected,

        /// Raised when the peripheral fails to connect, the reason for the failure is also given.
        FailedToConnect,

        /// Raised after a Connected event, once the required services have been discovered.
        Ready,

        /// Raised at the beginning of a user initiated disconnect.
        Disconnecting,

        /// Raised when the peripheral is disconnected, the reason for the disconnection is also given.
        Disconnected,
    }

    /// <summary>
    /// Peripheral connection event reasons.
    /// </summary>
    /// <remarks>
    /// Matches C++ enum Systemic::BluetoothLE::ConnectionEventReason and Objective-C SGBleConnectionEventReason.
    /// </remarks>
    public enum ConnectionEventReason
    {
        /// The disconnect happened for an unknown reason.
        Unknown = -1,

        /// The disconnect was initiated by user.
        Success = 0,

        /// Connection attempt canceled by user.
        Canceled,

        /// Peripheral doesn't have all required services.
        NotSupported,

        /// Peripheral didn't responded in time.
        Timeout,

        /// Peripheral was disconnected while in "auto connect" mode.
        LinkLoss,

        /// The local device Bluetooth adapter is off.
        AdapterOff,

        /// Disconnection was initiated by peripheral.
        Peripheral,
    }

    /// <summary>
    /// Standard BLE values for characteristic properties.
    /// </summary>
    [Flags]
    public enum CharacteristicProperties : ulong
    {
        None = 0,

        /// Characteristic is broadcastable.
        Broadcast = 0x001,

        /// Characteristic is readable.
        Read = 0x002,

        /// Characteristic can be written without response.
        WriteWithoutResponse = 0x004,

        /// Characteristic can be written.
        Write = 0x008,

        /// Characteristic supports notification.
        Notify = 0x010,

        /// Characteristic supports indication.
        Indicate = 0x020,

        /// Characteristic supports write with signature.
        SignedWrite = 0x040,

        /// Characteristic has extended properties.
        ExtendedProperties = 0x080,

        /// Characteristic notification uses encryption.
        NotifyEncryptionRequired = 0x100,

        /// Characteristic indication uses encryption.
        IndicateEncryptionRequired = 0x200,
    }
}
