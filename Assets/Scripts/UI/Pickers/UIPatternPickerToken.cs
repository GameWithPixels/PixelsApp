﻿using Systemic.Unity.Pixels;
using UnityEngine;
using UnityEngine.UI;

public class UIPatternPickerToken : MonoBehaviour
{
    [Header("Controls")]
    public Button mainButton;
    public RawImage animRenderImage;
    public RawImage textureImage;
    public Text animNameText;

    public EditPattern editPattern { get; private set; }
    public SingleDiceRenderer dieRenderer { get; private set; }

    public Button.ButtonClickedEvent onClick => mainButton.onClick;

    bool visible = true;

    public void Setup(EditPattern pattern)
    {
        this.editPattern = pattern;
        this.dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(PixelDesignAndColor.V5Black);
        if (dieRenderer != null)
        {
            animRenderImage.texture = dieRenderer.renderTexture;
        }
        animNameText.text = pattern.name;

        var anim = new EditAnimationKeyframed();
        anim.name = "temp anim";
        anim.pattern = pattern;
        anim.duration = pattern.duration;

        textureImage.texture = pattern.ToTexture();

        dieRenderer.SetAuto(true);
        dieRenderer.SetAnimation(anim);
        dieRenderer.Play(true);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
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
