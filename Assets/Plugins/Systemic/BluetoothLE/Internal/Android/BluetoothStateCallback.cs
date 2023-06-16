using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Android
{
    internal enum BluetoothState
    {
        Off, TurningOff, On, TurningOn,
    }

    internal sealed class BluetoothStateCallback : AndroidJavaProxy
    {
        // Values from Android documentation
        // https://developer.android.com/reference/android/bluetooth/BluetoothAdapter#STATE_ON
        const int STATE_OFF = 10;
        const int STATE_TURNING_ON = 11;
        const int STATE_ON = 12;
        const int STATE_TURNING_OFF = 13;

        System.Action<BluetoothState> _onStateChanged;

        public BluetoothStateCallback(System.Action<BluetoothState> onStateChanged)
            : base("com.systemic.bluetoothle.BluetoothState$StateCallback")
            => _onStateChanged = onStateChanged;

        void onStateChanged(int stateValue)
        {
            var state = ToState(stateValue);
            //Debug.Log($"[BLE] onStateChanged ==> {state?.ToString() ?? "Unknown"} ({stateValue})");
            if (state.HasValue)
            {
                _onStateChanged?.Invoke(state.Value);
            }
        }

        public static BluetoothState? ToState(int stateValue)
        {
            switch (stateValue)
            {
                case STATE_OFF:
                    return BluetoothState.Off;
                case STATE_TURNING_ON:
                    return BluetoothState.TurningOn;
                case STATE_ON:
                    return BluetoothState.On;
                case STATE_TURNING_OFF:
                    return BluetoothState.TurningOff;
            }
            return null;
        }
    }
}
