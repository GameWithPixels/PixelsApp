
using System;

namespace Systemic.Unity.Pixels
{
    /// <summary>
    /// Pixel dice Bluetooth Low Energy UUIDs.
    /// </summary>
    public class PixelUuids
    {
        /// <summary>
        /// Pixel service UUID.
        /// May be used to filter out Pixel dice during a scan and to access its characteristics.
        /// </summary>
        public static readonly Guid ServiceUuid = new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e");

        /// <summary>
        /// Pixel characteristic UUID for notification and read operations.
        /// May be used to get notified on dice events or read the current state.
        /// </summary>
        public static readonly Guid NotifyCharacteristicUuid = new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e");

        /// <summary>
        /// Pixel characteristic UUID for write operations.
        /// May be used to send messages to a dice.
        /// </summary>
        public static readonly Guid WriteCharacteristicUuid = new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
    }
}
