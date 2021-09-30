using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Presets;
using Dice;
using System.Linq;
using Systemic.Unity.Pixels;

public class ActiveBehaviorMonitor : MonoBehaviour
{
    List<EditDie> connectedDice = new List<EditDie>();

    // Start is called before the first frame update
    void Awake()
    {
        PixelsApp.Instance.onDieBehaviorUpdatedEvent += OnBehaviorDownloadedEvent;
    }

    void OnBehaviorDownloadedEvent(EditDie editDie, EditBehavior behavior)
    {
        // Check whether we should stay connected to some of the dice
        var toDisconnect = new List<EditDie>(connectedDice);
        if (behavior.CollectAudioClips().Any())
        {
            // This die assignment uses a behavior that has audio clips, so stay connected to the die
            if (connectedDice.Contains(editDie))
            {
                toDisconnect.Remove(editDie);
            }
            else if (editDie != null)
            {
                // Connect to the new die
                connectedDice.Add(editDie);
                PixelsApp.Instance.ConnectDie(editDie, gameObject, onFailed: (_, err) => connectedDice.Remove(editDie));
            }
        }

        foreach (var d in toDisconnect)
        {
            connectedDice.Remove(d);
            DiceBag.Instance.DisconnectPixel(d.die);
        }
    }
}
