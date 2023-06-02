/**
 * @file
 * @brief This file exists only to be included in UnityBridge.mm
 */

#import "SGBleCentralManagerDelegate.h"
#import "SGBlePeripheralQueue.h"
#import "SGBleTypes.h"
#import "SGBleUtils.h"

#include <cstdint>
#include <cstring>
#include <limits>

/// @brief Type for peripheral id which is the zero terminated string of the UUID
/// assigned by the system for the peripheral (may change over long periods of time).
using peripheral_id_t = const char *;

/// Type for the unique index of a BLE request given to this library.
using request_index_t = std::uint32_t;

/// Type for the index of a characteristic instance in a service.
using characteristic_index_t = std::uint32_t;

/// Type for the standard BLE properties of characteristics.
using characteristic_property_t = std::uint64_t;

/// Callback notifying of a change of the host device Bluetooth state, for example radio turned on or off.
typedef void (*BluetoothStateUpdateCallback)(bool available);

/// Callback notifying of the discovery of a BLE peripheral, with its advertisement data as a JSON string.
typedef void (*DiscoveredPeripheralCallback)(const char *advertisementDataJson);

/// Callback notifying of the status of a BLE request.
typedef void (*RequestStatusCallback)(request_index_t requestIndex, int errorCode);

/// Callback notifying of a change of a peripheral connection state, with the reason for the change.
typedef void (*PeripheralConnectionEventCallback)(request_index_t requestIndex, peripheral_id_t peripheralId, int connectionEvent, int reason);

/// Callback notifying of the RSSI value read from a peripheral.
typedef void (*RssiReadCallback)(request_index_t requestIndex, int rssi, int errorCode);

/// Callback notifying of the value read from a peripheral's characteristic.
typedef void (*ValueReadCallback)(request_index_t requestIndex, const void *data, size_t length, int errorCode);

