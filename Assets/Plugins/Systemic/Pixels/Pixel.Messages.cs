using System.Collections;
using Systemic.Unity.Pixels.Animations;
using Systemic.Unity.Pixels.Messages;
using UnityEngine;

namespace Systemic.Unity.Pixels
{
    partial class Pixel
    {
        /// <summary>
        /// The timeout in seconds for waiting the response of a message send to a Pixel.
        /// </summary>
        public const float AckMessageTimeout = 5;

        /// <summary>
        /// Sends a message to the Pixel to play the animation stored at the given index
        /// with face optional remapping and looping.
        /// </summary>
        /// <param name="animationIndex">The stored index of the animation to play.</param>
        /// <param name="remapFace">The index of the face to remap the animation to.</param>
        /// <param name="loop">Whether to loop the animation.</param>
        public void PlayAnimation(int animationIndex, int remapFace = 0, bool loop = false)
        {
            PostMessage(new PlayAnimation()
            {
                index = (byte)animationIndex,
                remapFace = (byte)remapFace,
                loop = loop ? (byte)1 : (byte)0
            });
        }

        /// <summary>
        /// Sends a message to the Pixel to stop playing the animation stored on the Pixel
        /// at the given index with face remapping.
        /// </summary>
        /// <param name="animationIndex">The stored index of the animation to stop playing.</param>
        /// <param name="remapFace">The index of the face to remap the animation to.</param>
        public void StopAnimation(int animationIndex, int remapFace = 0)
        {
            PostMessage(new StopAnimation()
            {
                index = (byte)animationIndex,
                remapFace = (byte)remapFace,
            });
        }

        /// <summary>
        /// Sends a message to the Pixel to update the instance <see cref="rollState"/> and
        /// <see cref="currentFace"/> properties.
        /// </summary>
        /// <param name="onResult">An optional callback that is called when the operation completes
        ///                        successfully (true) or not (false) with an error message.</param>
        /// <returns>An enumerator meant to be run as a coroutine.</returns>
        public IEnumerator GetRollStateAsync(OperationResultCallback onResult = null)
        {
            var op = new SendMessageAndWaitForResponseEnumerator<RequestRollState, RollState>(this);
            yield return op;
            onResult?.Invoke(op.IsSuccess, op.Error);
        }

        /// <summary>
        /// Sends a message to the Pixel to update the instance information.
        /// 
        /// On success, this will update the <see cref="ledCount"/>, <see cref="designAndColor"/>,
        /// <see cref="dataSetHash"/>, <see cref="availableFlashSize"/> and <see cref="firmwareVersion"/>
        /// properties and raise the <see cref="AppearanceChanged"/> event if the face out or design
        /// and color have changed.
        /// </summary>
        /// <param name="onResult">An optional callback that is called when the operation completes
        ///                        successfully (true) or not (false) with an error message.</param>
        /// <returns>An enumerator meant to be run as a coroutine.</returns>
        public IEnumerator UpdateInfoAsync(OperationResultCallback onResult = null)
        {
            var op = new SendMessageAndWaitForResponseEnumerator<WhoAreYou, IAmADie>(this);
            yield return op;
            onResult?.Invoke(op.IsSuccess, op.Error);
        }

        /// <summary>
        /// Sends a message to the Pixel to turn telemetry on or off.
        /// </summary>
        /// <param name="activate">Whether to turn telemetry reporting on or off.</param>
        public void ReportTelemetry(bool activate)
        {
            PostMessage(new RequestTelemetry()
            {
                requestMode = activate ? TelemetryRequestMode.Repeat : TelemetryRequestMode.Off,
                minInterval = 100,
            });
        }

        /// <summary>
        /// Sends a message to the Pixel to make it report the <see cref="rssi"/> value.
        /// </summary>
        /// <param name="activate">Whether to turn RSSI reporting on or off.</param>
        public void ReportRssi(bool activate)
        {
            PostMessage(new RequestRssi()
            {
                requestMode = activate ? TelemetryRequestMode.Repeat : TelemetryRequestMode.Off,
                minInterval = 5000, // 5 sec
            });
        }

        /// <summary>
        /// Sends a message to the Pixel to update the <see cref="mcuTemperature"/> and
        /// <see cref="batteryTemperature"/> properties.
        /// </summary>
        /// <param name="onResult"></param>
        public void UpdateTemperature(OperationResultCallback onResult = null)
        {
            PostMessage(new RequestTemperature());
        }

