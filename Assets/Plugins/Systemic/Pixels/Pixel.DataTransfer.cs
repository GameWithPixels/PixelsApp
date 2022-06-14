using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using Systemic.Unity.Pixels.Messages;
using Systemic.Unity.Pixels.Animations;
using UnityEngine;

namespace Systemic.Unity.Pixels
{
    partial class Pixel
    {
        /// <summary>
        /// Asynchronously uploads the given data to the Pixel flash memory.
        /// </summary>
        /// <param name="bytes">The data to upload.</param>
        /// <param name="onResult">An optional callback that is called when the operation completes
        ///                        successfully (true) or not (false) with an error message.</param>
        /// <param name="onProgress">An optional callback that is called as the operation progresses
        ///                          with the progress value being between 0 an 1.</param>
        /// <returns>An enumerator meant to be run as a coroutine.</returns>
        public IEnumerator UploadBulkDataAsync(byte[] bytes, OperationResultCallback onResult = null, OperationProgressCallback onProgress = null)
        {
            if (bytes == null) throw new System.ArgumentNullException(nameof(bytes));

            // Keep name locally in case our game object gets destroyed along the way
            string name = SafeName;

            short remainingSize = (short)bytes.Length;
            Debug.Log($"Pixel {name}: Sending {remainingSize} bytes of bulk data");
            onProgress?.Invoke(this, 0);

            // Send setup message
            IOperationEnumerator sendMsg =
                new SendMessageAndWaitForResponseEnumerator<BulkSetup, BulkSetupAck>(this, new BulkSetup { size = remainingSize });
            yield return sendMsg;

            if (sendMsg.IsSuccess)
            {
                Debug.Log($"Pixel {name}: ready for sending data");

                // Then transfer data
                ushort offset = 0;
                while (remainingSize > 0)
                {
                    var data = new BulkData()
                    {
                        offset = offset,
                        size = (byte)Mathf.Min(remainingSize, Marshaling.MaxDataSize),
                        data = new byte[Marshaling.MaxDataSize],
                    };

                    System.Array.Copy(bytes, offset, data.data, 0, data.size);

                    //Debug.Log($"Pixel {name}: sending Bulk Data (offset: 0x" + data.offset.ToString("X") + ", length: " + data.size + ")");
                    //StringBuilder hexdumpBuilder = new StringBuilder();
                    //for (int i = 0; i < data.data.Length; ++i)
                    //{
                    //    if (i % 8 == 0)
                    //    {
                    //        hexdumpBuilder.AppendLine();
                    //    }
                    //    hexdumpBuilder.Append(data.data[i].ToString("X02") + " ");
                    //}
                    //Debug.Log(hexdumpBuilder.ToString());

                    sendMsg = new SendMessageAndWaitForResponseEnumerator<BulkData, BulkDataAck>(this, data);
                    yield return sendMsg;

                    if (sendMsg.IsSuccess)
                    {
                        remainingSize -= data.size;
                        offset += data.size;
                        onProgress?.Invoke(this, (float)offset / bytes.Length);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (sendMsg.IsSuccess)
            {
                Debug.Log($"Pixel {name}: Finished sending bulk data");
                onResult?.Invoke(true, null);
            }
            else
            {
                Debug.LogError($"Pixel {name}: Failed to upload data, {sendMsg.Error}");
                onResult?.Invoke(false, sendMsg.Error);
            }
        }

        /// <summary>
        /// Asynchronously downloads the data from the Pixel flash memory.
        /// </summary>
        /// <param name="onResult">A callback that is called when the operation completes, with the received
        ///                        data if successful or null and an error message on failure.</param>
        /// <param name="onProgress">An optional callback that is called as the operation progresses
        ///                          with the progress value being between 0 an 1.</param>
        /// <returns>An enumerator meant to be run as a coroutine.</returns>
        public IEnumerator DownloadBulkDataAsync(DataOperationResultCallback onResult, OperationProgressCallback onProgress = null)
        {
            if (onResult == null) throw new System.ArgumentNullException(nameof(onResult));

            // Keep name locally in case our game object gets destroyed along the way
            string name = SafeName;

            // Wait for setup message
            short size = 0;
            var waitForMsg = new WaitForMessageEnumerator<BulkSetup>(this);
            yield return waitForMsg;

            byte[] buffer = null;
            string error = null;
            if (waitForMsg.IsSuccess)
            {
                size = waitForMsg.Message.size;
                float timeout = Time.realtimeSinceStartup + AckMessageTimeout;

                // Allocate a byte buffer
                buffer = new byte[size];
                ushort totalDataReceived = 0;

                // Setup bulk receive handler
                void bulkReceived(IPixelMessage msg)
                {
                    timeout = Time.realtimeSinceStartup + AckMessageTimeout;

                    var bulkMsg = (BulkData)msg;
                    if ((bulkMsg.offset + bulkMsg.size) < buffer.Length)
                    {
                        System.Array.Copy(bulkMsg.data, 0, buffer, bulkMsg.offset, bulkMsg.size);

                        // Sum data receive before sending any other message
                        totalDataReceived += bulkMsg.size;
                        onProgress?.Invoke(this, (float)totalDataReceived / bulkMsg.size);

                        // Send acknowledgment (no need to do it synchronously)
                        PostMessage(new BulkDataAck { offset = totalDataReceived });
                    }
                    else
                    {
                        Debug.LogError($"Pixel {name}: Received bulk data that is too big");
                    }
                }

                AddMessageHandler(MessageType.BulkData, bulkReceived);
                try
                {
                    // Send acknowledgment to Pixel, so it may transfer bulk data immediately
                    PostMessage(new BulkSetupAck());

                    // Wait for all the bulk data to be received
                    yield return new WaitUntil(() => (totalDataReceived == size) || (timeout > Time.realtimeSinceStartup));
                }
                finally
                {
                    // We're done
                    RemoveMessageHandler(MessageType.BulkData, bulkReceived);
                }

                if (totalDataReceived == size)
                {
                    Debug.Assert(error == null);
                    Debug.Log($"Pixel {name}: Done downloading bulk data");
                }
                else
                {
                    error = "Incomplete upload";
                }
            }
            else
            {
                error = "Couldn't initiate bulk upload";
            }

            if (error != null)
            {
                onResult(buffer, null);
            }
            else
            {
                Debug.LogError($"Pixel {name}: Error downloading bulk data, {error}");
                onResult(null, error);
            }
        }

        /// <summary>
        /// Asynchronously uploads the given data set of animations to the Pixel flash memory.
        /// </summary>
        /// <param name="dataSet">The data set to upload.</param>
        /// <param name="onResult">An optional callback that is called when the operation completes
        ///                        successfully (true) or not (false) with an error message.</param>
        /// <param name="onProgress">An optional callback that is called as the operation progresses
        ///                          with the progress value being between 0 an 1.</param>
        /// <returns>An enumerator meant to be run as a coroutine.</returns>
        public IEnumerator UploadDataSetAsync(DataSet dataSet, OperationResultCallback onResult = null, OperationProgressCallback onProgress = null)
        {
            if (dataSet == null) throw new System.ArgumentNullException(nameof(dataSet));

            // Keep name locally in case our game object gets destroyed along the way
            string name = SafeName;

            // Prepare the Pixel
            var prepareDie = new TransferAnimationSet
            {
                paletteSize = dataSet.animationBits.getPaletteSize(),
                rgbKeyFrameCount = dataSet.animationBits.getRGBKeyframeCount(),
                rgbTrackCount = dataSet.animationBits.getRGBTrackCount(),
                keyFrameCount = dataSet.animationBits.getKeyframeCount(),
                trackCount = dataSet.animationBits.getTrackCount(),
                animationCount = dataSet.getAnimationCount(),
                animationSize = (ushort)dataSet.animations.Sum((anim) => Marshal.SizeOf(anim.GetType())),
                conditionCount = dataSet.getConditionCount(),
                conditionSize = (ushort)dataSet.conditions.Sum((cond) => Marshal.SizeOf(cond.GetType())),
                actionCount = dataSet.getActionCount(),
                actionSize = (ushort)dataSet.actions.Sum((action) => Marshal.SizeOf(action.GetType())),
                ruleCount = dataSet.getRuleCount(),
            };
            //StringBuilder builder = new StringBuilder();
            //builder.AppendLine("Animation Data to be sent:");
            //builder.AppendLine("palette: " + prepareDie.paletteSize * Marshal.SizeOf<byte>());
            //builder.AppendLine("rgb keyframes: " + prepareDie.rgbKeyFrameCount + " * " + Marshal.SizeOf<Animations.RGBKeyframe>());
            //builder.AppendLine("rgb tracks: " + prepareDie.rgbTrackCount + " * " + Marshal.SizeOf<Animations.RGBTrack>());
            //builder.AppendLine("keyframes: " + prepareDie.keyFrameCount + " * " + Marshal.SizeOf<Animations.Keyframe>());
            //builder.AppendLine("tracks: " + prepareDie.trackCount + " * " + Marshal.SizeOf<Animations.Track>());
            //builder.AppendLine("animations: " + prepareDie.animationCount + ", " + prepareDie.animationSize);
            //builder.AppendLine("conditions: " + prepareDie.conditionCount + ", " + prepareDie.conditionSize);
            //builder.AppendLine("actions: " + prepareDie.actionCount + ", " + prepareDie.actionSize);
            //builder.AppendLine("rules: " + prepareDie.ruleCount + " * " + Marshal.SizeOf<Behaviors.Rule>());
            //builder.AppendLine("behavior: " + Marshal.SizeOf<Behaviors.Behavior>());
            //Debug.Log($"Pixel {name}: {builder}");
            //Debug.Log($"Pixel {name}: Animation Data size: {set.ComputeDataSetDataSize()}");

            var waitForMsg = new SendMessageAndWaitForResponseEnumerator<TransferAnimationSet, TransferAnimationSetAck>(this, prepareDie);
            yield return waitForMsg;

            string error = null;
            if (waitForMsg.IsSuccess)
            {
                if (waitForMsg.Message.result != 0)
                {
                    var setData = dataSet.ToByteArray();
                    //StringBuilder hexdumpBuilder = new StringBuilder();
                    //for (int i = 0; i < setData.Length; ++i)
                    //{
                    //    if (i % 8 == 0)
                    //    {
                    //        hexdumpBuilder.AppendLine();
                    //    }
                    //    hexdumpBuilder.Append(setData[i].ToString("X02") + " ");
                    //}
                    //Debug.Log($"Pixel {name}: Dump => {hexdumpBuilder}");

                    // Upload data
                    var hash = DataSet.ComputeHash(setData);
                    Debug.Log($"Pixel {name}: ready to receive dataset, byte array should be:"
                        + $" {dataSet.ComputeDataSetDataSize()} bytes and hash 0x{hash:X8}");
                    yield return InternalUploadDataSetAsync(
                        MessageType.TransferAnimationSetFinished, setData, err => error = err, onProgress);
                }
                else
                {
                    error = "Transfer refused, not enough memory";
                }
            }
            else
            {
                error = waitForMsg.Error;
            }

            if (error == null)
            {
                onResult?.Invoke(true, null);
            }
            else
            {
                Debug.LogError($"Pixel {name}: failed to upload data set, {error}");
                onResult?.Invoke(false, error);
            }
        }

        /// <summary>
        /// Asynchronously plays the LEDs animations for the given data set.
        /// </summary>
        /// <param name="testAnimSet">The data set containing the animations to play.</param>
        /// <param name="onResult">An optional callback that is called when the operation completes
        ///                        successfully (true) or not (false) with an error message.</param>
        /// <param name="onProgress">An optional callback that is called as the operation progresses
        ///                          with the progress value being between 0 an 1.</param>
        /// <returns>An enumerator meant to be run as a coroutine.</returns>
        public IEnumerator PlayTestAnimationAsync(DataSet testAnimSet, OperationResultCallback onResult = null, OperationProgressCallback onProgress = null)
        {
            if (testAnimSet == null) throw new System.ArgumentNullException(nameof(testAnimSet));

            // Keep name locally in case our game object gets destroyed along the way
            string name = SafeName;

            // Prepare the Pixel
            var prepareDie = new TransferTestAnimationSet
            {
                paletteSize = testAnimSet.animationBits.getPaletteSize(),
                rgbKeyFrameCount = testAnimSet.animationBits.getRGBKeyframeCount(),
                rgbTrackCount = testAnimSet.animationBits.getRGBTrackCount(),
                keyFrameCount = testAnimSet.animationBits.getKeyframeCount(),
                trackCount = testAnimSet.animationBits.getTrackCount(),
                animationSize = (ushort)Marshal.SizeOf(testAnimSet.animations[0].GetType()),
            };

            var setData = testAnimSet.ToTestAnimationByteArray();
            uint hash = DataSet.ComputeHash(setData);

            prepareDie.hash = hash;
            // Debug.Log($"Pixel {name}: Animation Data to be sent:");
            // Debug.Log("palette: " + prepareDie.paletteSize * Marshal.SizeOf<byte>());
            // Debug.Log("rgb keyframes: " + prepareDie.rgbKeyFrameCount + " * " + Marshal.SizeOf<Animations.RGBKeyframe>());
            // Debug.Log("rgb tracks: " + prepareDie.rgbTrackCount + " * " + Marshal.SizeOf<Animations.RGBTrack>());
            // Debug.Log("keyframes: " + prepareDie.keyFrameCount + " * " + Marshal.SizeOf<Animations.Keyframe>());
            // Debug.Log("tracks: " + prepareDie.trackCount + " * " + Marshal.SizeOf<Animations.Track>());
            var waitForMsg = new SendMessageAndWaitForResponseEnumerator<TransferTestAnimationSet, TransferTestAnimationSetAck>(this, prepareDie);
            yield return waitForMsg;

            string error = null;
            if (waitForMsg.IsSuccess)
            {
                switch (waitForMsg.Message.ackType)
                {
                    case TransferTestAnimationSetAckType.Download:
                        // Upload data
                        Debug.Log($"Pixel {name}: ready to receive test dataset, byte array should be:"
                            + $" {setData.Length} bytes and hash 0x{hash:X8}");
                        yield return InternalUploadDataSetAsync(
                            MessageType.TransferTestAnimationSetFinished, setData, err => error = err, onProgress);
                        break;

                    case TransferTestAnimationSetAckType.UpToDate:
                        // Nothing to do
                        Debug.Assert(error == null);
                        break;

                    default:
                        error = $"Got unknown ackType: {waitForMsg.Message.ackType}";
                        break;
                }
            }
            else
            {
                error = waitForMsg.Error;
            }

            if (error == null)
            {
                onResult?.Invoke(true, null);
            }
            else
            {
                Debug.LogError($"Pixel {name}: failed to play test animation, {error}");
                onResult?.Invoke(false, error);
            }
        }

        /// <summary>
        /// Asynchronously uploads the given data to the Pixel flash memory and waits for the specified
        /// confirmation message.
        /// </summary>
        /// <param name="transferDataFinished"></param>
        /// <param name="data"></param>
        /// <param name="onResult">An optional callback that is called when the operation completes
        ///                        successfully (true) or not (false) with an error message.</param>
        /// <param name="onProgress">An optional callback that is called as the operation progresses
        ///                          with the progress value being between 0 an 1.</param>
        /// <returns>An enumerator meant to be run as a coroutine.</returns>
        private IEnumerator InternalUploadDataSetAsync(MessageType transferDataFinished, byte[] data, System.Action<string> onResult, OperationProgressCallback onProgress = null)
        {
            // Keep name locally in case our game object gets destroyed along the way
            string name = SafeName;

            bool programmingFinished = false;
            void ProgrammingFinishedCallback(IPixelMessage finishedMsg) => programmingFinished = true;

            AddMessageHandler(transferDataFinished, ProgrammingFinishedCallback);

            string error = null;
            try
            {
                bool success = false;
                yield return UploadBulkDataAsync(data, (res, err) => (success, error) = (res, err), onProgress);

                if (success)
                {
                    // We're done sending data, wait for the Pixel to say its finished programming it!
                    Debug.Log($"Pixel {name}: done sending dataset, waiting for Pixel to finish programming");
                    float timeout = Time.realtimeSinceStartup + AckMessageTimeout;
                    yield return new WaitUntil(() => programmingFinished || (Time.realtimeSinceStartup > timeout));

                    if (programmingFinished)
                    {
                        Debug.Assert(error == null);
                        Debug.Log($"Pixel {name}: programming done");
                    }
                    else
                    {
                        error = "Timeout waiting on Pixel to confirm programming";
                    }
                }
            }
            finally
            {
                RemoveMessageHandler(transferDataFinished, ProgrammingFinishedCallback);
            }

            onResult(error);
        }

        //public IEnumerator UploadSettingsAsync(DieSettings settings, DieOperationResultHandler<bool> onResult = null, DieOperationProgressHandler onProgress = null)
        //{
        //    // Prepare the Pixel
        //    var waitForMsg = new SendMessageAndWaitForResponseEnumerator<DieMessageTransferSettings, DieMessageTransferSettingsAck>(this);
        //    yield return waitForMsg;

        //    if (waitForMsg.IsSuccess)
        //    {
        //        // Pixel is ready, perform bulk transfer of the settings
        //        byte[] settingsBytes = DieSettings.ToByteArray(settings);
        //        yield return UploadBulkDataAsync(settingsBytes, onResult, onProgress);
        //    }
        //    else
        //    {
        //        onResult?.Invoke(false, waitForMsg.Error);
        //    }
        //}

        //public IEnumerator DownloadSettingsAsync(DieOperationResultHandler<DieSettings> onResult = null, DieOperationProgressHandler onProgress = null)
        //{
        //    // Request the Pixel settings
        //    var waitForMsg = new SendMessageAndWaitForResponseEnumerator<DieMessageRequestSettings, DieMessageTransferSettings>(this);
        //    yield return waitForMsg;

        //    if (waitForMsg.IsSuccess)
        //    {
        //        // Got the message, acknowledge it
        //        PostMessage(new DieMessageTransferSettingsAck());

        //        string error = null;
        //        byte[] settingsBytes = null;
        //        yield return DownloadBulkDataAsync((buf, err) => (settingsBytes, error) = (buf, err));

        //        if (settingsBytes != null)
        //        {
        //            Debug.Assert(error == null);
        //            var newSettings = DieSettings.FromByteArray(settingsBytes);

        //            // We've read the settings
        //            onResult?.Invoke(newSettings, null);
        //        }
        //        else
        //        {
        //            onResult?.Invoke(null, error);
        //        }
        //    }
        //    else
        //    {
        //        onResult?.Invoke(null, waitForMsg.Error);
        //    }
        //}
    }
}