namespace internal
{

typedef void (^CompletionHandler)(NSError *error);
typedef void (^ValueReadHandler)(SGBlePeripheralQueue *peripheral, CBCharacteristic *characteristic, NSError *error);

static const int otherErrorsMask = 0x80000000;
static const int unexpectedError = otherErrorsMask;
static const int invalidPeripheralIdErrorCode = otherErrorsMask | 1;

inline int toErrorCode(NSError *error)
{
    if (!error)
    {
        return 0;
    }
    else if (error.domain == CBErrorDomain)
    {
        // CoreBluetooth error (zero is CBErrorUnknown)
        return -1 - (int)error.code;
    }
    else if (error.domain == CBATTErrorDomain)
    {
        // Protocol error (zero is success)
        return (int)error.code;
    }
    else if (error.domain == sgBleGetErrorDomain())
    {
        // One of our own error
        return otherErrorsMask | (0x100 + (int)error.code);
    }
    else
    {
        // Any other error
        return unexpectedError;
    }
}

inline CompletionHandler toCompletionHandler(RequestStatusCallback onRequestStatus, request_index_t requestIndex)
{
    return ^(NSError *error) {
      if (onRequestStatus)
          onRequestStatus(requestIndex, toErrorCode(error));
    };
}

inline ValueReadHandler toValueReadHandler(ValueReadCallback onValueRead, request_index_t requestIndex)
{
    ValueReadHandler handler = nil;
    if (onValueRead)
    {
        handler = ^(SGBlePeripheralQueue *peripheral, CBCharacteristic *characteristic, NSError *error) {
          NSData *data = characteristic.value;
          onValueRead(requestIndex, data.bytes, data.length, toErrorCode(error));
        };
    }
    return handler;
}

// Convert c-string to array of CBUUID
inline NSArray<CBUUID *> *toCBUUIDArray(const char *serviceUuids)
{
    NSMutableArray<CBUUID *> *arr = nil;
    if (serviceUuids)
    {
        NSArray<NSString *> *servicesList = [[NSString stringWithUTF8String:serviceUuids] componentsSeparatedByString:@","];
        if (servicesList.count > 0)
        {
            arr = [NSMutableArray<CBUUID *> arrayWithCapacity:servicesList.count];
            for (NSString *uuidStr in servicesList)
            {
                CBUUID *uuid = [CBUUID UUIDWithString:uuidStr];
                if (uuid != nil)
                {
                    [arr addObject:uuid];
                }
                //else TODO error
            }
        }
    }
    return arr;
}

inline NSString *toUuidsString(NSArray<CBAttribute *> *attributes)
{
    NSMutableString *uuids = [[NSMutableString alloc] initWithCapacity:36 * attributes.count]; // A UUID has 36 characters including the dashes
    for (CBService *attr in attributes)
    {
        if (uuids.length > 0)
        {
            [uuids appendString:@","];
        }
        [uuids appendString:attr.UUID.UUIDString.lowercaseString];
    }
    return uuids;
}

inline const char *allocateCStr(NSString *str)
{
    char *cStr = NULL;
    if (str)
    {
        const char *utf8CStr = [str UTF8String];
        cStr = (char *)malloc(strlen(utf8CStr) + 1);
        std::strcpy(cStr, utf8CStr);
    }
    return cStr;
}

inline NSString *toJsonStr(CBUUID *uuid)
{
    return uuid.UUIDString.lowercaseString;
}

inline void appendToJsonStr(NSMutableString *jsonStr, NSArray<CBUUID *> *uuids)
{
    [jsonStr appendString:@"["];
    NSUInteger len = uuids.count;
    for (NSUInteger i = 0; i < len; i++)
    {
        if (i)
        {
            [jsonStr appendString:@","];
        }
        [jsonStr appendFormat:@"\"%@\"", toJsonStr(uuids[i])];
    }
    [jsonStr appendString:@"]"];
}

inline void appendToJsonStr(NSMutableString *jsonStr,
                            std::uint8_t *bytes,
                            NSUInteger start,
                            NSUInteger end)
{
    [jsonStr appendString:@"["];
    for (NSUInteger i = start; i < end; i++)
    {
        if (i > start)
        {
            [jsonStr appendString:@","];
        }
        [jsonStr appendFormat:@"%d", bytes[i]];
    }
    [jsonStr appendString:@"]"];
}


inline void appendToJsonStr(NSMutableString *jsonStr, NSData *data)
{
    std::uint8_t *bytes = (std::uint8_t *)data.bytes;
    appendToJsonStr(jsonStr, bytes, 0, data.length);
}

inline NSString *advertisementDataToJsonString(const char *systemId, NSDictionary<NSString *, id> *advertisementData, NSNumber *RSSI)
{
    // Get the different bits of advertising data
    NSData *manufacturerData = advertisementData[CBAdvertisementDataManufacturerDataKey];
    NSString *localName = advertisementData[CBAdvertisementDataLocalNameKey];
    NSDictionary<CBUUID *, NSData *> *servicesData = advertisementData[CBAdvertisementDataServiceDataKey];
    NSArray<CBUUID *> *serviceUUIDs = advertisementData[CBAdvertisementDataServiceUUIDsKey];
    NSArray<CBUUID *> *overflowServiceUUIDs = advertisementData[CBAdvertisementDataOverflowServiceUUIDsKey];
    NSNumber *txPowerLevel = advertisementData[CBAdvertisementDataTxPowerLevelKey];
    NSNumber *isConnectable = advertisementData[CBAdvertisementDataIsConnectable];
    NSArray<CBUUID *> *solicitedServiceUUIDs = advertisementData[CBAdvertisementDataSolicitedServiceUUIDsKey];

    NSMutableString *jsonStr = [NSMutableString new];
    [jsonStr appendFormat:@"{\"systemId\":\"%s\",", systemId];
    if (manufacturerData && manufacturerData.length >= 2)
    {
        // Only one manufacturer
        [jsonStr appendString:@"\"manufacturersData\":["];
        std::uint8_t *bytes = (std::uint8_t *)manufacturerData.bytes;
        uint16_t companyId = bytes[1] | ((uint16_t)bytes[0] << 8);
        [jsonStr appendFormat:@"{\"companyId\":%d,", companyId];
        [jsonStr appendString:@"\"data\":"];
        appendToJsonStr(jsonStr, bytes, 2, manufacturerData.length);
        [jsonStr appendString:@"}],"];
    }
    if (localName)
    {
        [jsonStr appendFormat:@"\"name\":\"%@\",", localName];
    }
    if (isConnectable.boolValue)
    {
        [jsonStr appendString:@"\"isConnectable\":true,"];
    }
    if (servicesData && servicesData.count)
    {
        [jsonStr appendString:@"\"servicesData\":["];
        bool first = true;
        // Iterate services
        for (CBUUID *uuid in servicesData)
        {
            if (!first)
            {
                [jsonStr appendString:@","];
            }
            first = false;
            [jsonStr appendFormat:@"{\"uuid\":\"%@\",", toJsonStr(uuid)];
            [jsonStr appendString:@"\"data\":"];
            appendToJsonStr(jsonStr, [servicesData objectForKey:uuid]);
            [jsonStr appendString:@"}"];
        }
        [jsonStr appendString:@"],"];
    }
    if (serviceUUIDs && serviceUUIDs.count)
    {
        [jsonStr appendString:@"\"services\":"];
        appendToJsonStr(jsonStr, serviceUUIDs);
        [jsonStr appendString:@","];
    }
    if (overflowServiceUUIDs && overflowServiceUUIDs.count)
    {
        [jsonStr appendString:@"\"overflowServices\":"];
        appendToJsonStr(jsonStr, overflowServiceUUIDs);
        [jsonStr appendString:@","];
    }
    if (solicitedServiceUUIDs && solicitedServiceUUIDs.count)
    {
        [jsonStr appendString:@"\"solicitedServices\":"];
        appendToJsonStr(jsonStr, solicitedServiceUUIDs);
        [jsonStr appendString:@","];
    }
    if (txPowerLevel)
    {
        [jsonStr appendFormat:@"\"txPowerLevel\":%@,", txPowerLevel];
    }
    [jsonStr appendFormat:@"\"rssi\":%@", RSSI];
    [jsonStr appendString:@"}"];
    return jsonStr;
}

SGBleCentralManagerDelegate *getCentral();

NSMutableDictionary<CBPeripheral *, SGBlePeripheralQueue *> *getPeripherals();

inline const char *getPeripheralId(CBPeripheral *peripheral)
{
    return [[peripheral.identifier UUIDString] UTF8String];
}

inline CBPeripheral *getCBPeripheral(peripheral_id_t peripheralId)
{
    CBPeripheral *peripheral = nil;
    if (peripheralId)
    {
        NSUUID *uuid = [[NSUUID alloc] initWithUUIDString:[NSString stringWithUTF8String:peripheralId]];
        peripheral = [getCentral() peripheralForIdentifier:uuid];
    }
    return peripheral;
}

inline const char *getPeripheralId(SGBlePeripheralQueue *peripheral)
{
    return [[peripheral.peripheral.identifier UUIDString] UTF8String];
}

inline SGBlePeripheralQueue *getSGBlePeripheralQueue(peripheral_id_t peripheralId)
{
    return [getPeripherals() objectForKey:getCBPeripheral(peripheralId)];
}

inline SGBlePeripheralQueue *getSGBlePeripheralQueue(peripheral_id_t peripheralId, RequestStatusCallback onRequestStatus, request_index_t requestIndex)
{
    SGBlePeripheralQueue *sgPeripheral = getSGBlePeripheralQueue(peripheralId);
    if (!sgPeripheral && onRequestStatus)
    {
        onRequestStatus(requestIndex, invalidPeripheralIdErrorCode);
    }
    return sgPeripheral;
}

inline SGBlePeripheralQueue *getSGBlePeripheralQueue(peripheral_id_t peripheralId, RssiReadCallback onRssiRead, request_index_t requestIndex)
{
    SGBlePeripheralQueue *sgPeripheral = getSGBlePeripheralQueue(peripheralId);
    if (!sgPeripheral && onRssiRead)
    {
        onRssiRead(std::numeric_limits<int>::min(), requestIndex, invalidPeripheralIdErrorCode);
    }
    return sgPeripheral;
}

inline SGBlePeripheralQueue *getSGBlePeripheralQueue(peripheral_id_t peripheralId, ValueReadCallback onValueRead, request_index_t requestIndex)
{
    SGBlePeripheralQueue *sgPeripheral = getSGBlePeripheralQueue(peripheralId);
    if (!sgPeripheral && onValueRead)
    {
        onValueRead(requestIndex, nullptr, 0, invalidPeripheralIdErrorCode);
    }
    return sgPeripheral;
}

inline CBService *getService(peripheral_id_t peripheralId, const char *serviceUuidStr)
{
    if (peripheralId && serviceUuidStr)
    {
        CBUUID *serviceUuid = [CBUUID UUIDWithString:[NSString stringWithUTF8String:serviceUuidStr]];
        CBPeripheral *peripheral = getCBPeripheral(peripheralId);
        for (CBService *service in peripheral.services)
        {
            if ([serviceUuid isEqual:service.UUID])
            {
                return service;
            }
        }
    }
    return nil;
}

inline CBCharacteristic *getCharacteristic(peripheral_id_t peripheralId, const char *serviceUuidStr, const char *characteristicUuidStr, characteristic_index_t instanceIndex)
{
    CBService *service = getService(peripheralId, serviceUuidStr);
    if (service && characteristicUuidStr)
    {
        CBUUID *characteristicUuid = [CBUUID UUIDWithString:[NSString stringWithUTF8String:characteristicUuidStr]];
        for (CBCharacteristic *characteristic in service.characteristics)
        {
            if ([characteristicUuid isEqual:characteristic.UUID])
            {
                if (instanceIndex == 0)
                {
                    return characteristic;
                }
                else
                {
                    --instanceIndex;
                }
            }
        }
    }
    return nil;
}

} // namespace internal
