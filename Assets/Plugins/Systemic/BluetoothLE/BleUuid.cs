using System;

namespace Systemic.Unity.BluetoothLE
{
    /// <summary>
    /// Helper class for Bluetooth UUIDs
    /// </summary>
    public static class BleUuid
    {
        /// <summary>
        /// Converts a 16 bits Bluetooth LE UUID to a full 128 bit UUID.
        /// </summary>
        /// <param name="shortUuid">Short BLE UUID (16 bits).</param>
        /// <returns>A 128 bits UUID as a <see cref="Guid"/>.</returns>
        public static Guid ToFullUuid(short shortUuid) => new Guid($"0000{shortUuid:x4}-0000-1000-8000-00805f9b34fb");

        /// <summary>
        /// Converts a string representing a Bluetooth LE UUID (16 or 128 bits) to a <see cref="Guid"/>.
        /// </summary>
        /// <param name="bleUuid">String representing a BLE UUID (16 or 128 bits).</param>
        /// <returns>A 128 bit UUID as a <see cref="Guid"/>.</returns>
        public static Guid StringToGuid(string bleUuid)
        {
            if (bleUuid == null)
                throw new ArgumentNullException(nameof(bleUuid));
            if (bleUuid.Length == 36)
                return new Guid(bleUuid);
            if (bleUuid.Length <= 8)
                return new Guid($"{bleUuid.PadLeft(8, '0')}-0000-1000-8000-00805F9B34FB");

            throw new ArgumentException("Invalid BLE UUID string: " + bleUuid, nameof(bleUuid));
        }
    }
}
