#import "SGBleUtils.h"

NSErrorDomain sgBleGetErrorDomain()
{
    static NSErrorDomain pxBleErrorDomain =
        [NSString stringWithFormat:@"%@.pxBLE.errorDomain", [[NSBundle mainBundle] bundleIdentifier]];
    return pxBleErrorDomain;
}

dispatch_queue_t sgBleGetSerialQueue()
{
    static dispatch_queue_t queue =
        dispatch_queue_create("com.systemic.pixels.ble", DISPATCH_QUEUE_SERIAL);
    return queue;
}