        /// <summary>
        /// Sends a message to the Pixel to set its design and color.
        /// </summary>
        /// <param name="designAndColor">The design and color value to set.</param>
        /// <param name="onResult">An optional callback that is called when the operation completes
        ///                        successfully (true) or not (false) with an error message.</param>
        /// <returns>An enumerator meant to be run as a coroutine.</returns>
        public IEnumerator SetDesignAndColorAsync(PixelDesignAndColor designAndColor, OperationResultCallback onResult = null)
        {
            var op = new SendMessageAndProcessResponseEnumerator<SetDesignAndColor, Rssi>(this,
                new SetDesignAndColor() { designAndColor = designAndColor },
                _ =>
                {
                    if (this.designAndColor != designAndColor)
                    {
                        this.designAndColor = designAndColor;
                        AppearanceChanged?.Invoke(this, ledCount, this.designAndColor);
                    }
                });
            yield return op;
            onResult?.Invoke(op.IsSuccess, op.Error);
        }

        /// <summary>
        /// Sends a message to the Pixel to change its name.
        /// </summary>
        /// <param name="name">The name to set.</param>
        /// <param name="onResult">An optional callback that is called when the operation completes
        ///                        successfully (true) or not (false) with an error message.</param>
        /// <returns>An enumerator meant to be run as a coroutine.</returns>
        public IEnumerator RenameAsync(string name, OperationResultCallback onResult = null)
        {
            Debug.Log($"Pixel {SafeName}: Renaming to {name}");
            var bytes = Marshaling.StringToBytes(name, SetName.NameMaxSize, true);
            var op = new SendMessageAndWaitForResponseEnumerator<SetName, SetNameAck>(this, new SetName { name = bytes });
            yield return op;
            onResult?.Invoke(op.IsSuccess, op.Error);
        }

        /// <summary>
        /// Sends a message to the Pixel to set all the Pixel LEDs with the same given color.
        /// </summary>
        /// <param name="color">The desired color for the LEDs.</param>
        public void SetLEDsToColor(Color color)
        {
            Color32 color32 = color;
            PostMessage(new SetAllLEDsToColor
            {
                color = (uint)((color32.r << 16) + (color32.g << 8) + color32.b)
            });
        }

        /// <summary>
        /// Sends a message to the Pixel to make its LEDs blink a given number of time.
        /// </summary>
        /// <param name="color">The desired color for the LEDs.</param>
        /// <param name="count">The number of blinks.</param>
        /// <param name="duration">Duration of the animation in seconds.</param>
        /// <param name="onResult">An optional callback that is called when the operation completes
        ///                        successfully (true) or not (false) with an error message.</param>
        /// <returns>An enumerator meant to be run as a coroutine.</returns>
        public IEnumerator BlinkLEDsAsync(Color color, int count = 1, float duration = 1, float fade = 0.5f, OperationResultCallback onResult = null)
        {
            Color32 color32 = color;
            var msg = new Blink
            {
                color = (uint)((color32.r << 16) + (color32.g << 8) + color32.b),
                flashCount = (byte)count,
                duration = (ushort)(1000 * duration),
                fade = (byte)(255 * fade),
                faceMask = Constants.FaceMaskAll,
            };
            var op = new SendMessageAndWaitForResponseEnumerator<Blink, BlinkFinished>(this, msg);
            yield return op;
            onResult?.Invoke(op.IsSuccess, op.Error);
        }

        /// <summary>
        /// Sends a message to the Pixel to start die calibration.
        /// </summary>
        public void StartCalibration()
        {
            PostMessage(new Calibrate());
        }

        /// <summary>
        /// Sends a message to the Pixel to start face calibration.
        /// </summary>
        /// <param name="face"></param>
        public void CalibrateFace(int face)
        {
            PostMessage(new CalibrateFace() { faceIndex = (byte)face });
        }

        /// <summary>
        /// Sends a message to the Pixel to set it to standard mode (the default
        /// which plays animations based on roll events).
        /// </summary>
        public void SetStandardMode()
        {
            PostMessage(new SetTopLevelState() { state = 1 });
        }

        /// <summary>
        /// Sends a message to the Pixel to set it to LED animator mode.
        /// </summary>
        public void SetLEDAnimatorMode()
        {
            PostMessage(new SetTopLevelState() { state = 2 });
        }

        /// <summary>
        /// Sends a message to the Pixel to reset its parameters.
        /// </summary>
        public void ResetParameters()
        {
            PostMessage(new ProgramDefaultParameters());
        }
    }
}
