package com.systemic.bluetoothle;

import java.lang.StringBuilder;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import android.os.ParcelUuid;
import android.util.Log;
import android.util.SparseArray;
import android.bluetooth.BluetoothDevice;

import no.nordicsemi.android.support.v18.scanner.*;

/**
 * @brief Static class with methods for running a Bluetooth Low Energy (BLE) scan.
 *
 * @note This class was designed to work in a Unity plugin and its marshaling
 *       mechanism, and as such the advertisement data returned by a scan is passed
 *       as JSON string rather than a complex object.
 *
 * It relies on Nordic's Android-Scanner-Compat-Library library for most of the work.
 * @see https://github.com/NordicSemiconductor/Android-Scanner-Compat-Library
 */
public final class Scanner
{
    /**
     * @brief Interface for scan results callbacks.
     */
    public interface ScannerCallback
    {
        /**
         * @brief A callback invoked when an advertisement packet is received
         *        from a Bluetooth device.
         *
         * @param device The Android BluetoothDevice which send the advertisement data.
         * @param advertisementDataJson The Android BluetoothDevice which send the advertisement data.
         */
        public void onScanResult(BluetoothDevice device, String advertisementDataJson);

        /**
         * @brief A callback invoked when the scan fails.
         *
         * @param error A string with the error that caused the scan to fail.
         */
        public void onScanFailed(String error);
    }

	private static String TAG = "systemic";
    private static ScanCallback _scanCallback;
    private static Object _scanSync = new Object();

    /**
     * @brief Starts scanning for BLE peripherals advertising the given list of services.
     *
     * If a scan is already running, it is stopped before starting the new one.
     *
     * @param requiredServicesUuids Comma separated list of services UUIDs that the peripheral
     *                              should advertise, may be null or empty.
     * @param callback The callback for notifying of the scan results (called for each advertisement packet).
     */
    public static void startScan(final String requiredServicesUuids, final ScannerCallback callback)
    {
        Log.v(TAG, "==> startScan");

        if (callback == null)
        {
            throw new IllegalArgumentException("callback is null");
        }

        // Build scan settings
        ScanSettings settings = new ScanSettings.Builder()
            .setLegacy(false) // Default is true for compatibility with older apps, but we all type of advertisements, not just legacy
            .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY) // Default is low power which is good for long scans, in our use case we do short scans and we prefer having quick results
            .build(); // Other defaults are great for us

        // Convert the comma separated list of UUIDs
        List<ScanFilter> filters = null;
        if (requiredServicesUuids != null)
        {
            filters = new ArrayList<>();
            for (String uuidStr : requiredServicesUuids.split(","))
            {
                try
                {
                    ParcelUuid uuid = ParcelUuid.fromString(uuidStr);
                    filters.add(new ScanFilter.Builder().setServiceUuid(uuid).build());
                }
                catch (IllegalArgumentException e)
                {
                    throw new IllegalArgumentException("requiredServicesUuids must be either null, an empty string or a comma separated list of UUIDs");
                }
            }
        }

