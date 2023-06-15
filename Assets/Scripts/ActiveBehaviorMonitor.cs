using System.Collections.Generic;
using System.Linq;
using Systemic.Unity.Pixels;
using UnityEngine;

public class ActiveBehaviorMonitor : MonoBehaviour
{
    // List of "always connected" dice
    HashSet<EditDie> connectedDice = new HashSet<EditDie>();

    // Start is called before the first frame update
    void Awake()
    {
        DiceBag.BluetoothStatusChanged += (status) =>
        {
            if (status == Systemic.Unity.BluetoothLE.BluetoothStatus.Ready)
            {
                Refresh();
            }
        };
        PixelsApp.Instance.onDieProfileUpdatedEvent += (editDie) =>
        {
            if (!AddAndConnectIfRequired(editDie))
            {
                Remove(editDie, "no longer has a profile with audio", true);
            }
        };
    }

    void Update()
    {
        foreach (var pixel in DiceBag.ConnectedPixels.ToArray())
        {
            if (pixel.connectionState == PixelConnectionState.Ready)
            {
                var editDie = AppDataSet.Instance.dice.FirstOrDefault(d => d.die == pixel);
                if (editDie != null)
                {
                    AddAndConnectIfRequired(editDie);
                }
            }
        }
    }

    void Refresh()
    {
        var toDisconnect = new List<EditDie>(connectedDice);

        // Check each die
        foreach (var editDie in AppDataSet.Instance.dice)
        {
            if (AddAndConnectIfRequired(editDie))
            {
                toDisconnect.Remove(editDie);
            }
        }

        // Disconnect other dice
        foreach (var editDie in toDisconnect)
        {
            if (connectedDice.Contains(editDie))
            {
                Remove(editDie, "no longer has a profile with audio", true);
            }
        }
    }

    bool AddAndConnectIfRequired(EditDie editDie)
    {
        bool profileWithAudio = editDie.currentBehavior?.CollectAudioClips().Any() ?? false;
        if (profileWithAudio && (!connectedDice.Contains(editDie)))
        {
            connectedDice.Add(editDie);
            Debug.Log($"Attempting to connect to {editDie.name} because it has a profile with audio");
            PixelsApp.Instance.ConnectDie(editDie, gameObject, onConnected:(_) =>
            {
                void onStateChanged(Pixel die, PixelConnectionState state)
                {
                    if (state != PixelConnectionState.Ready)
                    {
                        die.ConnectionStateChanged -= onStateChanged;
                        Remove(editDie, "disconnected");
                    }
                };
                editDie.die.ConnectionStateChanged += onStateChanged;
            },
            onFailed: (_, __) =>
            {
                Remove(editDie, "failed to connect");
            });
        }
        return profileWithAudio;
    }

     void Remove(EditDie editDie, string reason, bool disconnect = false)
     {
        if (connectedDice.Contains(editDie))
        {
            Debug.Log($"Removing {editDie.name} from always connected list because it {reason}");
            connectedDice.Remove(editDie);
            if (disconnect)
            {
                DiceBag.DisconnectPixel(editDie.die);
            }
        }
     }
}
