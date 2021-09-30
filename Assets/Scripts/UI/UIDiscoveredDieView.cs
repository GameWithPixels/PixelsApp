using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dice;
using Systemic.Unity.Pixels;

public class UIDiscoveredDieView : MonoBehaviour
{
    // Controls
    [Header("Controls")]
    public Image backgroundImage;
    public RawImage dieRenderImage;
    public Text dieNameText;
    public UIDieLargeBatteryView batteryView;
    public UIDieLargeSignalView signalView;
    public Button selectButton;
    public Image toggleImage;

    [Header("Images")]
    public Sprite backgroundSelectedSprite;
    public Sprite backgroundUnselectedSprite;

    public Pixel die { get; private set; }
    public SingleDiceRenderer dieRenderer { get; private set; }
    public bool selected { get; private set; }

    public delegate void SelectedEvent(UIDiscoveredDieView uidie, bool selected);
    public SelectedEvent onSelected;

    public void Setup(Pixel die)
    {
        this.die = die;
        this.dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(die.designAndColor);
        if (dieRenderer != null)
        {
            dieRenderer.SetAuto(true);
            dieRenderImage.texture = dieRenderer.renderTexture;
        }
        dieNameText.text = die.name;

        batteryView.SetLevel(die.batteryLevel, die.isCharging);
        signalView.SetRssi(die.rssi);
        die.BatteryLevelChanged += OnBatteryLevelChanged;
        die.RssiChanged += OnRssiChanged;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        this.selected = selected;
        backgroundImage.sprite = selected ? backgroundSelectedSprite : backgroundUnselectedSprite;
        toggleImage.gameObject.SetActive(selected);
        onSelected?.Invoke(this, selected);
    }

    void Awake()
    {
        // Hook up to events
        selectButton.onClick.AddListener(OnToggle);
    }

    void OnToggle()
    {
        SetSelected(!selected);
    }

    void OnDestroy()
    {
        die.BatteryLevelChanged -= OnBatteryLevelChanged;
        die.RssiChanged -= OnRssiChanged;
        if (this.dieRenderer != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(this.dieRenderer);
            this.dieRenderer = null;
        }
    }
    
    void OnBatteryLevelChanged(Pixel die, float level, bool charging)
    {
        batteryView.SetLevel(level, charging);
    }

    void OnRssiChanged(Pixel die, int rssi)
    {
        signalView.SetRssi(rssi);
    }

}
