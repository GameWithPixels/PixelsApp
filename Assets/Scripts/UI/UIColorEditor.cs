using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UIColorEditor : MonoBehaviour
{
    [Header("Controls")]
    public UIColorWheel colorWheel;
    public UIColorWheelSelection colorWheelSelection;
    public Button dimButton;
    public Button brightButton;
    public Transform dimButtonSelected;
    public Transform brightButtonSelected;
    public List<UIColorButton> colorButtons;

    [Header("Parameters")]
    public float valueDimColors = 0.35f;

    public delegate void ColorSelectedEvent(Color color);
    public ColorSelectedEvent onColorSelected;

    const float epsilon = 0.01f;

    Color currentColor;
    Color[] defaultButtonColors;

    /// <summary>
    /// Invoke the color picker
    /// </sumary>
    public void SelectColor(Color color)
    {
        gameObject.SetActive(true);
        currentColor = color;

        Color.RGBToHSV(color, out _, out _, out float val);
        if (Mathf.Abs(valueDimColors - val) < epsilon)
        {
            ChangeColorsLightness(valueDimColors);
        }
        else
        {
            // Any other case, initialize the color wheel to the bright one
            ChangeColorsLightness(1.0f);
        }

        UpdateSelection();

        onColorSelected?.Invoke(currentColor);
    }

    public void ClearColorSelection()
    {
        // Any other case, initialize the color wheel to the bright one
        ChangeColorsLightness(1.0f);
        SetSelectedColorButton(null);
        colorWheelSelection.ClearSelection();
    }

    void Awake()
    {
        defaultButtonColors = colorButtons.Select(btn => btn.color).ToArray();
        colorWheel.onClicked += (color, _1, _2) => SelectColor(color);
        foreach (var btn in colorButtons)
        {
            btn.onClick.AddListener(() => SelectColor(btn.color));
        }
        brightButton.onClick.AddListener(() => { ChangeColorsLightness(1.0f); UpdateSelection(); });
        dimButton.onClick.AddListener(() => { ChangeColorsLightness(valueDimColors); UpdateSelection(); });
    }

    void ChangeColorsLightness(float value)
    {
        dimButtonSelected.gameObject.SetActive(value < 1);
        brightButtonSelected.gameObject.SetActive(value >= 1);

        // Update wheel colors
        colorWheel.colorValue = value;

        // Update preselected colors as well
        for (int i = 0; i < colorButtons.Count; ++i)
        {
            // We want to keep our black
            if (defaultButtonColors[i] != Color.black)
            {
                Color.RGBToHSV(defaultButtonColors[i], out float hue, out float sat, out _);
                colorButtons[i].GetComponent<Image>().color = Color.HSVToRGB(hue, sat, value);
            }
        }
    }

    void SetSelectedColorButton(UIColorButton btn)
    {
        foreach (var b in colorButtons)
        {
            b.SetSelected(b == btn);
        }
    }

    void UpdateSelection()
    {
        colorWheel.FindColor(currentColor, out int selectedHueIndex, out int selectedSatIndex);
        colorWheelSelection.SetSelection(currentColor, selectedHueIndex, selectedSatIndex);

        static bool AreColorEquals(Color c0, Color c1) =>
            ((c0.r - c1.r) * (c0.r - c1.r) + (c0.g - c1.g) * (c0.g - c1.g) + (c0.b - c1.b) * (c0.b - c1.b)) < epsilon;
        SetSelectedColorButton(colorButtons.FirstOrDefault(b => AreColorEquals(b.color, currentColor)));
    }
}
