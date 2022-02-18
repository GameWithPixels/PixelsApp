using System.Collections.Generic;

namespace Systemic.Unity.BluetoothLE.Internal
{
    /// <summary>
    /// Represents the advertisement data in JSON format as send by the native BLE implementation.
    /// </summary>
    internal sealed class NativeAdvertisementDataJson
    {
        public string systemId = default;
        public ulong address = default;
        public string name = default;
        public bool isConnectable = default;
        public int rssi = default;
        public int txPowerLevel = default;
        public byte[] manufacturerData0 = default; // JsonUtility doesn't support nested arrays unfortunately,
        public byte[] manufacturerData1 = default; // we have room for 4 entries of manufacturerData
        public byte[] manufacturerData2 = default;
        public byte[] manufacturerData3 = default;
        public Dictionary<string, byte[]> servicesData = default;
        public string[] services = default;
        public string[] overflowServiceUUIDs = default;
        public string[] solicitedServiceUUIDs = default;
    }
}
