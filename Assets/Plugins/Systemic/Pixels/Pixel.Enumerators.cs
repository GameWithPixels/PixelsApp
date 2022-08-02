using System.Collections;
using Systemic.Unity.Pixels.Messages;
using UnityEngine;

namespace Systemic.Unity.Pixels
{
    partial class Pixel
    {
        // In this file you'll find several enumerators to run asynchronous communication operations with a Pixel

        /// <summary>
        /// Interface for an enumerator handling an asynchronous operation with a Pixel.
        /// 
        /// Instances are meant to be run as coroutines.
        /// </summary>
        protected interface IOperationEnumerator : IEnumerator
        {
            /// <summary>
            /// Indicates whether the operation has completed (successfully or not).
            /// </summary>
            bool IsDone { get; }

            /// <summary>
            /// Indicates whether the operation was successful.
            /// </summary>
            bool IsSuccess { get; }

            /// <summary>
            /// Indicates whether the operation has timed-out.
            /// </summary>
            bool IsTimeout { get; }

            /// <summary>
            /// Gets the error string or null if there was no error.
            /// </summary>
            string Error { get; }
        }

        /// <summary>
        /// Enumerator that waits until the Pixel receives a message of the given type.
        /// 
        /// Instances are meant to be run as coroutines.
        /// </summary>
        /// <typeparam name="T">The type of message we are waiting for.</typeparam>
        protected class WaitForMessageEnumerator<T> : IOperationEnumerator
            where T : IPixelMessage, new()
        {
            // The enum value for the message type
            readonly MessageType _msgType;

            // Time at which the operation will time out
            readonly float _timeout;

            // Whether the operation has started
            bool _isStarted;

            /// <summary>
            /// Indicates whether the operation has completed (successfully or not).
            /// </summary>
            public bool IsDone => IsSuccess || (Error != null);

            /// <summary>
            /// Indicates whether the operation was successful.
            /// </summary>
            public bool IsSuccess => Message != null;

            /// <summary>
            /// Indicates whether the operation has timed-out.
            /// </summary>
            public bool IsTimeout { get; protected set; }

            /// <summary>
            /// Gets the request status as a string or null if there was no error.
            /// </summary>
            public string Error { get; protected set; }

            /// <summary>
            /// Gets the received message object or null if none.
            /// </summary>
            public T Message { get; private set; }

            /// <summary>
            /// Gets the current object, always null.
            /// </summary>
            public object Current => null;

            /// <summary>
            /// Gets the Pixel with which the operation is run.
            /// </summary>
            protected Pixel Pixel { get; }

            /// <summary>
            /// Indicates whether this instance was disposed.
            /// </summary>
            protected bool IsDisposed { get; private set; }

            /// <summary>
            /// Initializes an enumerator waiting to receive a message of a specific type from the given Pixel.
            /// </summary>
            /// <param name="pixel">The Pixel to work with.</param>
            /// <param name="timeoutSec">Maximum number of seconds to wait to get the message.</param>
            public WaitForMessageEnumerator(Pixel pixel, float timeoutSec = AckMessageTimeout)
            {
                if (timeoutSec <= 0) throw new System.ArgumentException("Timeout value must be greater than zero", nameof(timeoutSec));
                if (pixel == null) throw new System.ArgumentNullException(nameof(pixel));

                Pixel = pixel;
                _timeout = Time.realtimeSinceStartup + timeoutSec;
                _msgType = Marshaling.GetMessageType<T>();
            }

            /// <summary>
            /// Run the operation.
            /// </summary>
            /// <returns>Indicates whether the request is still running.</returns>
            public virtual bool MoveNext()
            {
                if (IsDisposed) throw new System.ObjectDisposedException(nameof(WaitForMessageEnumerator<T>));

                // Subscribe to our response message on first call
                if (!_isStarted)
                {
                    _isStarted = true;
                    Pixel.AddMessageHandler(_msgType, OnMessage);
                }

                if ((!IsSuccess) && (_timeout > 0) && (Error == null))
                {
                    // Update timeout
                    if (IsTimeout = (Time.realtimeSinceStartupAsDouble > _timeout))
                    {
                        Error = $"Timeout while waiting for message of type {typeof(T)}";
                    }
                }

                // Error might be set by child class
                bool done = IsSuccess || IsTimeout || (Error != null);
                if (done)
                {
                    // Unsubscribe from message notifications
                    Pixel.RemoveMessageHandler(_msgType, OnMessage);

                    if (IsSuccess)
                    {
                        if (Error != null)
                        {
                            // Some error occurred, we might have got an old message
                            // Forget message, this will make IsSuccess return false
                            Message = default;
                        }
                    }
                    else if (Error == null)
                    {
                        // Operation failed
                        Error = $"Unknown error while waiting for message of type {typeof(T)}";
                    }
                }
                return !done;
            }

            /// <summary>
            /// Not supported.
            /// </summary>
            public void Reset()
            {
            }

            // Store the received message and unhook itself from the Pixel notification
            void OnMessage(IPixelMessage msg)
            {
                Debug.Assert(msg is T);
                Message = (T)msg;
                Pixel.RemoveMessageHandler(_msgType, OnMessage);
            }
        }

