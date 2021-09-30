using System;
using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Android
{
    internal sealed class RequestCallback : AndroidJavaProxy
    {
        RequestOperation _operation;
        NativeRequestResultCallback _onResult;

        public RequestCallback(RequestOperation operation, NativeRequestResultCallback onResult)
            : base("com.systemic.bluetoothle.Peripheral$RequestCallback")
            => (_operation, _onResult) = (operation, onResult);

        void onRequestCompleted(AndroidJavaObject device)
        {
            Debug.Log($"[BLE] {_operation} ==> onRequestCompleted");
            _onResult?.Invoke(0); //RequestStatus.GATT_SUCCESS
        }

        void onRequestFailed(AndroidJavaObject device, int status)
        {
            Debug.LogError($"[BLE] {_operation} ==> onRequestFailed: {(AndroidRequestStatus)status}");
            _onResult?.Invoke(AndroidNativeInterfaceImpl.ToRequestStatus(status));
        }

        void onInvalidRequest()
        {
            Debug.LogError($"[BLE] {_operation} ==> onInvalidRequest");
            _onResult?.Invoke(RequestStatus.InvalidCall);
        }
    }
}
