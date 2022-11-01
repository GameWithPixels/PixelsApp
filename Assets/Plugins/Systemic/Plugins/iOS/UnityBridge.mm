/**
 * @file
 * @brief C library for discovering, connecting to, and interacting with Bluetooth Low Energy
 * (BLE) peripherals on iOS.
 *
 * @attention None of those functions are thread safe.
 */

#import "UnitBridge.h"

namespace internal
{
    // Our central manager instance
    static SGBleCentralManagerDelegate *_central = nil;

    // Maps a CBPeripheral to a SGBlePeripheralQueue
    static NSMutableDictionary<CBPeripheral *, SGBlePeripheralQueue *> *_peripherals = nil;

    SGBleCentralManagerDelegate *getCentral()
    {
        return _central;
    }

    NSMutableDictionary<CBPeripheral *, SGBlePeripheralQueue *> *getPeripherals()
    {
        return _peripherals;
    }
}

using namespace internal;

extern "C"
{

//! \name Library life cycle
//! @{

/**
 * @brief Initializes the library for accessing BLE peripherals.
 *
 * @param onBluetoothEvent Called when the host device Bluetooth state changes.
 * @return Whether the initialization was successful.
 */
bool sgBleInitialize(BluetoothStateUpdateCallback onBluetoothEvent)
{
    if (!_peripherals)
    {
        // Allocate just once
        _peripherals = [NSMutableDictionary<CBPeripheral *, SGBlePeripheralQueue *> new];
    }
    if (!_central)
    {
        // Allocate every time (set to nil in shutdown)
        _central = [[SGBleCentralManagerDelegate alloc] initWithStateUpdateHandler:^(CBManagerState state) {
            if (onBluetoothEvent) onBluetoothEvent(state >= CBManagerStatePoweredOn);
        }];
    }
    
    return _peripherals && _central;
}

/**
 * @brief Shuts down the library.
 *
 * Scanning is stopped and all peripherals are disconnected and removed.
 */
void sgBleShutdown()
{
    if (_central)
    {
        [_peripherals removeAllObjects];
        [_central clearPeripherals];
        _central = nil;
    }
}

//! @}
//! \name Peripherals scanning
//! @{

/**
 * @brief Starts scanning for BLE peripherals advertising the given list of services.
 *
 * If a scan is already running, it is updated to run with the new parameters.
 *
 * @param requiredServicesUuids Comma separated list of services UUIDs that the peripheral
 *                              should advertise, may be null or empty.
 * @param allowDuplicates If <c>false</c>, let the system coalesces multiple discoveries of the same peripheral
                          into a single discovery event which preserves battery life. <br>
                          If <c>true</c>, generates a discovery event each time it receives an advertising
                          packet from the peripheral
 * @param onDiscoveredPeripheral Called every time an advertisement packet with the required
 *                               services is received. <br>
 *                               The advertisement data is passed as a JSON string.
 *                               The callback must stay valid until the scan is stopped.
 * @return Whether the scan was successfully started.
 */
bool sgBleStartScan(const char *requiredServicesUuids,
                    bool allowDuplicates,
                    DiscoveredPeripheralCallback onDiscoveredPeripheral)
{
    if (!onDiscoveredPeripheral)
    {
        return false;
    }
    
    _central.peripheralDiscoveryHandler = ^(CBPeripheral *peripheral, NSDictionary<NSString *,id> *advertisementData, NSNumber *RSSI){
        onDiscoveredPeripheral([advertisementDataToJsonString(getPeripheralId(peripheral), advertisementData, RSSI) UTF8String]);
    };
    
    // If already scanning, update the existing scan
    [_central.centralManager scanForPeripheralsWithServices:toCBUUIDArray(requiredServicesUuids)
                                                    options:allowDuplicates ? @{ CBCentralManagerScanOptionAllowDuplicatesKey: @YES } : nil];
    
    return _central != nil;
}

/**
 * @brief Stops an on-going BLE scan.
 */
void sgBleStopScan()
{
    [_central.centralManager stopScan];
}

//! @}
//! \name Peripherals life cycle
//! @{

/**
 * @brief Creates a Peripheral for the BLE peripheral with the given Bluetooth address.
 *
 * The underlying object is not returned, instead the peripheral must be referenced by
 * its peripheral id. Call sgBleReleasePeripheral() to destroy the object.
 *
 * A scan must first be run to discover available BLE peripherals through their advertisement data.
 * The later includes the peripheral system id, a UUID assigned by the system.
 *
 * @note The the peripheral system id may change over long period of time.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @param onPeripheralConnectionEvent Called when the peripheral connection state changes. <br>
 *                                    The callback must stay valid until the peripheral is released.
 * @param requestIndex The index of this request, passed back when calling @p onPeripheralConnectionEvent.
 * @return Whether the peripheral object was successfully created.
 */
bool sgBleCreatePeripheral(peripheral_id_t peripheralId,
                           PeripheralConnectionEventCallback onPeripheralConnectionEvent,
                           request_index_t requestIndex)
{
    if (getSGBlePeripheralQueue(peripheralId))
    {
        // Already created
        return false;
    }
    
    CBPeripheral *cbPeripheral = getCBPeripheral(peripheralId);
    if (!cbPeripheral)
    {
        // No known peripheral for this id
        return false;
    }
    
    // Creates our peripheral object
    SGBlePeripheralQueue *sgPeripheral = [[SGBlePeripheralQueue alloc] initWithPeripheral:cbPeripheral
                                                         centralManagerDelegate:_central
                                                         connectionEventHandler:^(SGBleConnectionEvent connectionEvent, SGBleConnectionEventReason reason){
        //TODO check valid peripheral
        if (onPeripheralConnectionEvent)
            onPeripheralConnectionEvent(requestIndex,
                                        getPeripheralId(cbPeripheral),
                                        (int)connectionEvent,
                                        (int)reason);
    }];

    // And store it
    if (sgPeripheral)
    {
        [_peripherals setObject:sgPeripheral forKey:cbPeripheral];
    }
    return sgPeripheral != nil;
}

/**
 * @brief Releases the Peripheral object associated with the given peripheral id.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 */
void sgBleReleasePeripheral(peripheral_id_t peripheralId)
{
    CBPeripheral *cbPeripheral = getCBPeripheral(peripheralId);
    [_peripherals removeObjectForKey:cbPeripheral];
}

//! @}
//! \name Peripheral connection and disconnection
//! @{

/**
 * @brief Connects to the given peripheral.
 *
 * This request never timeouts.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @param requiredServicesUuids Comma separated list of services UUIDs that the peripheral
 *                              should support, may be null or empty.
 * @param onRequestStatus Called when the request has completed (successfully or not).
 * @param requestIndex The index of this request, passed back when calling @p onRequestStatus.
 */
void sgBleConnectPeripheral(peripheral_id_t peripheralId,
                            const char* requiredServicesUuids,
                            RequestStatusCallback onRequestStatus,
                            request_index_t requestIndex)
{
    SGBlePeripheralQueue *sgPeripheral = getSGBlePeripheralQueue(peripheralId, onRequestStatus, requestIndex);
    [sgPeripheral queueConnectWithServices:toCBUUIDArray(requiredServicesUuids)
                         completionHandler:toCompletionHandler(onRequestStatus, requestIndex)];
}

/**
 * @brief Disconnects the given peripheral.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @param onRequestStatus Called when the request has completed (successfully or not).
 * @param requestIndex The index of this request, passed back when calling @p onRequestStatus.
 */
void sgBleDisconnectPeripheral(peripheral_id_t peripheralId,
                               RequestStatusCallback onRequestStatus,
                               request_index_t requestIndex)
{
    SGBlePeripheralQueue *peripheral = getSGBlePeripheralQueue(peripheralId, onRequestStatus, requestIndex);
    [peripheral cancelQueue];
    [peripheral queueDisconnect:toCompletionHandler(onRequestStatus, requestIndex)];
}

//! @}
//! \name Peripheral operations
//! Valid only for connected peripherals.
//! @{

/**
 * @brief Gets the name of the given peripheral.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @return The name of the peripheral, or null if the call failed.
 *
 * @remark The caller should free the returned string with a call to free(). <br>
 *         Unity marshaling takes care of it.
 */
const char* sgBleGetPeripheralName(peripheral_id_t peripheralId)
{
    CBPeripheral *cbPeripheral = getCBPeripheral(peripheralId);
    return allocateCStr(cbPeripheral.name);
}

/**
 * @brief Gets the Maximum Transmission Unit (MTU) for the given peripheral.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @return The MTU of the peripheral, or zero if the call failed. 
 */
int sgBleGetPeripheralMtu(peripheral_id_t peripheralId)
{
    CBPeripheral *cbPeripheral = getCBPeripheral(peripheralId);
    // Return the smallest MTU since we don't differentiate the 2 values
    int mtu1 = (int)[cbPeripheral maximumWriteValueLengthForType:CBCharacteristicWriteWithResponse];
    int mtu2 = (int)[cbPeripheral maximumWriteValueLengthForType:CBCharacteristicWriteWithoutResponse];
    return mtu1 <= mtu2 ? mtu1 : mtu2;
}

/**
 * @brief Reads the Received Signal Strength Indicator (RSSI) of the given peripheral.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @param onRssiRead Called with the read RSSI.
 * @param requestIndex The index of this request, passed back when calling @p onRssiRead.
 */
void sgBleReadPeripheralRssi(peripheral_id_t peripheralId,
                             RssiReadCallback onRssiRead,
                             request_index_t requestIndex)
{
    SGBlePeripheralQueue *peripheral = getSGBlePeripheralQueue(peripheralId, onRssiRead, requestIndex);
    [peripheral queueReadRssi:^(NSError *error) {
        if (onRssiRead)
            onRssiRead(requestIndex, error ? std::numeric_limits<int>::min() : peripheral.rssi, toErrorCode(error));
    }];
}

//! @}
//! \name Services operations
//! Valid only for ready peripherals.
//! @{

/**
 * @brief Gets the list of discovered services for the given peripheral.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @return A comma separated list of services UUIDs, or null if the call failed.
 *
 * @remark The caller should free the returned string with a call to free(). <br>
 *         Unity marshaling takes care of it.
 */
const char *sgBleGetDiscoveredServices(peripheral_id_t peripheralId)
{
    CBPeripheral *peripheral = getCBPeripheral(peripheralId);
    return allocateCStr(toUuidsString(peripheral.services));
}

/**
 * @brief Gets the list of discovered characteristics for the given peripheral's service.
 *
 * The same characteristic may be listed several times according to the peripheral's configuration.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @param serviceUuid The service UUID for which to retrieve the characteristics.
 * @return A comma separated list of characteristics UUIDs, or null if the call failed.
 *
 * @remark The caller should free the returned string with a call to free(). <br>
 *         Unity marshaling takes care of it.
 */
const char *sgBleGetServiceCharacteristics(peripheral_id_t peripheralId,
                                           const char* serviceUuid)
{
    CBService *service = getService(peripheralId, serviceUuid);
    return allocateCStr(toUuidsString(service.characteristics));
}

//! @}
//! \name Characteristics operations
//! Valid only for connected peripherals.
//! @{

/**
 * @brief Gets the standard BLE properties of the specified service's characteristic
 *        for the given peripheral.
 *
 * @see https://developer.apple.com/documentation/corebluetooth/cbcharacteristicproperties?language=objc
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @param serviceUuid The service UUID.
 * @param characteristicUuid The characteristic UUID.
 * @param instanceIndex The instance index of the characteristic if listed more than once
 *                      for the service, otherwise zero.
 * @return The standard BLE properties of a service's characteristic, or zero if the call failed.
 */
characteristic_property_t sgBleGetCharacteristicProperties(peripheral_id_t peripheralId,
                                                           const char *serviceUuid,
                                                           const char *characteristicUuid,
                                                           characteristic_index_t instanceIndex)
{
    CBCharacteristic *characteristic = getCharacteristic(peripheralId, serviceUuid, characteristicUuid, instanceIndex);
    return characteristic.properties;
}

/**
 * @brief Sends a request to read the value of the specified service's characteristic
 *        for the given peripheral.
 *
 * The call fails if the characteristic is not readable.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @param serviceUuid The service UUID.
 * @param characteristicUuid The characteristic UUID.
 * @param instanceIndex The instance index of the characteristic if listed more than once
 *                      for the service, otherwise zero.
 * @param onValueRead Called when the request has completed (successfully or not)
 *                    and with the data read from the characteristic.
 * @param requestIndex The index of this request, passed back when calling @p onRequestStatus.
 */
void sgBleReadCharacteristic(peripheral_id_t peripheralId,
                             const char *serviceUuid,
                             const char *characteristicUuid,
                             characteristic_index_t instanceIndex,
                             ValueReadCallback onValueRead,
                             request_index_t requestIndex)
{
    SGBlePeripheralQueue *peripheral = getSGBlePeripheralQueue(peripheralId, onValueRead, requestIndex);
    [peripheral queueReadValueForCharacteristic:getCharacteristic(peripheralId, serviceUuid, characteristicUuid, instanceIndex)
                            valueReadHandler:toValueReadHandler(onValueRead, requestIndex)];
}

/**
 * @brief Sends a request to write to the specified service's characteristic
 *        for the given peripheral.
 *
 * The call fails if the characteristic is not writable.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @param serviceUuid The service UUID.
 * @param characteristicUuid The characteristic UUID.
 * @param instanceIndex The instance index of the characteristic if listed more than once
 *                      for the service, otherwise zero.
 * @param data A pointer to the data to write to the characteristic (may be null if length is zero).
 * @param length The size in bytes of the data.
 * @param withoutResponse Whether to wait for the peripheral to respond.
 * @param onRequestStatus Called when the request has completed (successfully or not).
 * @param requestIndex The index of this request, passed back when calling @p onRequestStatus.
 */
void sgBleWriteCharacteristic(peripheral_id_t peripheralId,
                              const char *serviceUuid,
                              const char *characteristicUuid,
                              characteristic_index_t instanceIndex,
                              const void *data,
                              const size_t length,
                              bool withoutResponse,
                              RequestStatusCallback onRequestStatus,
                              request_index_t requestIndex)
{
    if (data)
    {
        SGBlePeripheralQueue *peripheral = getSGBlePeripheralQueue(peripheralId, onRequestStatus, requestIndex);
        [peripheral queueWriteValue:[NSData dataWithBytes:data length:length]
                  forCharacteristic:getCharacteristic(peripheralId, serviceUuid, characteristicUuid, instanceIndex)
                               type:withoutResponse ? CBCharacteristicWriteWithoutResponse : CBCharacteristicWriteWithResponse
                  completionHandler:toCompletionHandler(onRequestStatus, requestIndex)];
    }
    else if (onRequestStatus)
    {
        onRequestStatus(requestIndex, toErrorCode(SGBleInvalidParametersError));
    }
}

/**
 * @brief Subscribes or unsubscribes for value changes of the specified service's characteristic
 *        for the given peripheral.
 *
 * Replaces a previously registered value change handler for the same characteristic.
 * The call fails if the characteristic doesn't support notifications.
 *
 * @param peripheralId The UUID assigned by the system for the peripheral.
 * @param serviceUuid The service UUID.
 * @param characteristicUuid The characteristic UUID.
 * @param instanceIndex The instance index of the characteristic if listed more than once
 *                      for the service, otherwise zero.
 * @param onValueChanged Called when the value of the characteristic changes.
 *                       Pass null to unsubscribe. <br>
 *                       The callback must stay valid until the characteristic is unsubscribed
 *                       or the peripheral is released.
 * @param onRequestStatus Called when the request has completed (successfully or not).
 * @param requestIndex The index of this request, passed back when calling @p onRequestStatus.
 */
void sgBleSetNotifyCharacteristic(peripheral_id_t peripheralId,
                                  const char *serviceUuid,
                                  const char *characteristicUuid,
                                  characteristic_index_t instanceIndex,
                                  ValueReadCallback onValueChanged,
                                  RequestStatusCallback onRequestStatus,
                                  request_index_t requestIndex)
{
    SGBlePeripheralQueue *peripheral = getSGBlePeripheralQueue(peripheralId, onRequestStatus, requestIndex);
    [peripheral queueSetNotifyValueForCharacteristic:getCharacteristic(peripheralId, serviceUuid, characteristicUuid, instanceIndex)
                                 valueChangedHandler:toValueReadHandler(onValueChanged, requestIndex)
                                   completionHandler:toCompletionHandler(onRequestStatus, requestIndex)];
}

} // extern "C"
