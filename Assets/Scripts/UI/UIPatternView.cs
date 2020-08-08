﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPatternView
    : PixelsApp.Page
{
    [Header("Controls")]
    public Button backButton;
    public InputField animationNameText;
    public Button menuButton;
    public RawImage previewImage;
    public UIParameterEnum animationSelector;
    public RectTransform parametersRoot;

    public Animations.EditAnimation editAnimation { get; private set; }
    public DiceRenderer dieRenderer { get; private set; }

    public override void Enter(object context)
    {
        base.Enter(context);
        var anim = context as Animations.EditAnimation;
        if (anim != null)
        {
            Setup(anim);
        }
    }

    void OnEnable()
    {
    }

    void OnDisable()
    {
        if (DiceRendererManager.Instance != null && this.dieRenderer != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(this.dieRenderer);
            this.dieRenderer = null;
        }

        if (UIParameterManager.Instance != null && editAnimation != null)
        {
            UIParameterManager.Instance.DestroyControls(editAnimation);
        }
    }

    void Setup(Animations.EditAnimation anim)
    {
        editAnimation = anim;
        this.dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(anim.defaultPreviewSettings.design, 600);
        if (dieRenderer != null)
        {
            previewImage.texture = dieRenderer.renderTexture;
        }
        animationNameText.text = anim.name;

        animationSelector.Setup("Animation Type", () => editAnimation.type, (t) => SetAnimationType((Animations.AnimationType)t));

        // Setup all other parameters
        var paramList = UIParameterManager.Instance.CreateControls(anim, parametersRoot);
        paramList.onParameterChanged += OnAnimParameterChanged;

        dieRenderer.rotating = true;
        dieRenderer.SetAnimation(anim);
        dieRenderer.Play(true);
    }

    void Awake()
    {
        backButton.onClick.AddListener(SaveAndGoBack);
        animationNameText.onEndEdit.AddListener(newName => editAnimation.name = newName);
    }

    void SaveAndGoBack()
    {
        AppDataSet.Instance.SaveData(); // Not sure about this one!
        NavigationManager.Instance.GoBack();
    }

    void OnAnimParameterChanged(object animObject, UIParameter parameter, object newValue)
    {
        var theEditAnim = (Animations.EditAnimation)animObject;
        Debug.Assert(theEditAnim == editAnimation);
        dieRenderer.SetAnimation(theEditAnim);
    }

    void SetAnimationType(Animations.AnimationType newType)
    {
        if (newType != editAnimation.type)
        {
            // Change the type, which really means create a new animation and replace the old one
            var newEditAnimation = Animations.EditAnimation.Create(newType);

            // Copy over the few things we can
            newEditAnimation.duration = editAnimation.duration;
            newEditAnimation.name = editAnimation.name;
            newEditAnimation.defaultPreviewSettings = editAnimation.defaultPreviewSettings;

            // Replace the animation
            AppDataSet.Instance.ReplaceAnimation(editAnimation, newEditAnimation);

            // Setup the parameters again
            UIParameterManager.Instance.DestroyControls(editAnimation);

            var paramList = UIParameterManager.Instance.CreateControls(newEditAnimation, parametersRoot);
            paramList.onParameterChanged += OnAnimParameterChanged;

            dieRenderer.rotating = true;
            dieRenderer.SetAnimation(newEditAnimation);

            editAnimation = newEditAnimation;
        }
    }
}