﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Systemic.Unity.Pixels.Profiles;

public class UIBehaviorPickerBehaviorToken : MonoBehaviour
{
    // Controls
    [Header("Controls")]
    public RawImage dieRenderImage;
    public Text nameText;
    public Text descriptionText;
    public Button mainButton;
    public Image frame;

    [Header("Parameters")]
    public Color defaultTextColor;
    public Color defaultFrameColor;
    public Color selectedColor;

    public EditProfile editBehavior { get; private set; }
    public SingleDiceRenderer dieRenderer { get; private set; }
    public bool selected { get; private set; }

    public Button.ButtonClickedEvent onClick => mainButton.onClick;

    bool visible = true;

    public void Setup(EditProfile profile)
    {
        this.editBehavior = profile;
        this.dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(editBehavior.defaultPreviewSettings.design);
        if (dieRenderer != null)
        {
            dieRenderer.SetAuto(true);
            dieRenderImage.texture = dieRenderer.renderTexture;
            dieRenderer.SetAnimations(this.editBehavior.CollectAnimations());
            dieRenderer.Play(true);
        }
        nameText.text = profile.name;
        descriptionText.text = profile.description;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        this.selected = selected;
        if (selected)
        {
            nameText.color = selectedColor;
            descriptionText.color = selectedColor;
            frame.color = selectedColor;
        }
        else
        {
            nameText.color = defaultTextColor;
            descriptionText.color = defaultTextColor;
            frame.color = defaultFrameColor;
        }
    }

    void Update()
    {
        bool newVisible = GetComponent<RectTransform>().IsVisibleFrom();
        if (newVisible != visible)
        {
            visible = newVisible;
            DiceRendererManager.Instance.OnDiceRendererVisible(dieRenderer, visible);
        }
    }

    void OnDestroy()
    {
        if (this.dieRenderer != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(this.dieRenderer);
            this.dieRenderer = null;
        }
    }
}
