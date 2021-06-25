using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Animations;

public class UIGradientEditor : MonoBehaviour
{
    [Header("Controls")]
    public Button backButton;
    public Text titleText;
    public Button saveButton;
    public UIColorEditor colorEditor;
    public MultiSlider multiSlider;

    public bool isShown => gameObject.activeSelf;

    EditRGBGradient currentGradient;
    System.Action<bool, EditRGBGradient> closeAction;

    public bool isDirty => saveButton.gameObject.activeSelf;

    /// <summary>
    /// Invoke the color picker
    /// </sumary>
    public void Show(string title, EditRGBGradient previousGradient, System.Action<bool, EditRGBGradient> closeAction)
    {
        if (isShown)
        {
            Debug.LogWarning("Previous Color picker still active");
            ForceHide();
        }

        gameObject.SetActive(true);
        currentGradient = previousGradient.Duplicate();
        titleText.text = title;

        multiSlider.FromGradient(currentGradient);
        multiSlider.HandleSelected += OnHandleSelected;
		multiSlider.SelectHandle(multiSlider.AllHandles[0]);
        colorEditor.onColorSelected += OnColorSelected;

        saveButton.gameObject.SetActive(true); // Always dirty for now...

        this.closeAction = closeAction;
    }

    /// <summary>
    /// If for some reason the app needs to close the dialog box, this will do it!
    /// Normally it closes itself when you tap ok or cancel
    /// </sumary>
    public void ForceHide()
    {
        Hide(false, currentGradient);
    }

    void Awake()
    {
        backButton.onClick.AddListener(DiscardAndBack);
        saveButton.onClick.AddListener(SaveAndBack);
    }

    void Hide(bool result, EditRGBGradient gradient)
    {
        gameObject.SetActive(false);
        closeAction?.Invoke(result, gradient);
        closeAction = null;
    }

    void SaveAndBack()
    {
        Hide(true, multiSlider.ToGradient());
    }

    void DiscardAndBack()
    {
        if (isDirty)
        {
            PixelsApp.Instance.ShowDialogBox(
                "Discard Changes",
                "You have unsaved changes, are you sure you want to discard them?",
                "Discard",
                "Cancel", discard =>
                {
                    if (discard)
                    {
                        Hide(false, currentGradient);
                    }
                });
        }
        else
        {
            Hide(false, currentGradient);
        }
    }

    void OnHandleSelected(MultiSliderHandle handle)
    {
        if (handle != null)
        {
            colorEditor.SelectColor(handle.Color);
        }
        else
        {
            colorEditor.ClearColorSelection();
        }
    }

    void OnColorSelected(Color newColor)
    {
        multiSlider.ActiveHandle.ChangeColor(newColor);
    }

}
