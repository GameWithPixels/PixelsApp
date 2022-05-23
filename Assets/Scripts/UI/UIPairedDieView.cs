using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dice;
using Presets;
using Systemic.Unity.Pixels;

public class UIPairedDieView : MonoBehaviour
{
    // Controls
    [Header("Controls")]
    public RawImage dieRenderImage;
    public Text dieNameText;
    public Text firmwareIDText;
    public UIDieLargeBatteryView batteryView;
    public UIDieLargeSignalView signalView;
    public Text statusText;
    public RectTransform disconnectedTextRoot;
    public RectTransform errorTextRoot;

    [Header("Parameters")]
    public Color defaultTextColor;
    public Color selectedColor;


    public EditDie die { get; private set; }
    public SingleDiceRenderer dieRenderer { get; private set; }
    public bool selected { get; private set; }

    bool visible = true;

    public void Setup(EditDie die)
    {
        this.die = die;
        dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(die.designAndColor);
        if (dieRenderer != null)
        {
            dieRenderImage.texture = dieRenderer.renderTexture;
        }
        UpdateState();
        SetSelected(false);

        if (die.die == null)
        {
            die.onDieFound += OnDieFound;
        }
        else
        {
            OnDieFound(die);
        }
        die.onDieWillBeLost += OnDieWillBeLost;
    }

    public void SetSelected(bool selected)
    {
        this.selected = selected;
        if (selected)
        {
            dieNameText.color = selectedColor;
        }
        else
        {
            dieNameText.color = defaultTextColor;
        }
    }

    public void UpdateState()
    {
        dieNameText.text = die.name;

        if (die.die == null)
        {
            batteryView.SetLevel(null, null);
            signalView.SetRssi(null);
            dieRenderer.SetAuto(false);
            dieRenderImage.color = Color.white;
            batteryView.gameObject.SetActive(true);
            signalView.gameObject.SetActive(true);
            statusText.text = "Disconnected";
            disconnectedTextRoot.gameObject.SetActive(false);
            errorTextRoot.gameObject.SetActive(false);
            firmwareIDText.text = "Firmware: Unavailable";
        }
        else
        {
            firmwareIDText.text = "Firmware: " + die.die.firmwareVersionId;
            batteryView.SetLevel(die.die.batteryLevel, die.die.isCharging);
            signalView.SetRssi(die.die.rssi);
            switch (die.die.lastError)
            {
                case PixelError.None:
                    switch (die.die.connectionState)
                    {
                    case PixelConnectionState.Invalid:
                        dieRenderer.SetAuto(false);
                        if (AppConstants.Instance)
                            dieRenderImage.color = AppConstants.Instance.DieUnavailableColor;
                        batteryView.gameObject.SetActive(false);
                        signalView.gameObject.SetActive(false);
                        statusText.text = "Invalid";
                        disconnectedTextRoot.gameObject.SetActive(true);
                        errorTextRoot.gameObject.SetActive(false);
                        break;
                    case PixelConnectionState.Available:
                        dieRenderer.SetAuto(true);
                        dieRenderImage.color = Color.white;
                        batteryView.gameObject.SetActive(true);
                        signalView.gameObject.SetActive(true);
                        statusText.text = "Available";
                        disconnectedTextRoot.gameObject.SetActive(false);
                        errorTextRoot.gameObject.SetActive(false);
                        break;
                    case PixelConnectionState.Connecting:
                        dieRenderer.SetAuto(false);
                        dieRenderImage.color = Color.white;
                        batteryView.gameObject.SetActive(true);
                        signalView.gameObject.SetActive(true);
                        statusText.text = "Connecting";
                        disconnectedTextRoot.gameObject.SetActive(false);
                        errorTextRoot.gameObject.SetActive(false);
                        break;
                    case PixelConnectionState.Identifying:
                        dieRenderer.SetAuto(true);
                        dieRenderImage.color = Color.white;
                        batteryView.gameObject.SetActive(true);
                        signalView.gameObject.SetActive(true);
                        statusText.text = "Identifying";
                        disconnectedTextRoot.gameObject.SetActive(false);
                        errorTextRoot.gameObject.SetActive(false);
                        break;
                    case PixelConnectionState.Ready:
                        dieRenderer.SetAuto(true);
                        dieRenderImage.color = Color.white;
                        batteryView.gameObject.SetActive(true);
                        signalView.gameObject.SetActive(true);
                        statusText.text = "Ready";
                        disconnectedTextRoot.gameObject.SetActive(false);
                        errorTextRoot.gameObject.SetActive(false);
                        break;
                    }
                    break;
                case PixelError.ConnectionError:
                    dieRenderer.SetAuto(false);
                    if (AppConstants.Instance)
                        dieRenderImage.color = AppConstants.Instance.DieUnavailableColor;
                    batteryView.gameObject.SetActive(false);
                    signalView.gameObject.SetActive(false);
                    statusText.text = "Connection Error";
                    disconnectedTextRoot.gameObject.SetActive(false);
                    errorTextRoot.gameObject.SetActive(true);
                    break;
                case PixelError.Disconnected:
                    dieRenderer.SetAuto(false);
                    if (AppConstants.Instance)
                        dieRenderImage.color = AppConstants.Instance.DieUnavailableColor;
                    batteryView.gameObject.SetActive(false);
                    signalView.gameObject.SetActive(false);
                    statusText.text = "Disconnected";
                    disconnectedTextRoot.gameObject.SetActive(true);
                    errorTextRoot.gameObject.SetActive(false);
                    break;
            }
        }
    }

