
namespace Systemic.Unity.Pixels
{
    /// <summary>
    /// Available combinations of Pixel designs and colors.
    /// </summary>
    public enum PixelDesignAndColor : byte
    {
        Unknown = 0,
        Generic,
        V3Orange,
        V4BlackClear,
        V4WhiteClear,
        V5Grey,
        V5White,
        V5Black,
        V5Gold,
        OnyxBlack,
        HematiteGrey,
        MidnightGalaxy,
        AuroraSky
    }

    /// <summary>
    /// Pixel roll states.
    /// </summary>
    public enum PixelRollState : byte
    {
        /// The Pixel roll state could not be determined.
        Unknown = 0,

        /// The Pixel is resting in a position with a face up.
        OnFace,

        /// The Pixel is being handled.
        Handling,

        /// The Pixel is rolling.
        Rolling,

        /// The Pixel is resting in a crooked position.
        Crooked,
    };

    /// <summary>
    /// Pixel battery states.
    /// </summary>
    public enum PixelBatteryState : byte
    {
        Unknown = 0,
        Ok,            // Battery looks fine, nothing is happening
        Low,           // Battery level is low, notify user they should recharge
        Transition,    // Coil voltage is bad, but we don't know yet if that's because we removed the die and
                       // the coil cap is still discharging, or if indeed the die is incorrectly positioned
        BadCharging,   // Coil voltage is bad, die is probably positioned incorrectly
                       // Note that currently this state is triggered during transition between charging and not charging...
        Error,         // Charge state doesn't make sense (charging but no coil voltage detected for instance)
        Charging,      // Battery is currently recharging
        TrickleCharge, // Battery is almost full
        Done		   // Battery is full and finished charging
    }

    /// <summary>
    /// Pixel connection states.
    /// </summary>
    public enum PixelConnectionState
    {
        /// This is the value right after creation.
        Invalid = -1,

        /// This is a Pixel we knew about and scanned.
        Available,

        /// We are currently connecting to this Pixel.
        Connecting,

        /// Getting info from the Pixel, making sure it is valid to be used (correct firmware version, etc.).
        Identifying,

        /// Pixel is ready for communications.
        Ready,

        /// We are currently disconnecting from this Pixel.
        Disconnecting,
    }

    /// <summary>
    /// Identify an error encountered while communicating with a PixeL.
    /// </summary>
    public enum PixelError
    {
        /// No error.
        None = 0,

        /// An error occurred during the connection.
        ConnectionError,

        /// The Pixel is disconnected.
        Disconnected,
    }
}
