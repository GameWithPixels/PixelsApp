using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Systemic.Unity.BluetoothLE.Internal;

namespace Systemic.Unity.BluetoothLE
{
    /// <summary>
    /// The advertisement data of a peripheral received during a BLE scan.
    ///
    /// This data usually holds the peripheral name and advertised services.
    /// The exact contents may vary depending on the platform.
    /// </summary>
    /// <remarks>
    /// This is a read only class.
    /// </remarks>
    public class ScannedPeripheral
    {
        /// <summary>
        /// Initializes a scanned peripheral instance with a given native device and the corresponding advertisement data.
        /// </summary>
        /// <param name="nativeDevice">The underlying native device of the scanned peripheral.</param>
        /// <param name="advertisementData">The advertisement data received from the device.</param>
        internal ScannedPeripheral(INativeDevice nativeDevice, NativeAdvertisementDataJson advertisementData)
        {
            if (nativeDevice == null) throw new ArgumentNullException(nameof(nativeDevice));
            if (!nativeDevice.IsValid) throw new ArgumentException("Invalid native device", nameof(nativeDevice));
            if (advertisementData == null) throw new ArgumentNullException(nameof(advertisementData));

            NativeDevice = nativeDevice;
            SystemId = advertisementData.systemId;
            BluetoothAddress = advertisementData.address;
            Name = advertisementData.name;
            IsConnectable = advertisementData.isConnectable;
            Rssi = advertisementData.rssi;
            TxPowerLevel = advertisementData.txPowerLevel;
            ManufacturerData = Array.AsReadOnly((advertisementData.manufacturerData ?? Array.Empty<byte>()).ToArray());
            ServicesData = new ReadOnlyDictionary<string, byte[]>(CloneDictionary(advertisementData.servicesData));
            Services = Array.AsReadOnly(ToGuidArray(advertisementData.services));
            OverflowServices = Array.AsReadOnly(ToGuidArray(advertisementData.overflowServiceUUIDs));
            SolicitedServices = Array.AsReadOnly(ToGuidArray(advertisementData.solicitedServiceUUIDs));
        }

        /// <summary>
        /// Gets the underlying native device.
        /// </summary>
        internal INativeDevice NativeDevice { get; }

        /// <summary>
        /// Gets the unique id assigned to the peripheral (platform dependent).
        /// </summary>
        public string SystemId { get; }

        /// <summary>
        /// Gets the bluetooth address of the peripheral (not available on iOS).
        /// </summary>
        public ulong BluetoothAddress { get; }

        /// <summary>
        /// Gets the name of the peripheral.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Indicates whether the peripheral is connectable (at the time of this advertisement).
        /// </summary>
        public bool IsConnectable { get; }

        /// <summary>
        /// Gets the Received Signal Strength Indicator (RSSI) for the peripheral when this advertisement data was received.
        /// </summary>
        public int Rssi { get; }

        /// <summary>
        /// Gets the received transmit power of the advertisement.
        /// </summary>
        public int TxPowerLevel { get; }

        /// <summary>
        /// Gets the manufacturer data of the peripheral.
        /// </summary>
        public IReadOnlyList<byte> ManufacturerData { get; }

        /// <summary>
        /// Gets the service-specific advertisement data (iOS only).
        /// </summary>
        public IReadOnlyDictionary<string, byte[]> ServicesData { get; }

        /// <summary>
        /// Gets the list of services advertised by the peripheral.
        /// </summary>
        public IReadOnlyList<Guid> Services { get; }

        /// <summary>
        /// Gets an array of the UUIDs found in the overflow area of the advertisement data (iOS only).
        /// </summary>
        public IReadOnlyList<Guid> OverflowServices { get; }

        /// <summary>
        /// Gets an array of the solicited service UUIDs (iOS only).
        /// </summary>
        public IReadOnlyList<Guid> SolicitedServices { get; }

        // Converts an of string representing BLE UUIDS to an array of Guid
        private static Guid[] ToGuidArray(string[] uuids)
        {
            return uuids?.Select(BleUuid.StringToGuid).ToArray() ?? Array.Empty<Guid>();
        }

        // Duplicates the given dictionary and its contents
        private static IDictionary<string, byte[]> CloneDictionary(IDictionary<string, byte[]> servicesData)
        {
            var clone = new Dictionary<string, byte[]>(servicesData?.Count ?? 0);
            if (servicesData != null)
            {
                foreach (var kv in servicesData)
                {
                    clone.Add(kv.Key, (byte[])kv.Value.Clone());
                }
            }
            return clone;
        }
    }
}