    void OnDestroy()
    {
        if (dieRenderer != null && DiceRendererManager.Instance)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(dieRenderer);
            dieRenderer = null;
        }

        die.onDieFound -= OnDieFound;
        die.onDieWillBeLost -= OnDieWillBeLost;

        if (die.die != null)
        {
            OnDieWillBeLost(die);
        }
    }

    void OnConnectionStateChanged(Pixel die, PixelConnectionState oldState, PixelConnectionState newState)
    {
        UpdateState();
    }

    void OnError(Pixel die, PixelError lastError)
    {
        UpdateState();
    }

    void OnBatteryLevelChanged(Pixel die, float level, bool? charging)
    {
        UpdateState();
    }

    void OnRssiChanged(Pixel die, int rssi)
    {
        UpdateState();
    }

    void OnNameChanged(Pixel die, string newName)
    {
        this.die.name = die.name;
        UpdateState();
    }

    void OnAppearanceChanged(Pixel die, int newFaceCount, PixelDesignAndColor newDesign)
    {
        this.die.designAndColor = newDesign;
        if (dieRenderer != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(dieRenderer);
            dieRenderer = null;
        }
        dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(newDesign);
        if (dieRenderer != null)
        {
            dieRenderImage.texture = dieRenderer.renderTexture;
        }
    }

    void OnDieFound(EditDie editDie)
    {
        Debug.Assert(editDie == die);
        die.die.ConnectionStateChanged += OnConnectionStateChanged;
        die.die.ErrorEncountered += OnError;
        die.die.AppearanceChanged += OnAppearanceChanged;
        die.die.BatteryLevelChanged += OnBatteryLevelChanged;
        die.die.RssiChanged += OnRssiChanged;

        bool saveUpdatedData = false;
        if (die.designAndColor != die.die.designAndColor)
        {
            OnAppearanceChanged(die.die, die.die.faceCount, die.die.designAndColor);
            saveUpdatedData = true;
        }

        if (die.name != die.die.name)
        {
            OnNameChanged(die.die, die.die.name);
            saveUpdatedData = true;
        }

        if (saveUpdatedData)
        {
            AppDataSet.Instance.SaveData();
        }
    }

    void OnDieWillBeLost(EditDie editDie)
    {
        editDie.die.ConnectionStateChanged -= OnConnectionStateChanged;
        editDie.die.AppearanceChanged -= OnAppearanceChanged;
        editDie.die.BatteryLevelChanged -= OnBatteryLevelChanged;
        editDie.die.RssiChanged -= OnRssiChanged;
        editDie.die.ErrorEncountered -= OnError;
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
