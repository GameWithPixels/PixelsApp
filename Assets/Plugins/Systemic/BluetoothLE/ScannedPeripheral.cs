using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Systemic.Unity.BluetoothLE.Internal;

namespace Systemic.Unity.BluetoothLE
{
    /// <summary>
    /// The manufacturer data of an advertisement packet.
    /// </summary>
    /// <remarks>
    /// This is a read only class.
    /// </remarks>
    public struct ManufacturerData
    {
        /// <summary>
        /// Initialize a manufacturer data instance from an array of bytes.
        /// </summary>
        /// <param name="data">The manufacturer data, should be at least of size 2.</param>
        internal ManufacturerData(byte[] data)
        {
            if (data?.Length > 2)
            {
                ManufacturerId = (ushort)(data[0] | (data[1] << 8));
                Data = Array.AsReadOnly(data.Skip(2).ToArray());
            }
            else
            {
                ManufacturerId = 0;
                Data = Array.AsReadOnly(Array.Empty<byte>());
            }
        }

        /// <summary>
        /// The manufacturer id.
        /// </summary>
        public ushort ManufacturerId { get; }

        /// <summary>
        /// The data for the manufacturer.
        /// </summary>
        public IReadOnlyList<byte> Data { get; }
    }

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
            ManufacturerData = Array.AsReadOnly(ToManufacturerDataArray(advertisementData.manufacturerData0,
                advertisementData.manufacturerData1, advertisementData.manufacturerData2, advertisementData.manufacturerData3));
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
        public IReadOnlyList<ManufacturerData> ManufacturerData { get; }

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

        // Converts a double array of bytes to an array of manufacturer data
        private ManufacturerData[] ToManufacturerDataArray(byte[] data0, byte[] data1, byte[] data2, byte[] data3)
        {
            return new[] { data0, data1, data2, data3 }.Where(d => d != null).Select(d => new ManufacturerData(d)).ToArray();
        }

        // Converts a list of strings representing BLE UUIDS to an array of Guids
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
