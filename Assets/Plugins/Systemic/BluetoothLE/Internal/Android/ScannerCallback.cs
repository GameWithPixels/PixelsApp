using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Android
{
    internal delegate void ScanResultCallback(AndroidJavaObject device, NativeAdvertisementDataJson advertisementData);

    internal sealed class ScannerCallback : AndroidJavaProxy
    {
        ScanResultCallback _onScanResult;

        public ScannerCallback(ScanResultCallback onScanResult)
            : base("com.systemic.bluetoothle.Scanner$ScannerCallback")
            => _onScanResult = onScanResult;

        void onScanResult(AndroidJavaObject device, string advertisementDataJson)
        {
            //Debug.Log($"[BLE] Scan ==> onScanResult: {advertisementDataJson}");

            _onScanResult?.Invoke(device, JsonUtility.FromJson<NativeAdvertisementDataJson>(advertisementDataJson));
        }

        void onScanFailed(string error)
        {
            Debug.Log($"[BLE] Scan ==> onScanFailed: {error}");
        }
    }
}