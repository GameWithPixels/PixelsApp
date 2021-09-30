using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Android
{
    internal sealed class DataReceivedCallback : AndroidJavaProxy
    {
        NativeValueRequestResultCallback<byte[]> _onDataReceived;

        public DataReceivedCallback(NativeValueRequestResultCallback<byte[]> onDataReceived)
            : base("no.nordicsemi.android.ble.callback.DataReceivedCallback")
            => _onDataReceived = onDataReceived;

        void onDataReceived(AndroidJavaObject device, AndroidJavaObject data)
        {
            //Debug.Log($"[BLE] {RequestOperation.SubscribeCharacteristic} ==> onDataReceived");
            using var javaArray = data.Call<AndroidJavaObject>("getValue");
            _onDataReceived?.Invoke(JavaUtils.ToDotNetArray(javaArray), RequestStatus.Success); // No notification with error on Android
        }
    }
}
