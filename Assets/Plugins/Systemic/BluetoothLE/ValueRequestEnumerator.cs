using System;

namespace Systemic.Unity.BluetoothLE
{
    /// <summary>
    /// Enumerator handling a request for reading a value from a BLE peripheral.
    /// Instances are meant to be run as coroutines.
    /// </summary>
    /// <typeparam name="T">Type of the value to be returned by the request.</typeparam>
    public class ValueRequestEnumerator<T> : RequestEnumerator
    {
        /// <summary>
        /// Gets the value retrieved by the request, only valid if <see cref="RequestEnumerator.IsSuccess"/> is true.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Initializes a value request enumerator with a given operation, peripheral, timeout value and
        /// an action to invoke if the peripheral is valid.
        /// </summary>
        /// <param name="operation">The operation to run.</param>
        /// <param name="nativeHandle">The peripheral for which to run the operation.</param>
        /// <param name="timeoutSec">The timeout in seconds.</param>
        /// <param name="action">The action to invoke if the peripheral is valid.</param>
        internal ValueRequestEnumerator(
            RequestOperation operation,
            NativePeripheralHandle nativeHandle,
            float timeoutSec,
            Action<NativePeripheralHandle, NativeValueRequestResultCallback<T>> action)
            : base(operation, nativeHandle, timeoutSec)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (Peripheral.IsValid)
            {
                action(Peripheral, (value, error) =>
                {
                    Value = value;
                    SetResult(error);
                });
            }
            else
            {
                SetResult(RequestStatus.InvalidPeripheral);
            }
        }
    }
}
