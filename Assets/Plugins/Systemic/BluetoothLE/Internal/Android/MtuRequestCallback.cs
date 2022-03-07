using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Android
{
    internal sealed class MtuRequestCallback : AndroidJavaProxy
    {
        NativeValueRequestResultCallback<int> _onMtuResult;

        public MtuRequestCallback(NativeValueRequestResultCallback<int> onMtuResult)
            : base("com.systemic.bluetoothle.Peripheral$MtuRequestCallback")
            => _onMtuResult = onMtuResult;

        void onMtuChanged(AndroidJavaObject device, int mtu)
        {
            Debug.Log($"[BLE] {RequestOperation.RequestPeripheralMtu} ==> onMtuChanged: {mtu}");
            _onMtuResult?.Invoke(mtu, RequestStatus.Success);
        }

        void onRequestFailed(AndroidJavaObject device, int status)
        {
            Debug.LogError($"[BLE] {RequestOperation.RequestPeripheralMtu} ==> onRequestFailed: {(AndroidRequestStatus)status} ({status})");
            _onMtuResult?.Invoke(0, AndroidNativeInterfaceImpl.ToRequestStatus(status));
        }

        void onInvalidRequest()
        {
            Debug.LogError($"[BLE] {RequestOperation.RequestPeripheralMtu} ==> onInvalidRequest");
            _onMtuResult?.Invoke(0, RequestStatus.InvalidCall);
        }
    }
}
