/**
 * @file
 * @brief Library error codes and a few internal functions.
 */

#import <Foundation/Foundation.h>
#import <CoreBluetooth/CoreBluetooth.h>

/**
 * @brief Peripheral request error codes.
 * @ingroup Apple_Objective-C
 */
typedef NS_ENUM(NSInteger, SGBlePeripheralRequestError)
{
    /// Peripheral got disconnected while executing request.
    SGBlePeripheralRequestErrorDisconnected,
    
    /// Peripheral not in proper state to execute request.
    SGBlePeripheralRequestErrorInvalidCall,
    
    /// Peripheral request has some invalid parameters.
    SGBlePeripheralRequestErrorInvalidParameters,
    
    /// Peripheral request got canceled.
    SGBlePeripheralRequestErrorCanceled,
};

/**
 * @brief Peripheral got disconnected.
 * @ingroup Apple_Objective-C
 */
extern NSError *SGBleDisconnectedError;

/**
 * @brief Peripheral not in proper state to execute request.
 * @ingroup Apple_Objective-C
 */
extern NSError *SGBleInvalidCallError;

/**
 * @brief Peripheral request has some invalid parameters.
 * @ingroup Apple_Objective-C
 */
extern NSError *SGBleInvalidParametersError;

/**
 * @brief Peripheral request got canceled.
 * @ingroup Apple_Objective-C
 */
extern NSError *SGBleCanceledError;

//
// Internal
//

// Gets the serial queue used to run all BLE operations
dispatch_queue_t sgBleGetSerialQueue();

// Gets the error domain of the BLE library
NSErrorDomain sgBleGetErrorDomain();
