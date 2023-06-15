using Systemic.Unity.Pixels.Messages;
using UnityEngine;

namespace Systemic.Unity.Pixels
{
    /// <summary>
    /// Delegate for Pixel connection request result.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="ready">Whether the Pixel is ready for communications.</param>
    /// <param name="error">The error if it failed to connect.</param>
    public delegate void ConnectionResultCallback(Pixel pixel, bool ready, string error);

    /// <summary>
    /// Delegate for Pixel operations result.
    /// </summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="error">The error if the operation failed.</param>
    public delegate void OperationResultCallback(bool success, string error);

    /// <summary>
    /// Delegate for Pixel operations returning some data.
    /// </summary>
    /// <param name="data">The resulting data.</param>
    /// <param name="error">The error if the operation failed.</param>
    public delegate void DataOperationResultCallback(byte[] data, string error);

    /// <summary>
    /// Delegate for Pixel operations progress reporting.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="progress">The progress, between 0 and 1 included.</param>
    public delegate void OperationProgressCallback(Pixel pixel, float progress);

    /// <summary>
    /// Delegate for Pixel connection state events.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="state">The current connection sate.</param>
    public delegate void ConnectionStateChangedEventHandler(Pixel pixel, PixelConnectionState state);

    /// <summary>
    /// Delegate for Pixel communication error events.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="error">The type of error.</param>
    public delegate void ErrorRaisedEventHandler(Pixel pixel, PixelError error);

    /// <summary>
    /// Delegate for Pixel appearance setting changes.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="ledCount">Number of LEDs on the die.</param>
    /// <param name="design">Design and coloring of the die.</param>
    public delegate void AppearanceChangedEventHandler(Pixel pixel, int ledCount, PixelDesignAndColor design);

    /// <summary>
    /// Delegate for Pixel roll events.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="rollState">The roll state.</param>
    /// <param name="face">The face index, when applicable (face number is index + 1).</param>
    public delegate void RollStateChangedEventHandler(Pixel pixel, PixelRollState rollState, int face);

    /// <summary>
    /// Delegate for Pixel battery level changes.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="batteryLevel">The latest battery level reported by the die, in percent.</param>
    /// <param name="isCharging">Whether or not the battery is reported as charging.</param>
    public delegate void BatteryLevelChangedEventHandler(Pixel pixel, int batteryLevel, bool isCharging);

    /// <summary>
    /// Delegate for Pixel RSSI changes.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="rssi">The latest RSSI in dBm reported by the die.</param>
    public delegate void RssiChangedEventHandler(Pixel pixel, int rssi);

    /// <summary>
    /// Delegate for Pixel temperature changes.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="temperature">The latest temperature for the micro-controller and the battery
    ///                           in Celsius degrees reported by the die.</param>
    public delegate void TemperatureChangedEventHandler(Pixel pixel, float mcuTemperature, float batteryTemperature);

    /// <summary>
    /// Delegate for Pixel telemetry events.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="frame">The latest acceleration data reported by the die.</param>
    public delegate void TelemetryEventHandler(Pixel pixel, AccelerationFrame frame);

    /// <summary>
    /// Delegate for Pixel requests to notify user of some message, with the option to cancel the operation.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="text">The text to display to the user.</param>
    /// <param name="canCancel">Whether the user may cancel the operation.</param>
    /// <param name="userActionCallback">The callback to run once the user has acknowledged the message.
    ///                                  False may be passed to cancel the operation when applicable.</param>
    public delegate void NotifyUserCallback(Pixel pixel, string text, bool canCancel, System.Action<bool> userActionCallback);

    /// <summary>
    /// Delegate for Pixel requests to play an audio clip.
    /// </summary>
    /// <param name="pixel">The source of the event.</param>
    /// <param name="clipId">The audio clip id to play.</param>
    public delegate void PlayAudioClipCallback(Pixel pixel, uint clipId);
}
