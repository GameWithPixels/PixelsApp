using System;

namespace Systemic.Unity.BluetoothLE.Internal
{
    /// <summary>
    /// Enumerator handling a BLE connection request.
    /// </summary>
    internal sealed class ConnectRequestEnumerator : RequestEnumerator
    {
        DisconnectRequestEnumerator _disconnect;
        Action _onTimeoutDisconnect;

        public ConnectRequestEnumerator(
            NativePeripheralHandle peripheral,
            float timeoutSec,
            Action<NativePeripheralHandle, NativeRequestResultCallback> action,
            Action onTimeoutDisconnect)
            : base(RequestOperation.ConnectPeripheral, peripheral, timeoutSec, action)
        {
            _onTimeoutDisconnect = onTimeoutDisconnect;
        }

        public override bool MoveNext()
        {
            bool done;

            if (_disconnect == null)
            {
                done = !base.MoveNext();

                // Did we fail with a timeout?
                if (done && IsTimeout && Peripheral.IsValid)
                {
                    _onTimeoutDisconnect?.Invoke();

                    // Cancel connection attempt
                    _disconnect = new DisconnectRequestEnumerator(Peripheral); //TODO we should already be disconnected!!
                    done = !_disconnect.MoveNext();
                }
            }
            else
            {
                done = !_disconnect.MoveNext();
            }

            return !done;
        }
    }
}
