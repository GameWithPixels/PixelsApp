﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dice;
using Systemic.Unity.Pixels;

public class RenameDieDialog : MonoBehaviour
{
    [Header("Fields")]
    public Button renameButton;
    public Button cancelButton;
    public InputField nameField;
    public CanvasGroup canvasGroup;

    // Use this for initialization
    private void Awake()
    {
        Hide();
    }

    public void Show(Pixel dieToRename)
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;

        // Setup buttons
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() =>
        {
            Hide();
        });
    }

    public void Hide()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        renameButton.interactable = nameField.text != null && nameField.text.Length > 0;
    }
}
