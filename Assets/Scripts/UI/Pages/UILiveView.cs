using System.Collections;
using System.Collections.Generic;
using Systemic.Unity.Pixels;
using UnityEngine;

public class UILiveView
    : UIPage
{
    [Header("Controls")]
    public Transform contentRoot;

    [Header("Prefabs")]
    public UILiveDieEntry dieEntryPrefab;

    // The list of entries we have created to display behaviors
    readonly List<UILiveDieEntry> entries = new List<UILiveDieEntry>();

    // Dice list
    readonly List<Pixel> watchedDice = new List<Pixel>();

    public override void Enter(object context)
    {
        base.Enter(context);
        watchedDice.Clear();
        PixelsApp.Instance.ConnectAllDice(gameObject, editDie => watchedDice.Add(editDie.die));
    }

    public override void Leave()
    {
        base.Leave();
        watchedDice.ForEach(d => DiceBag.Instance.DisconnectPixel(d));
        watchedDice.Clear();
    }

    void OnEnable()
    {
        base.SetupHeader(true, false, "Live View", null);
    }

    void OnDisable()
    {
        if (AppDataSet.Instance != null) // When quiting the app, it may be null
        {
            foreach (var uientry in entries)
            {
                GameObject.Destroy(uientry.gameObject);
            }
            entries.Clear();
        }
    }

    UILiveDieEntry CreateEntry(EditDie die, int roll)
    {
        // Create the gameObject
        var ret = GameObject.Instantiate<UILiveDieEntry>(dieEntryPrefab, Vector3.zero, Quaternion.identity, contentRoot.transform);

        // Initialize it
        ret.Setup(die, roll);
        return ret;
    }

}
