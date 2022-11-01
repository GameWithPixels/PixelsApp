using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Dice;
using Systemic.Unity.Pixels;

public class UIScanView
    : UIPage
{
    [Header("Controls")]
    public GameObject contentRoot;
    public UIPairSelectedDiceButton pairSelectedDice;
    public Button clearListButton;

    [Header("Prefabs")]
    public UIDiscoveredDieView discoveredDiePrefab;

    List<UIDiscoveredDieView> discoveredDice = new List<UIDiscoveredDieView>();
    List<UIDiscoveredDieView> selectedDice = new List<UIDiscoveredDieView>();

    void Awake()
    {
        clearListButton.onClick.AddListener(ClearList);
    }

    void OnEnable()
    {
        base.SetupHeader(false, false, "Scanning", null);
        RefreshView();

        StartCoroutine(BeginScanCr());

        IEnumerator BeginScanCr()
        {
            while (!DiceBag.IsReady)
            {
                yield return null;
            }
            DiceBag.PixelDiscovered += OnDieDiscovered;
            DiceBag.ScanForPixels();
            pairSelectedDice.SetActive(false);
        }
    }

    void OnDisable()
    {
        DiceBag.PixelDiscovered -= OnDieDiscovered;
        DiceBag.StopScanning();
        foreach (var die in discoveredDice)
        {
            DestroyDiscoveredDie(die);
        }
        selectedDice.Clear();
        discoveredDice.Clear();
    }

    void RefreshView()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("RefreshView called on inactive UIScanView!");
            return;
        }

        // Assume all scanned dice will be destroyed
        var toDestroy = new List<UIDiscoveredDieView>(discoveredDice);
        foreach (var die in DiceBag.AvailablePixels)
        {
            if (AppDataSet.Instance.GetEditDie(die) == null)
            {
                // It's an advertising die we don't *know* about
                int prevIndex = toDestroy.FindIndex(uid => uid.die == die);
                if (prevIndex == -1)
                {
                    // New scanned die
                    var newUIDie = CreateDiscoveredDie(die);
                    discoveredDice.Add(newUIDie);
                }
                else
                {
                    toDestroy.RemoveAt(prevIndex);
                }
            }
        }

        // Remove all remaining dice
        foreach (var uidie in toDestroy)
        {
            discoveredDice.Remove(uidie);
            DestroyDiscoveredDie(uidie);
        }

        UpdateTitle(discoveredDice.Count == 0 ? "Scanning" : $"Scanning ({discoveredDice.Count} found)");
    }

    public override void OnBack()
    {
        NavigationManager.Instance.GoBack();
    }

    UIDiscoveredDieView CreateDiscoveredDie(Pixel die)
    {
        //Debug.Log("Creating discovered Die: " + die.name);
        // Create the gameObject
        var ret = GameObject.Instantiate<UIDiscoveredDieView>(discoveredDiePrefab, Vector3.zero, Quaternion.identity, contentRoot.transform);
        ret.transform.SetAsFirstSibling();
        // Initialize it
        ret.Setup(die);
        ret.onSelected += OnDieSelected;
        die.ConnectionStateChanged += OnDieStateChanged;
        return ret;
    }

    void DestroyDiscoveredDie(UIDiscoveredDieView dieView)
    {
        //Debug.Log("Destroying discovered Die: " + dieView.die.name);
        dieView.die.ConnectionStateChanged -= OnDieStateChanged;
        dieView.onSelected -= OnDieSelected;
        GameObject.Destroy(dieView.gameObject);
    }

    void PairSelectedDice()
    {
        pairSelectedDice.onClick.RemoveListener(PairSelectedDice);
        pairSelectedDice.SetActive(false);
        PixelsApp.Instance.AddDiscoveredDice(selectedDice.Select(d => d.die)); //TODO d.die might be null
        // Tell the navigation to go back to the pool, and then start connecting to the selected dice
        NavigationManager.Instance.GoBack();
    }

    void OnDieDiscovered(Pixel newDie)
    {
        RefreshView();
    }

    void OnDieSelected(UIDiscoveredDieView uidie, bool selected)
    {
        if (selected)
        {
            if (selectedDice.Count == 0)
            {
                pairSelectedDice.onClick.AddListener(PairSelectedDice);
                pairSelectedDice.SetActive(true);
            }
            selectedDice.Add(uidie);
        }
        else
        {
            selectedDice.Remove(uidie);
            if (selectedDice.Count == 0)
            {
                pairSelectedDice.onClick.RemoveListener(PairSelectedDice);
                pairSelectedDice.SetActive(false);
            }
        }
    }

    // void onWillDestroyDie(Die die)
    // {
    //     var uidie = discoveredDice.Find(d => d.die == die);
    //     if (uidie != null)
    //     {
    //         die.OnConnectionStateChanged -= OnDieStateChanged;
    //         Debug.Assert(die.connectionState == ConnectionState.New); // if not we should have been notified previously
    //         discoveredDice.Remove(uidie);
    //         DestroyDiscoveredDie(uidie);
    //     }
    // }

    void OnDieStateChanged(Pixel die, PixelConnectionState oldState, PixelConnectionState newState)
    {
        RefreshView();
    }

    void ClearList()
    {
        DiceBag.ClearAvailablePixels();
        RefreshView();
    }
}
