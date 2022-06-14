using System.Collections.Generic;

namespace Systemic.Unity.BluetoothLE.Internal
{
    /// <summary>
    /// Represents the advertisement data in JSON format as send by the native BLE implementation.
    /// </summary>
    [System.Serializable]
    internal sealed class NativeAdvertisementDataJson
    {
        [System.Serializable]
        internal sealed class ManufacturerData
        {
            public ushort companyId = default;
            public byte[] data = default;
        }

        [System.Serializable]
        internal sealed class ServiceData
        {
            public string uuid = default;
            public byte[] data = default;
        }

        public string systemId = default;
        public ulong address = default;
        public string name = default;
        public bool isConnectable = default;
        public int rssi = default;
        public int txPowerLevel = default;
        public ManufacturerData[] manufacturersData = default;
        public ServiceData[] servicesData = default;
        public string[] services = default;
        public string[] overflowServices = default;
        public string[] solicitedServices = default;
    }
}
