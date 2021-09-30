using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Android
{
    internal sealed class ReadValueRequestCallback : AndroidJavaProxy
    {
        NativeValueRequestResultCallback<byte[]> _onValueRead;

        public ReadValueRequestCallback(NativeValueRequestResultCallback<byte[]> onValueRead)
            : base("com.systemic.bluetoothle.Peripheral$ReadValueRequestCallback")
            => _onValueRead = onValueRead;

        void onDataReceived(AndroidJavaObject device, AndroidJavaObject data)
        {
            Debug.Log($"[BLE] {RequestOperation.ReadCharacteristic} ==> onDataReceived");
            using var javaArray = data.Call<AndroidJavaObject>("getValue");
            _onValueRead?.Invoke(JavaUtils.ToDotNetArray(javaArray), RequestStatus.Success);
        }

        void onRequestFailed(AndroidJavaObject device, int status)
        {
            Debug.LogError($"[BLE] {RequestOperation.ReadCharacteristic} ==> onRequestFailed: {(AndroidRequestStatus)status}");
            _onValueRead?.Invoke(null, AndroidNativeInterfaceImpl.ToRequestStatus(status));
        }

        void onInvalidRequest()
        {
            Debug.LogError($"[BLE] {RequestOperation.ReadCharacteristic} ==> onInvalidRequest");
            _onValueRead?.Invoke(null, RequestStatus.InvalidCall);
        }
    }
}
