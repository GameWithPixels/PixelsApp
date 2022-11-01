using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Systemic.Unity.BluetoothLE.Internal;

namespace Systemic.Unity.BluetoothLE
{
    /// <summary>
    /// Represents the manufacturer data of an advertisement packet.
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
        internal ManufacturerData(ushort companyId, byte[] data = null)
        {
            CompanyId = companyId;
            Data = Array.AsReadOnly(data ?? Array.Empty<byte>());
        }

        /// <summary>
        /// The company assigned id.
        /// </summary>
        public ushort CompanyId { get; }

        /// <summary>
        /// The data for the manufacturer.
        /// </summary>
        public IReadOnlyList<byte> Data { get; }
    }

    /// <summary>
    /// Represents the service data of an advertisement packet.
    /// </summary>
    /// <remarks>
    /// This is a read only class.
    /// </remarks>
    public struct ServiceData
    {
        /// <summary>
        /// Initialize a manufacturer data instance from an array of bytes.
        /// </summary>
        /// <param name="data">The manufacturer data, should be at least of size 2.</param>
        internal ServiceData(Guid uuid, byte[] data = null)
        {
            Uuid = uuid;
            Data = Array.AsReadOnly(data ?? Array.Empty<byte>());
        }

        /// <summary>
        /// The service UUID.
        /// </summary>
        public Guid Uuid { get; }

        /// <summary>
        /// The custom data for the service.
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
            ManufacturersData = Array.AsReadOnly(ToManufacturerDataArray(advertisementData.manufacturersData));
            ServicesData = Array.AsReadOnly(ToServiceDataArray(advertisementData.servicesData));
            Services = Array.AsReadOnly(ToGuidArray(advertisementData.services));
            OverflowServices = Array.AsReadOnly(ToGuidArray(advertisementData.overflowServices));
            SolicitedServices = Array.AsReadOnly(ToGuidArray(advertisementData.solicitedServices));
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
        /// Gets the Bluetooth address of the peripheral (not available on iOS).
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
        /// Gets the manufacturers data of the peripheral.
        /// </summary>
        public IReadOnlyList<ManufacturerData> ManufacturersData { get; }

        /// <summary>
        /// Gets the advertised services data.
        /// </summary>
        public IReadOnlyList<ServiceData> ServicesData { get; }

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

        // Converts an array of JSON manufacturer data to an array of manufacturer data
        private ManufacturerData[] ToManufacturerDataArray(NativeAdvertisementDataJson.ManufacturerData[] arr)
        {
            return arr?.Select(d => new ManufacturerData(d.companyId, d.data)).ToArray() ?? Array.Empty<ManufacturerData>();
        }

        // Converts an array of JSON service data to an array of service data
        private ServiceData[] ToServiceDataArray(NativeAdvertisementDataJson.ServiceData[] arr)
        {
            return arr?.Select(d => new ServiceData(new Guid(d.uuid), d.data)).ToArray() ?? Array.Empty<ServiceData>();
        }

        // Converts a list of strings representing BLE UUIDS to an array of Guids
        private static Guid[] ToGuidArray(string[] uuids)
        {
            return uuids?.Select(BleUuid.StringToGuid).ToArray() ?? Array.Empty<Guid>();
        }
    }
}
