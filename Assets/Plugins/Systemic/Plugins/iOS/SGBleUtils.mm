#import "SGBleUtils.h"

dispatch_queue_t sgBleGetSerialQueue()
{
    static dispatch_queue_t queue =
        dispatch_queue_create("com.systemic.pixels.ble", DISPATCH_QUEUE_SERIAL);
    return queue;
}

NSErrorDomain sgBleGetErrorDomain()
{
    static NSErrorDomain pxBleErrorDomain =
        [NSString stringWithFormat:@"%@.pxBLE.errorDomain", [[NSBundle mainBundle] bundleIdentifier]];
    return pxBleErrorDomain;
}

NSError *SGBleDisconnectedError = [NSError errorWithDomain:sgBleGetErrorDomain()
                                                        code:SGBlePeripheralRequestErrorDisconnected
                                                    userInfo:@{ NSLocalizedDescriptionKey: @"Disconnected" }];

NSError *SGBleInvalidCallError = [NSError errorWithDomain:sgBleGetErrorDomain()
                                                       code:SGBlePeripheralRequestErrorInvalidCall
                                                   userInfo:@{ NSLocalizedDescriptionKey: @"Invalid call" }];


NSError *SGBleInvalidParametersError = [NSError errorWithDomain:sgBleGetErrorDomain()
                                                             code:SGBlePeripheralRequestErrorInvalidParameters
                                                         userInfo:@{ NSLocalizedDescriptionKey: @"Invalid parameters" }];

NSError *SGBleCanceledError = [NSError errorWithDomain:sgBleGetErrorDomain()
                                                    code:SGBlePeripheralRequestErrorCanceled
                                                userInfo:@{ NSLocalizedDescriptionKey: @"Canceled" }];
