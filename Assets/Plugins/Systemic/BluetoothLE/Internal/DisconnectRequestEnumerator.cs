
using System;

namespace Systemic.Unity.BluetoothLE.Internal
{
    /// <summary>
    /// Enumerator handling a BLE disconnection request.
    /// </summary>
    internal sealed class DisconnectRequestEnumerator : RequestEnumerator
    {
        Action<NativePeripheralHandle> _onDone;

        public DisconnectRequestEnumerator(NativePeripheralHandle peripheral, Action<NativePeripheralHandle> onDone = null)
            : base(RequestOperation.DisconnectPeripheral, peripheral, 0)
        {
            _onDone = onDone;

            if (Peripheral.IsValid)
            {
                NativeInterface.DisconnectPeripheral(peripheral, SetResult);
            }
            else
            {
                SetResult(RequestStatus.InvalidPeripheral);
            }
        }

        public override bool MoveNext()
        {
            bool done = !base.MoveNext();

            // Are we done with the disconnect?
            if (done && Peripheral.IsValid)
            {
                _onDone?.Invoke(Peripheral);
                _onDone = null;
            }

            return !done;
        }
    }
}
