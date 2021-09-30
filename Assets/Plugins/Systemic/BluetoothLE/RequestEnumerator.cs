using System;
using System.Collections;
using UnityEngine;

namespace Systemic.Unity.BluetoothLE
{
    /// <summary>
    /// List of asynchronous BLE operations handled though requests.
    /// </summary>
    public enum RequestOperation
    {
        /// Connect to peripheral.
        ConnectPeripheral,

        /// Disconnect from peripheral.
        DisconnectPeripheral,

        /// Read peripheral RSSI value.
        ReadPeripheralRssi,

        /// Request peripheral to change its MTU to a given value.
        RequestPeripheralMtu,

        /// Read a peripheral's characteristic value.
        ReadCharacteristic,

        /// Write to a peripheral's characteristic.
        WriteCharacteristic,

        /// Subscribe to a peripheral's characteristic.
        SubscribeCharacteristic,

        /// Unsubscribe from a peripheral's characteristic.
        UnsubscribeCharacteristic,
    }

    /// <summary>
    /// Enumerator handling a request to a BLE peripheral.
    /// Instances are meant to be run as coroutines.
    /// </summary>
    public class RequestEnumerator : IEnumerator
    {
        readonly double _timeout;
        RequestStatus? _status;

        /// <summary>
        /// Gets the operation being requested.
        /// </summary>
        public RequestOperation Operation { get; }

        /// <summary>
        /// Indicates whether the request has completed (successfully or not).
        /// </summary>
        public bool IsDone => _status.HasValue;

        /// <summary>
        /// Indicates whether the request was successful.
        /// </summary>
        public bool IsSuccess => _status.HasValue && (_status.Value == RequestStatus.Success);

        /// <summary>
        /// Indicates whether the request has timed-out.
        /// </summary>
        public bool IsTimeout { get; private set; }

        /// <summary>
        /// Gets the request current status.
        /// </summary>
        public RequestStatus RequestStatus => _status.HasValue ? _status.Value : RequestStatus.InProgress;

        /// <summary>
        /// Gets the request status as a string or null if there was no error.
        /// </summary>
        public string Error => RequestStatus switch
        {
            RequestStatus.Success => null,
            RequestStatus.InProgress => "Operation in progress",
            RequestStatus.Canceled => "Operation canceled",
            RequestStatus.InvalidPeripheral => "Invalid peripheral",
            RequestStatus.InvalidCall => "Invalid call",
            RequestStatus.InvalidParameters => "Invalid parameters",
            RequestStatus.NotSupported => "Operation not supported",
            RequestStatus.ProtocolError => "GATT protocol error",
            RequestStatus.AccessDenied => "Access denied",
            RequestStatus.Timeout => "Timeout",
            _ => "Unknown error",
        };

        /// <summary>
        /// Gets the current object, always null.
        /// </summary>
        public object Current => null;

        /// <summary>
        /// Gets the peripheral for which the request was made.
        /// </summary>
        private protected NativePeripheralHandle Peripheral { get; }

        /// <summary>
        /// Initializes a request enumerator with a given operation, peripheral, timeout value and
        /// an action to invoke if the peripheral is valid.
        /// </summary>
        /// <param name="operation">The operation to run.</param>
        /// <param name="nativeHandle">The peripheral for which to run the operation.</param>
        /// <param name="timeoutSec">The timeout in seconds.</param>
        /// <param name="action">The action to invoke if the peripheral is valid.</param>
        internal RequestEnumerator(
            RequestOperation operation,
            NativePeripheralHandle nativeHandle,
            float timeoutSec,
            Action<NativePeripheralHandle, NativeRequestResultCallback> action)
            : this(operation, nativeHandle, timeoutSec)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Operation = operation;
            _timeout = timeoutSec == 0 ? 0 : Time.realtimeSinceStartupAsDouble + timeoutSec;
            if (Peripheral.IsValid)
            {
                action?.Invoke(Peripheral, SetResult);
            }
            else
            {
                SetResult(RequestStatus.InvalidPeripheral);
                //TODO check in NativeInterface instead
            }
        }

        /// <summary>
        /// Initializes a request with a given operation, peripheral and timeout value.
        /// </summary>
        /// <param name="operation">The operation to run.</param>
        /// <param name="nativeHandle">The peripheral for which to run the operation.</param>
        /// <param name="timeoutSec">The timeout in seconds.</param>
        private protected RequestEnumerator(
            RequestOperation operation,
            NativePeripheralHandle nativeHandle,
            float timeoutSec)
        {
            Operation = operation;
            Peripheral = nativeHandle;
            _timeout = timeoutSec == 0 ? 0 : Time.realtimeSinceStartupAsDouble + timeoutSec;
        }

        /// <summary>
        /// Called to mark the request as done, with a given status.
        /// </summary>
        /// <param name="status">Request completion status.</param>
        private protected void SetResult(RequestStatus status)
        {
            // Only keep first error
            if (!_status.HasValue)
            {
                _status = status;
            }
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <returns>Indicates whether the request is still running.</returns>
        public virtual bool MoveNext()
        {
            if ((!_status.HasValue) && (_timeout > 0))
            {
                // Update timeout
                if (Time.realtimeSinceStartupAsDouble > _timeout)
                {
                    IsTimeout = true;
                    _status = RequestStatus.Timeout;
                }
            }

            return !_status.HasValue;
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public void Reset()
        {
        }
    }
}
