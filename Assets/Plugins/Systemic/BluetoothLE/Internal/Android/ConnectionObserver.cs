using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Android
{
	internal sealed class ConnectionObserver : AndroidJavaProxy
	{
		NativeConnectionEventCallback _connectionEventHandler;

		public ConnectionObserver(NativeConnectionEventCallback onConnectionEventHandler)
			: base("no.nordicsemi.android.ble.observer.ConnectionObserver")
			=> _connectionEventHandler = onConnectionEventHandler;

		/// <summary>
		/// Called when the Android device started connecting to given device.
		/// The method <see cref="onDeviceConnected"/> is called when the device is connected
		/// or <see cref="onDeviceFailedToConnect"/> is called if the connection failed.
		/// </summary>
		/// <param name="device">The device that is connecting.</param>
		void onDeviceConnecting(AndroidJavaObject device)
        {
			Debug.Log($"[BLE] ConnectionObserver ==> onDeviceConnecting");
			_connectionEventHandler?.Invoke(ConnectionEvent.Connecting, ConnectionEventReason.Success);
		}

		/// <summary>
		/// Called when the device has been connected. This doesn't mean that the application may start
		/// communication. Service discovery is handled automatically after this call.
		/// </summary>
		/// <param name="device">The device that got connected.</param>
		void onDeviceConnected(AndroidJavaObject device)
        {
			Debug.Log($"[BLE] ConnectionObserver ==> onDeviceConnected");
			_connectionEventHandler?.Invoke(ConnectionEvent.Connected, ConnectionEventReason.Success);
		}

		/// <summary>
		/// Called when the device failed to connect.
		/// </summary>
		/// <param name="device">The device that failed to connect.</param>
		/// <param name="reason">The reason of failure.</param>
		void onDeviceFailedToConnect(AndroidJavaObject device, int reason)
        {
			Debug.Log($"[BLE] ConnectionObserver ==> onDeviceFailedToConnect: {(AndroidConnectionEventReason)reason}");
			_connectionEventHandler?.Invoke(ConnectionEvent.FailedToConnect, AndroidNativeInterfaceImpl.ToConnectionEventReason(reason));
		}

		/// <summary>
		/// Called when all initialization requests have completed.
		/// </summary>
		/// <param name="device">The device that is ready.</param>
		void onDeviceReady(AndroidJavaObject device)
        {
			Debug.Log($"[BLE] ConnectionObserver ==> onDeviceReady");
			_connectionEventHandler?.Invoke(ConnectionEvent.Ready, ConnectionEventReason.Success);
		}

		/// <summary>
		/// Called when the user code requested a disconnection.
		/// </summary>
		/// <param name="device">The device that gets disconnecting.</param>
		void onDeviceDisconnecting(AndroidJavaObject device)
        {
			Debug.Log($"[BLE] ConnectionObserver ==> onDeviceDisconnecting");
			_connectionEventHandler?.Invoke(ConnectionEvent.Disconnecting, ConnectionEventReason.Success);
		}

		/// <summary>
		/// Called when the device has disconnected.
		/// </summary>
		/// <param name="device">The device that got disconnected.</param>
		/// <param name="reason">The reason of the disconnection.</param>
		void onDeviceDisconnected(AndroidJavaObject device, int reason)
        {
			Debug.Log($"[BLE] ConnectionObserver ==> onDeviceDisconnected: {(AndroidConnectionEventReason)reason}");
			_connectionEventHandler?.Invoke(ConnectionEvent.Disconnected, AndroidNativeInterfaceImpl.ToConnectionEventReason(reason));
		}
	}
}
