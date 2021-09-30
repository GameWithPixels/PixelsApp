
namespace Systemic.Unity.BluetoothLE
{
    /// <summary>
    /// Delegate for Bluetooth radio status event.
    /// </summary>
    /// <param name="status">The new status.</param>
    public delegate void NativeBluetoothCallback(BluetoothStatus status);

    /// <summary>
    /// Delegate for BLE peripheral connection event, with the reason.
    /// </summary>
    /// <param name="connectionEvent">The peripheral event.</param>
    /// <param name="reason">The reason of the event.</param>
    public delegate void NativeConnectionEventCallback(ConnectionEvent connectionEvent, ConnectionEventReason reason);

    /// <summary>
    /// Delegate for BLE peripheral requests result.
    /// </summary>
    /// <param name="result">The request status.</param>
    public delegate void NativeRequestResultCallback(RequestStatus result);

    /// <summary>
    /// Delegate for returning a value and a status.
    /// </summary>
    /// <typeparam name="T">The type of the returned value.</typeparam>
    /// <param name="value">The returned value.</param>
    /// <param name="result">The request status.</param>
    public delegate void NativeValueRequestResultCallback<T>(T value, RequestStatus status);
}
