using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Dice;
using Systemic.Unity.Pixels;

public class UIDicePoolView
    : UIPage
{
    [Header("Controls")]
    public GameObject contentRoot;
    public Button addNewDiceButton;
    public UIDicePoolRefreshButton refreshButton;

    [Header("Prefabs")]
    public UIPairedDieToken pairedDieViewPrefab;

    // The list of controls we have created to display die status
    List<UIPairedDieToken> pairedDice = new List<UIPairedDieToken>();

    IEnumerator connectAllDiceCoroutine;
    List<EditDie> connectedDice = new List<EditDie>();

    void Awake()
    {
        addNewDiceButton.onClick.AddListener(AddNewDice);
        refreshButton.onClick.AddListener(ForceRefresh);
    }

    public override void Enter(object context)
    {
        base.Enter(context);

        // Connect to all the dice in the pool if possible
        connectAllDiceCoroutine = ConnectAllDice();
        StartCoroutine(connectAllDiceCoroutine);
    }

    public override void Leave()
    {
        base.Leave();
        if (connectAllDiceCoroutine != null)
        {
            StopCoroutine(connectAllDiceCoroutine);
            ((System.IDisposable)connectAllDiceCoroutine).Dispose(); // This will make sure the finally {} block is run
            connectAllDiceCoroutine = null;
        }
        foreach (var editDie in connectedDice)
        {
            DiceBag.DisconnectPixel(editDie.die);
        }
        connectedDice.Clear();
    }

    void OnEnable()
    {
        base.SetupHeader(true, false, "Dice Bag", null);
        RefreshView();

        PixelsApp.Instance.onDieAdded += OnDieAdded;
        PixelsApp.Instance.onWillRemoveDie += OnWillRemoveDie;
    }

    void OnDisable()
    {
        if (PixelsApp.Instance)
        {
            PixelsApp.Instance.onDieAdded -= OnDieAdded;
            PixelsApp.Instance.onWillRemoveDie -= OnWillRemoveDie;
        }

        foreach (var uidie in pairedDice)
        {
            DestroyPairedDie(uidie);
        }
        pairedDice.Clear();
    }

    UIPairedDieToken CreatePairedDie(EditDie die)
    {
        // Create the gameObject
        var ret = GameObject.Instantiate<UIPairedDieToken>(pairedDieViewPrefab, Vector3.zero, Quaternion.identity, contentRoot.transform);
        ret.transform.SetAsFirstSibling();
        // Initialize it
        ret.Setup(die);
        return ret;
    }

    void DestroyPairedDie(UIPairedDieToken die)
    {
        GameObject.Destroy(die.gameObject);
    }

    void AddNewDice()
    {
        NavigationManager.Instance.GoToPage(UIPage.PageId.DicePoolScanning, null);
    }

    void RefreshView()
    {
        // Assume all pool dice will be destroyed
        var toDestroy = new List<UIPairedDieToken>(pairedDice);
        foreach (var editDie in AppDataSet.Instance.dice)
        {
            int prevIndex = toDestroy.FindIndex(uid => uid.die == editDie);
            if (prevIndex == -1)
            {
                // New scanned die
                var newUIDie = CreatePairedDie(editDie);
                pairedDice.Add(newUIDie);
            }
            else
            {
                // Previous die is still advertising, good
                toDestroy.RemoveAt(prevIndex);
            }
        }

        // Remove all remaining dice
        foreach (var uidie in toDestroy)
        {
            pairedDice.Remove(uidie);
            DestroyPairedDie(uidie);
        }
    }

    void OnDieAdded(EditDie editDie)
    {
        if (!connectedDice.Contains(editDie))
        {
            connectedDice.Add(editDie);
            PixelsApp.Instance.ConnectDie(editDie, gameObject);
        }
        RefreshView();
    }

    void OnWillRemoveDie(EditDie editDie)
    {
        connectedDice.Remove(editDie);
        var ui = pairedDice.FirstOrDefault(uid => uid.die == editDie);
        if (ui != null)
        {
            pairedDice.Remove(ui);
            DestroyPairedDie(ui);
        }
    }

    void OnBeginRefreshPool()
    {
        refreshButton.StartRotating();
    }

    IEnumerator ConnectAllDice()
    {
        try
        {
            OnBeginRefreshPool();
            yield return PixelsApp.Instance.ConnectAllDice(gameObject, editDie => connectedDice.Add(editDie));
            RefreshView();
        }
        finally
        {
            OnEndRefreshPool();
        }
        connectAllDiceCoroutine = null;
    }

    void OnEndRefreshPool()
    {
        if (refreshButton.rotating)
        {
            refreshButton.StopRotating();
        }
    }

    void ForceRefresh()
    {
        if (connectAllDiceCoroutine == null)
        {
            // Connect to all the dice in the pool if possible
            connectAllDiceCoroutine = ConnectAllDice();
            StartCoroutine(connectAllDiceCoroutine);
        }
    }
}
