package com.systemic.bluetoothle;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothManager;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

/**
 * @brief Static class that notifies off Bluetooth adapter state changes.
 *
 */
public final class BluetoothState
{
    private static final String TAG = "SystemicGames";

    private static StateCallback _callback = null;

    private static final BroadcastReceiver _receiver = new BroadcastReceiver()
    {
        @Override
        public void onReceive(Context context, Intent intent)
        {
            final String action = intent.getAction();
            if ((_callback != null) && action.equals(BluetoothAdapter.ACTION_STATE_CHANGED))
            {
                final int state = intent.getIntExtra(BluetoothAdapter.EXTRA_STATE, BluetoothAdapter.ERROR);
                if (state != 0)
                {
                    switch (state)
                    {
                    case BluetoothAdapter.STATE_OFF:
                        Log.v(TAG, "Bluetooth off");
                        break;
                    case BluetoothAdapter.STATE_TURNING_OFF:
                        Log.v(TAG, "Turning Bluetooth off...");
                        break;
                    case BluetoothAdapter.STATE_ON:
                        Log.v(TAG, "Bluetooth on");
                        break;
                    case BluetoothAdapter.STATE_TURNING_ON:
                        Log.v(TAG, "Turning Bluetooth on...");
                        break;
                    }
                    _callback.onStateChanged(state);
                }
            }
        }
    };

    /**
     * @brief Interface for MTU change request callbacks.
     */
	public interface StateCallback
    {
        public void onStateChanged(int state);
    }

    /**
     * @brief Starts monitoring for Bluetooth adapter state changes.
     *
     * @param callback The callback for notifying of state changes.
     */
    public static void Start(final StateCallback callback)
    {
        if (callback == null)
        {
            throw new IllegalArgumentException("callback is null");
        }

        _callback = callback;
        
        Context appContext = UnityPlayer.currentActivity.getApplicationContext();
        // Register for broadcasts on BluetoothAdapter state change
        IntentFilter filter = new IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED);
        appContext.registerReceiver(_receiver, filter);
    }

    /**
     * @brief Stops monitoring for Bluetooth adapter state changes.
     */
    public static void Stop()
    {
        _callback = null;
        Context appContext = UnityPlayer.currentActivity.getApplicationContext();
        appContext.unregisterReceiver(_receiver);
    }

    /**
     * @brief Gets the Bluetooth adapter state.
     */
    public static int GetState()
    {
        Context appContext = UnityPlayer.currentActivity.getApplicationContext();
        BluetoothManager bluetoothManager = (BluetoothManager)(appContext.getSystemService(Context.BLUETOOTH_SERVICE));
        BluetoothAdapter adapter = bluetoothManager.getAdapter();
        return adapter.getState();
    }
}