        /// <summary>
        /// Enumerator that first sends a message to a Pixel and then waits for a message of the given type
        /// as the response.
        /// 
        /// It's possible to get the response before the message is send if the Pixel was already about
        /// to send a message of the expected type.
        /// Instances are meant to be run as coroutines.
        /// </summary>
        /// <typeparam name="TMsg">The type of message to send.</typeparam>
        /// <typeparam name="TResp">The type of message we are waiting for.</typeparam>
        protected class SendMessageAndWaitForResponseEnumerator<TMsg, TResp> : WaitForMessageEnumerator<TResp>
            where TMsg : IPixelMessage, new()
            where TResp : IPixelMessage, new()
        {
            // The enumerator used to send the message
            IOperationEnumerator _sendMessage;

            // The message type
            readonly System.Type _msgType;

            /// <summary>
            /// Initializes an enumerator that first sends a message to the given Pixel and then waits for a message
            /// of <see cref="TResp"/> type as the response.
            /// </summary>
            /// <param name="pixel">The Pixel to work with.</param>
            /// <param name="message">The message to send.</param>
            /// <param name="timeoutSec">Maximum number of seconds to wait to get the message.</param>
            public SendMessageAndWaitForResponseEnumerator(Pixel pixel, TMsg message, float timeoutSec = AckMessageTimeout)
                : base(pixel, timeoutSec)
            {
                if (message == null) throw new System.ArgumentNullException(nameof(message));

                _msgType = message.GetType();
                _sendMessage = Pixel.SendMessageAsync(Marshaling.ToByteArray(message), timeoutSec);
            }

            /// <summary>
            /// Initializes an enumerator that first sends a default initialized message to the given Pixel
            /// and then waits for a specific message type as the response.
            ///
            /// The message send is a default instance of the <see cref="TMsg"/> type.
            /// </summary>
            /// <param name="pixel">The Pixel to work with.</param>
            /// <param name="timeoutSec">Maximum number of seconds to wait to get the message.</param>
            public SendMessageAndWaitForResponseEnumerator(Pixel pixel, float timeoutSec = AckMessageTimeout)
                : this(pixel, new TMsg(), timeoutSec)
            {
            }

            /// <summary>
            /// Run the operation.
            /// </summary>
            /// <returns>Indicates whether the request is still running.</returns>
            public override bool MoveNext()
            {
                if (IsDisposed) throw new System.ObjectDisposedException(nameof(SendMessageAndWaitForResponseEnumerator<TMsg, TResp>));

                // First send a message
                if ((_sendMessage != null) && (!_sendMessage.MoveNext()))
                {
                    // And check for success
                    if (!_sendMessage.IsSuccess)
                    {
                        if (_sendMessage.IsTimeout)
                        {
                            // Timeout sending message, we stop here (the call to base.MoveNext() will return false now)
                            IsTimeout = true;
                            Error = $"Timeout while sending message of type {typeof(TMsg).Name}";
                        }
                        else
                        {
                            // Done sending message
                            Error = $"Failed to send message of type {typeof(TMsg).Name}, {_sendMessage.Error}";
                        }
                    }
                    _sendMessage = null;
                }

                return base.MoveNext();
            }
        }

