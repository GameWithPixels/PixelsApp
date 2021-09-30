/**
 * @file
 * @brief Definition of the SGBlePeripheralQueue class.
 */

#import <Foundation/Foundation.h>
#import <CoreBluetooth/CoreBluetooth.h>
#import "SGBleCentralManagerDelegate.h"
#import "SGBleTypes.h"
#import "SGBleRequest.h"

/**
 * @brief Implementation of the
 *        <a href="https://developer.apple.com/documentation/corebluetooth/cbperipheraldelegate">
 *        CBPeripheralDelegate</a> protocol. Queues up operations to
 *        be performed with a Bluetooth Low Energy (BLE)
 *        <a href="https://developer.apple.com/documentation/corebluetooth/cbperipheral">
 *        peripheral</a>, run them sequentially and notify of their outcome.
 *
 * The next queued operation (a SGBleRequest instance) is run only once the current one
 * completes (whether successfully or not).
 *
 * The connection request has no time out and waits until the peripheral is connected.
 * Any subsequent request is therefore blocked until the connection is successful.
 *
 * Call cancelQueue() to cancel all pending operations, including an on-going connection.
 *
 * Handlers (such as request completion handlers) are called on the shared BLE queue,
 * user code for those handlers should return as quickly as possible to avoid delaying
 * any further BLE event or operation.
 *
 * On being de-allocated, the instance cancels the connection to peripheral.
 *
 * @note A request keep a strong reference to the instance so the later won't be de-allocated
 *       until the queue is empty.
 * 
 * @ingroup Apple_Objective-C
 */
@interface SGBlePeripheralQueue : NSObject<CBPeripheralDelegate>
//! @cond
{
    dispatch_queue_t _queue; // Run all peripheral requests
    SGBleCentralManagerDelegate *_centralDelegate;
    CBPeripheral *_peripheral;
    
    // Connection
    void (^_connectionEventHandler)(SGBleConnectionEvent connectionEvent, SGBleConnectionEventReason reason);
    NSArray<CBUUID *> *_requiredServices;
    NSUInteger _discoveringServicesCounter;
    SGBleConnectionEventReason _disconnectReason;
    
    // Last RSSI
    int _rssi;

    // Requests
    SGBleRequest *_runningRequest; // Accessed only from queue
    NSMutableArray<SGBleRequest *> *_pendingRequests; // Always synchronize access to this list

    // Read notifications
    void (^_valueReadHandler)(CBCharacteristic *characteristic, NSError *error);
    NSMapTable<CBCharacteristic *, void (^)(CBCharacteristic *characteristic, NSError *error)> *_valueChangedHandlers;
}

// Property getters
- (CBPeripheral *)peripheral;
- (bool)isConnected;
- (int)rssi;
//! @endcond

/**
 * @brief Gets the CBPeripheral object for this peripheral.
 */
@property(readonly, getter=peripheral) CBPeripheral *peripheral;

/**
 * @brief Indicates whether the peripheral is connected.
 */
@property(readonly, getter=isConnected) bool isConnected;

/**
 * @brief Gets the last read value of the Received Signal Strength Indicator (RSSI).
 */
@property(readonly, getter=rssi) int rssi;

//! \name Initialization
//! @{

/**
 * @brief Initializes a SGBlePeripheralQueue instance for the given CBPeripheral object,
 *        with a central manager delegate and a connection event handler.
 *
 * @param peripheral The CBPeripheral object for the BLE peripheral.
 * @param centralManagerDelegate The instance of SGBleCentralManagerDelegate that discovered the peripheral.
 * @param connectionEventHandler The handler for notifying of changes of the connection status of the peripheral.
 * @return The initialized SGBlePeripheral instance.
 */
- (instancetype)initWithPeripheral:(CBPeripheral *)peripheral
            centralManagerDelegate:(SGBleCentralManagerDelegate *)centralManagerDelegate
            connectionEventHandler:(void (^)(SGBleConnectionEvent connectionEvent, SGBleConnectionEventReason reason))connectionEventHandler;

//! @}
//! \name Connection and disconnection
//! @{

/**
 * @brief Queues a request to connect to the peripheral.
 *
 * This request has not time out and waits until the peripheral is connected.
 * Call cancelQueue() to cancel all pending operations.
 *
 * @param services List of services UUIDs that the peripheral should support, may be null or empty.
 * @param completionHandler The handler for notifying of the request result.
 */
- (void)queueConnectWithServices:(NSArray<CBUUID *> *)services
               completionHandler:(void (^)(NSError *error))completionHandler;

/**
 * @brief Queues a request to disconnect the peripheral.
 *
 * @note The request being queued, it is processed only when previous requests have completed.
 *       For an immediate disconnection first call cancelQueue().
 *
 * @param completionHandler The handler for notifying of the request result.
 */
- (void)queueDisconnect:(void (^)(NSError *error))completionHandler;

//! @}
//! \name Peripheral operations
//! Valid only for connected peripherals.
//! @{

/**
 * @brief Queues a request to read the Received Signal Strength Indicator (RSSI).
 *
 * @param completionHandler The handler for notifying of the read RSSI and the request status..
 */
- (void)queueReadRssi:(void (^)(NSError *error))completionHandler;

//! @}
//! \name Characteristic operations
//! Valid only for connected peripherals.
//! @{

/**
 * @brief Queues a request to read the value of the specified service's characteristic.
 *
 * The call fails if the characteristic is not readable.
 *
 * @param characteristic The CBCharacteristic object.
 * @param valueReadHandler The handler for notifying of the read value and the request status.
 */
- (void)queueReadValueForCharacteristic:(CBCharacteristic *)characteristic
                       valueReadHandler:(void (^)(CBCharacteristic *characteristic, NSError *error))valueReadHandler;

/**
 * @brief Queues a request to write the value of specified service's characteristic.
 *
 * The call fails if the characteristic is not writable.
 *
 * @param data The data to write (may be empty but not nil).
 * @param characteristic The CBCharacteristic object.
 * @param type The write type to perform.
 * @param completionHandler The handler for notifying of the request result.
 */
- (void)queueWriteValue:(NSData *)data
      forCharacteristic:(CBCharacteristic *)characteristic
                   type:(CBCharacteristicWriteType)type
      completionHandler:(void (^)(NSError *error))completionHandler;

/**
 * @brief Queues a request to enable or disable notifications for the specified service's characteristic.
 *
 * Replaces a previously registered value change handler.
 * The call fails if the characteristic doesn't support notifications.
 *
 * @param characteristic The CBCharacteristic object.
 * @param valueChangedHandler The handler for notifying of the characteristic's value changes.
 * @param completionHandler The handler for notifying of the request result.
 */
- (void)queueSetNotifyValueForCharacteristic:(CBCharacteristic *)characteristic
                         valueChangedHandler:(void (^)(CBCharacteristic *characteristic, NSError *error))valueChangedHandler
                           completionHandler:(void (^)(NSError *error))completionHandler;

//! @}
//! \name Queue management.
//! @{

/**
 * @brief Clear the queue of pending request and cancel the running request.
 */
- (void)cancelQueue;

//! @}

@end
