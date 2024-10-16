﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple modal dialog box, where you can set the text and ok/cancel buttons
/// </sumary>
public class UIDialogBox : MonoBehaviour
{
    [Header("Controls")]
    public Button okButton;
    public Button cancelButton;
    public Text titleText;
    public Text messageText;
    public Text cancelText;
    public Text okText;

    public bool isShown { get; private set; } = false;

    System.Action<bool> closeAction;

    /// <summary>
    /// Invoke the modal dialog box, passing in all the parameters to configure it and a callback
    /// </sumary>
    public void Show(string title, string message, string okMessage = "Ok", string cancelMessage = null, System.Action<bool> closeAction = null)
    {
        Debug.Log($"Showing dialog: title={title}, message={message}, okMessage={okMessage}, cancelMessage={cancelMessage}, hasAction={closeAction != null}");

        if (isShown)
        {
            Debug.LogWarning("Previous Message box still active");
            ForceHide();
        }

        isShown = true;

        gameObject.SetActive(true);
        if (string.IsNullOrEmpty(cancelMessage))
        {
            // No cancel button
            cancelButton.gameObject.SetActive(false);
        }
        else
        {
            cancelButton.gameObject.SetActive(true);
            cancelText.text = cancelMessage;
        }

        Debug.Assert(!string.IsNullOrEmpty(okMessage));
        okText.text = okMessage;

        titleText.text = title;
        messageText.text = message;

        this.closeAction = closeAction;
    }

    void Awake()
    {
        cancelButton.onClick.AddListener(() => Hide(false));
        okButton.onClick.AddListener(() => Hide(true));
    }

    /// <summary>
    /// If for some reason the app needs to close the dialog box, this will do it!
    /// Normally it closes itself when you tap ok or cancel
    /// </sumary>
    public void ForceHide()
    {
        Hide(false);
    }

    void Hide(bool result)
    {
        Debug.Log($"Hiding dialog: title={titleText.text}, message={messageText.text}, result={result}");

        gameObject.SetActive(false);
        isShown = false;
        var closeActionCopy = closeAction;
        closeAction = null;
        closeActionCopy?.Invoke(result);
    }
}
