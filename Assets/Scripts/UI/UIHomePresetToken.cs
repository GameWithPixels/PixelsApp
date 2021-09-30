using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Presets;
using System.Linq;
using Dice;
using Systemic.Unity.Pixels;

public class UIHomePresetToken : MonoBehaviour
{
    [Header("Controls")]
    public Button mainButton;
    public RawImage presetRenderImage;
    public Text presetNameText;
    public RectTransform checkMarkRoot;

    public EditPreset editPreset { get; private set; }
    public MultiDiceRenderer dieRenderer { get; private set; }

    public Button.ButtonClickedEvent onClick => mainButton.onClick;

    bool visible = true;

    public void Setup(EditPreset preset)
    {
        this.editPreset = preset;
        var designs = new List<PixelDesignAndColor>(preset.dieAssignments.Select(ass => (ass.die != null) ? ass.die.designAndColor : PixelDesignAndColor.Unknown));

        this.dieRenderer = DiceRendererManager.Instance.CreateMultiDiceRenderer(designs, 400);
        if (dieRenderer != null)
        {
            presetRenderImage.texture = dieRenderer.renderTexture;
        }
        presetNameText.text = preset.name;

        dieRenderer.rotating = true;
        for (int i = 0; i < preset.dieAssignments.Count; ++i)
        {
            if (preset.dieAssignments[i].behavior != null)
            {
                dieRenderer.SetDieAnimations(i, preset.dieAssignments[i].behavior.CollectAnimations().Where(anim => anim != null));
                dieRenderer.Play(i, false);
            }
        }
        RefreshState();
    }

    public void RefreshState()
    {
        // Displays a check mark if this preset is active
        //checkMarkRoot.gameObject.SetActive(editPreset.IsActive());
        checkMarkRoot.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (this.dieRenderer != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(this.dieRenderer);
            this.dieRenderer = null;
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

}
