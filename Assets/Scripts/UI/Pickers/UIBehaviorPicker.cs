using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Systemic.Unity.Pixels.Profiles;

public class UIBehaviorPicker : MonoBehaviour
{
    [Header("Controls")]
    public Button backButton;
    public Text titleText;
    public RectTransform contentRoot;

    [Header("Prefabs")]
    public UIBehaviorPickerBehaviorToken behaviorTokenPrefab;

    EditProfile currentBehavior;
    System.Action<bool, EditProfile> closeAction;

    // The list of controls we have created to display dice
    List<UIBehaviorPickerBehaviorToken> behaviors = new List<UIBehaviorPickerBehaviorToken>();

    public bool isShown => gameObject.activeSelf;

    /// <summary>
    /// Invoke the die picker
    /// </sumary>
    public void Show(string title, EditProfile previousBehavior, System.Action<bool, EditProfile> closeAction)
    {
        if (isShown)
        {
            Debug.LogWarning("Previous Behavior picker still active");
            ForceHide();
        }

        foreach (var behavior in AppDataSet.Instance.profiles)
        {
            // New pattern
            var newBehaviorUI = CreateBehaviorToken(behavior);
            newBehaviorUI.SetSelected(behavior == previousBehavior);
            behaviors.Add(newBehaviorUI);
        }

        gameObject.SetActive(true);
        currentBehavior = previousBehavior;
        titleText.text = title;

        this.closeAction = closeAction;
    }

    UIBehaviorPickerBehaviorToken CreateBehaviorToken(EditProfile profile)
    {
        // Create the gameObject
        var ret = GameObject.Instantiate<UIBehaviorPickerBehaviorToken>(behaviorTokenPrefab, contentRoot.transform);

        // When we click on the pattern main button, go to the edit page
        ret.onClick.AddListener(() => Hide(true, ret.editBehavior));

        // Initialize it
        ret.Setup(profile);
        return ret;
    }

    /// <summary>
    /// If for some reason the app needs to close the dialog box, this will do it!
    /// Normally it closes itself when you tap ok or cancel
    /// </sumary>
    public void ForceHide()
    {
        Hide(false, currentBehavior);
    }

    void Awake()
    {
        backButton.onClick.AddListener(Back);
    }

    void Hide(bool result, EditProfile profile)
    {
        foreach (var uibehavior in behaviors)
        {
            DestroyBehaviorToken(uibehavior);
        }
        behaviors.Clear();

        gameObject.SetActive(false);
        closeAction?.Invoke(result, profile);
        closeAction = null;
    }

    void Back()
    {
        Hide(false, currentBehavior);
    }

    void DestroyBehaviorToken(UIBehaviorPickerBehaviorToken token)
    {
        GameObject.Destroy(token.gameObject);
    }
}
