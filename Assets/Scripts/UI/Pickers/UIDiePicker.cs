using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Presets;
using System.Linq;
using Dice;
using Systemic.Unity.Pixels;

public class UIDiePicker : MonoBehaviour
{
    [Header("Controls")]
    public Button backButton;
    public Text titleText;
    public RectTransform contentRoot;
    public RectTransform noPairedDice;
    public RectTransform notEnoughPairedDice;
    
    [Header("Prefabs")]
    public UIDiePickerDieToken dieTokenPrefab;

    EditDie currentDie;
    System.Action<bool, EditDie> closeAction;

    // The list of controls we have created to display dice
    readonly List<UIDiePickerDieToken> dice = new List<UIDiePickerDieToken>();

    System.Func<EditDie, bool> dieSelector;

    public bool isShown => gameObject.activeSelf;

    /// <summary>
    /// Invoke the die picker
    /// </sumary>
    public void Show(string title, EditDie previousDie, System.Func<EditDie, bool> selector, System.Action<bool, EditDie> closeAction)
    {
        if (isShown)
        {
            Debug.LogWarning("Previous Die picker still active");
            ForceHide();
        }

        dieSelector = selector;
        if (dieSelector == null)
        {
            dieSelector = d => true;
        }

        var allDice = AppDataSet.Instance.dice.Where(dieSelector);
        if (allDice.Count() > 0)
        {
            noPairedDice.gameObject.SetActive(false);
            notEnoughPairedDice.gameObject.SetActive(false);
            foreach (var dt in allDice)
            {
                // New pattern
                var newDieUI = CreateDieToken(dt);
                newDieUI.SetSelected(dt == previousDie);
                dice.Add(newDieUI);
            }
        }
        else
        {
            if (AppDataSet.Instance.dice.Count > 0)
            {
                noPairedDice.gameObject.SetActive(false);
                notEnoughPairedDice.gameObject.SetActive(true);
            }
            else
            {
                noPairedDice.gameObject.SetActive(true);
                notEnoughPairedDice.gameObject.SetActive(false);
            }
        }

        gameObject.SetActive(true);
        currentDie = previousDie;
        titleText.text = title;

        this.closeAction = closeAction;
    }

    UIDiePickerDieToken CreateDieToken(EditDie die)
    {
        // Create the gameObject
        var ret = GameObject.Instantiate<UIDiePickerDieToken>(dieTokenPrefab, contentRoot.transform);

        // Initialize it
        ret.Setup(die);

        // When we click on the pattern main button, go to the edit page
        ret.onClick.AddListener(() => Hide(true, ret.die));

        return ret;
    }

    /// <summary>
    /// If for some reason the app needs to close the dialog box, this will do it!
    /// Normally it closes itself when you tap ok or cancel
    /// </sumary>
    public void ForceHide()
    {
        Hide(false, currentDie);
    }

    void Awake()
    {
        backButton.onClick.AddListener(Back);
    }

    void Hide(bool result, EditDie die)
    {
        foreach (var uidie in dice)
        {
            DestroyDieToken(uidie);
        }
        dice.Clear();

        var closeActionCopy = closeAction;
        closeAction = null;

        gameObject.SetActive(false);
        closeActionCopy?.Invoke(result, die);
    }

    void Back()
    {
        Hide(false, currentDie);
    }

    void DestroyDieToken(UIDiePickerDieToken token)
    {
        GameObject.Destroy(token.gameObject);
    }

}