        /// <summary>
        /// Enumerator that first sends a message to a Pixel, then waits for a message of the given type
        /// as the response and pass it to a user callback.
        /// 
        /// It's possible to get the response before the message is send if the Pixel was already about
        /// to send a message of the expected type.
        /// Instances are meant to be run as coroutines.
        /// </summary>
        /// <typeparam name="TMsg">The type of message to send.</typeparam>
        /// <typeparam name="TResp">The type of message we are waiting for.</typeparam>
        protected class SendMessageAndProcessResponseEnumerator<TMsg, TResp> : SendMessageAndWaitForResponseEnumerator<TMsg, TResp>
            where TMsg : IPixelMessage, new()
            where TResp : IPixelMessage, new()
        {
            // The callback to run when getting a response
            readonly System.Action<TResp> _onResponse;

            /// <summary>
            /// Initializes an enumerator that first sends a message to the given Pixel, then waits for a specific
            /// message type as the response and pass it to the given callback.
            /// </summary>
            /// <param name="pixel">The Pixel to work with.</param>
            /// <param name="message">The message to send.</param>
            /// <param name="onResponse">The callback to run when getting a response.</param>
            /// <param name="timeoutSec">Maximum number of seconds to wait to get the message.</param>
            public SendMessageAndProcessResponseEnumerator(Pixel pixel, TMsg message, System.Action<TResp> onResponse, float timeoutSec = AckMessageTimeout)
               : base(pixel, message, timeoutSec)
            {
                _onResponse = onResponse ?? throw new System.ArgumentNullException(nameof(onResponse));
            }

            /// <summary>
            /// Initializes an enumerator that first sends a default initialized message to the given Pixel,
            /// then waits for a specific message type as the response and pass it to the given callback.
            ///
            /// The message send is a default instance of the <see cref="TMsg"/> type.
            /// </summary>
            /// <param name="pixel">The Pixel to work with.</param>
            /// <param name="onResponse">The callback to run when getting a response.</param>
            /// <param name="timeoutSec">Maximum number of seconds to wait to get the message.</param>
            public SendMessageAndProcessResponseEnumerator(Pixel pixel, System.Action<TResp> onResponse, float timeoutSec = AckMessageTimeout)
               : this(pixel, new TMsg(), onResponse, timeoutSec)
            {
            }

            /// <summary>
            /// Processes the request.
            /// </summary>
            /// <returns>Indicates whether the request is still running.</returns>
            public override bool MoveNext()
            {
                bool result = base.MoveNext();
                if (IsSuccess)
                {
                    _onResponse(Message);
                }
                return result;
            }
        }

        /// <summary>
        /// Enumerator that first sends a message to a Pixel, then waits for a message of the given type
        /// as the response and pass it to a user callback.
        /// The value returned by the callback is stored in the <see cref="Value"/> property.
        /// 
        /// It's possible to get the response before the message is send if the Pixel was already about
        /// to send a message of the expected type.
        /// Instances are meant to be run as coroutines.
        /// </summary>
        /// <typeparam name="TMsg">The type of message to send.</typeparam>
        /// <typeparam name="TResp">The type of message we are waiting for.</typeparam>
        /// <typeparam name="TValue">The type of the value extracted from the response message.</typeparam>
        protected class SendMessageAndProcessResponseWithValueEnumerator<TMsg, TResp, TValue> : SendMessageAndWaitForResponseEnumerator<TMsg, TResp>
            where TMsg : IPixelMessage, new()
            where TResp : IPixelMessage, new()
        {
            // The callback to run when getting a response
            readonly System.Func<TResp, TValue> _onResponse;

            /// <summary>
            /// 
            /// </summary>
            public TValue Value { get; private set; }

            /// <summary>
            /// Initializes an enumerator that first sends a message to the given Pixel, then waits for a specific
            /// message type as the response and pass it to the given callback.
            /// The value returned by the callback is stored in the <see cref="Value"/> property.
            /// </summary>
            /// <param name="pixel">The Pixel to work with.</param>
            /// <param name="message">The message to send.</param>
            /// <param name="onResponse">The callback to run when getting a response.</param>
            /// <param name="timeoutSec">Maximum number of seconds to wait to get the message.</param>
            public SendMessageAndProcessResponseWithValueEnumerator(Pixel pixel, TMsg message, System.Func<TResp, TValue> onResponse, float timeoutSec = AckMessageTimeout)
               : base(pixel, message, timeoutSec)
            {
                _onResponse = onResponse ?? throw new System.ArgumentNullException(nameof(onResponse)); ;
            }

            /// <summary>
            /// Initializes an enumerator that first sends a default initialized message to the given Pixel,
            /// then waits for a specific message type as the response and pass it to the given callback.
            /// The value returned by the callback is stored in the <see cref="Value"/> property.
            ///
            /// The message send is a default instance of the <see cref="TMsg"/> type.
            /// </summary>
            /// <param name="pixel">The Pixel to work with.</param>
            /// <param name="onResponse">The callback to run when getting a response.</param>
            /// <param name="timeoutSec">Maximum number of seconds to wait to get the message.</param>
            public SendMessageAndProcessResponseWithValueEnumerator(Pixel pixel, System.Func<TResp, TValue> onResponse, float timeoutSec = AckMessageTimeout)
                : this(pixel, new TMsg(), onResponse, timeoutSec)
            {
            }

            /// <summary>
            /// Processes the request.
            /// </summary>
            /// <returns>Indicates whether the request is still running.</returns>
            public override bool MoveNext()
            {
                bool result = base.MoveNext();
                if (IsSuccess)
                {
                    Value = _onResponse(Message);
                }
                return result;
            }
        }
    }
}
