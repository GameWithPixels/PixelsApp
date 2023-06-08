// Ignore Spelling: Rssi

using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Android
{
    internal sealed class ReadRssiRequestCallback : AndroidJavaProxy
    {
        NativeValueRequestResultCallback<int> _onRssiRead;

        public ReadRssiRequestCallback(NativeValueRequestResultCallback<int> onRssiRead)
            : base("com.systemic.bluetoothle.Peripheral$RssiRequestCallback")
            => _onRssiRead = onRssiRead;

        // @IntRange(from = -128, to = 20)
        void onRssiRead(AndroidJavaObject device, int rssi)
        {
            Debug.Log($"[BLE] {RequestOperation.ReadPeripheralRssi} ==> onRssiRead {rssi}");
            _onRssiRead?.Invoke(rssi, RequestStatus.Success);
        }

        void onRequestFailed(AndroidJavaObject device, int status)
        {
            Debug.LogError($"[BLE] {RequestOperation.ReadPeripheralRssi} ==> onRequestFailed: {(AndroidRequestStatus)status} ({status})");
            _onRssiRead?.Invoke(int.MinValue, AndroidNativeInterfaceImpl.ToRequestStatus(status));
        }

        void onInvalidRequest()
        {
            Debug.LogError($"[BLE] {RequestOperation.ReadPeripheralRssi} ==> onInvalidRequest");
            _onRssiRead?.Invoke(int.MinValue, RequestStatus.InvalidCall);
        }
    }
}