        synchronized (_scanSync)
        {
            // Only one scan at a time
            if (_scanCallback != null)
            {
    	        BluetoothLeScannerCompat.getScanner().stopScan(_scanCallback);
            }

            // Start scanning
            _scanCallback = createCallback(callback);
            BluetoothLeScannerCompat.getScanner().startScan(filters, settings, _scanCallback);
        }
    }

    /**
     * @brief Stops an on-going BLE scan.
     */
	public static void stopScan()
    {
        Log.v(TAG, "==> stopScan");

        synchronized (_scanSync)
        {
            if (_scanCallback != null)
            {
    	        BluetoothLeScannerCompat.getScanner().stopScan(_scanCallback);
                _scanCallback = null;
            }
        }
    }

    /**
     * @brief Gets a ScanCallback instance that notify scan results to user code.
     */
    private static ScanCallback createCallback(final ScannerCallback callback)
    {
        return new ScanCallback()
        {
            @Override
            public void onScanResult(final int callbackType, final ScanResult result)
            {
                NotifyScanResult(result);
            }

            @Override
            public void onBatchScanResults(final List<ScanResult> results)
            {
                for (ScanResult scan : results)
                {
                    NotifyScanResult(scan);
                }
            }

            @Override
        	public void onScanFailed(final int errorCode)
            {
                callback.onScanFailed(errorToString(errorCode));
            }

            private String errorToString(final int errorCode)
            {
                switch (errorCode)
                {
                    case ScanCallback.SCAN_FAILED_ALREADY_STARTED:
                        return "Already started";
                    case ScanCallback.SCAN_FAILED_APPLICATION_REGISTRATION_FAILED:
                        return "Application registration failed";
                    case ScanCallback.SCAN_FAILED_INTERNAL_ERROR:
                        return "Internal error";
                    case ScanCallback.SCAN_FAILED_FEATURE_UNSUPPORTED:
                        return "Feature unsupported";
                    case ScanCallback.SCAN_FAILED_OUT_OF_HARDWARE_RESOURCES:
                        return "Out of hardware resources";
                }
                return "Unknown error";
            }

            private void NotifyScanResult(final ScanResult scanResult)
            {
                BluetoothDevice device = scanResult.getDevice();
                if (device != null)
                {
                    long address = 0, shift = 0;
                    String[] octets = device.getAddress().split(":");
                    for (int i = octets.length - 1; i >= 0; --i)
                    {
                        address += (long)Integer.parseInt(octets[i], 16) << shift;
                        shift += 8;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.append("{\"systemId\":\"");
                    sb.append(device.hashCode());
                    sb.append("\",\"address\":");
                    sb.append(address);
                    sb.append(",\"name\":\"");
                    sb.append(device.getName());
                    sb.append("\",\"isConnectable\":");
                    sb.append(scanResult.isConnectable());
                    sb.append(",\"rssi\":");
                    sb.append(scanResult.getRssi());
                    sb.append(",\"txPowerLevel\":");
                    sb.append(scanResult.getTxPower());

                    ScanRecord scanRecord = scanResult.getScanRecord();
                    if (scanRecord != null)
                    {
                        // Services
                        List<ParcelUuid> services = scanRecord.getServiceUuids();
                        if ((services != null) && (services.size() > 0))
                        {
                            sb.append(",\"services\":[");
                            for (int i = 0; i < services.size(); ++i)
                            {
                                if (i > 0) sb.append(",");
                                sb.append("\"");
                                sb.append(services.get(i));
                                sb.append("\"");
                            }
                            sb.append("]");
                        }

                        // Added in API level 29
                        // Solicited services
                        // List<ParcelUuid> solicitedServices = scanRecord.getServiceSolicitationUuids();
                        // if ((solicitedServices != null) && (solicitedServices.size() > 0))
                        // {
                        //     sb.append(",\"solicitedServices\":[");
                        //     for (int i = 0; i < solicitedServices.size(); ++i)
                        //     {
                        //         if (i > 0) sb.append(",");
                        //         sb.append("\"");
                        //         sb.append(solicitedServices.get(i));
                        //         sb.append("\"");
                        //     }
                        //     sb.append("]");
                        // }

                        // Manufacturer data
                        SparseArray<byte[]> manufacturersData = scanRecord.getManufacturerSpecificData();
                        if ((manufacturersData != null) && (manufacturersData.size() > 0))
                        {
                            sb.append(",\"manufacturersData\":[");
                            for (int i = 0; i < manufacturersData.size(); ++i)
                            {
                                if (i > 0) sb.append(",");
                                sb.append("{\"companyId\":");
                                sb.append(manufacturersData.keyAt(i));
                                sb.append(",\"data\":[");
                                byte[] data = manufacturersData.valueAt(i);
                                for (int j = 0; j < data.length; ++j)
                                {
                                    if (j > 0) sb.append(",");
                                    sb.append(data[j]);
                                }
                                sb.append("]}");
                            }
                            sb.append("]");
                        }

                        // Service data
                        Map<ParcelUuid, byte[]> servicesData = scanRecord.getServiceData();
                        if ((servicesData != null) && (servicesData.size() > 0))
                        {
                            sb.append(",\"servicesData\":[");
                            boolean first = true;
                            for (Map.Entry<ParcelUuid, byte[]> entry : servicesData.entrySet())
                            {
                                if (!first) sb.append(",");
                                first = false;

                                sb.append("{\"uuid\":\"");
                                sb.append(entry.getKey());
                                sb.append("\",\"data\":[");
                                byte[] data = entry.getValue();
                                for (int j = 0; j < data.length; ++j)
                                {
                                    if (j > 0) sb.append(",");
                                    sb.append(data[j]);
                                }
                                sb.append("]}");
                            }
                            sb.append("]");
                        }
                    }
                    sb.append("}");

                    callback.onScanResult(device, sb.toString());
                }
            }
        };
    }
}
