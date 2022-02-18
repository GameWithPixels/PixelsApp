/**
 * @file
 * @brief A few internal functions.
 */

#import <Foundation/Foundation.h>
#import <CoreBluetooth/CoreBluetooth.h>

// Gets the error domain of the BLE library
NSErrorDomain sgBleGetErrorDomain();

// Gets the serial queue used to run all BLE operations
dispatch_queue_t sgBleGetSerialQueue();
