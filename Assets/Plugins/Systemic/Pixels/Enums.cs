
namespace Systemic.Unity.Pixels
{
    /// <summary>
    /// Available combinations of Pixel designs and colors.
    /// </summary>
    public enum PixelDesignAndColor : byte
    {
        Unknown = 0,
        Generic,
        V3_Orange,
        V4_BlackClear,
        V4_WhiteClear,
        V5_Grey,
        V5_White,
        V5_Black,
        V5_Gold,
        Onyx_Back,
        Hematite_Grey,
        Midnight_Galaxy,
        Aurora_Sky
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
