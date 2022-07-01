using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIParameterEditColor : UIParameter
{
    [Header("Controls")]
    public Text nameText;
    public Button colorButton;
    public Image colorImage;
    public InputField valueField;
    public Image disableOverlayImage;
    public Toggle overrideColorToggle;

    static readonly char[] hexChars = Enumerable.Range('0', 10).Select(i => (char)i)
        .Concat(Enumerable.Range('A', 6).Select(i => (char)i)).ToArray();

    public override bool CanEdit(System.Type parameterType, IEnumerable<object> attributes = null)
    {
        return parameterType == typeof(EditColor);
    }

    protected override void SetupControls(string name, System.Func<object> getterFunc, System.Action<object> setterAction, IEnumerable<object> attributes = null)
    {
        // Set name
        nameText.text = name;

        // Set initial value
        SetColor((EditColor)getterFunc.Invoke());

        colorButton.onClick.RemoveAllListeners();
        colorButton.onClick.AddListener(() => PixelsApp.Instance.ShowColorPicker("Select " + name, ((EditColor)getterFunc.Invoke()).asColor32, (res, newColor) => 
        {
            if (res)
            {
                var editColor = EditColor.FromColor(newColor);
                SetColor(editColor);
                setterAction?.Invoke(editColor);
            }
        }));

        overrideColorToggle.onValueChanged.RemoveAllListeners();
        overrideColorToggle.onValueChanged.AddListener(newToggleValue =>
        {
            var curColor = (EditColor)getterFunc.Invoke();
            curColor.type = newToggleValue ? EditColor.ColorType.Face : EditColor.ColorType.RGB;
            SetColor(curColor);
            setterAction?.Invoke(curColor);
        });

        valueField.onValidateInput = (string text, int charIndex, char addedChar) =>
        {
            if (overrideColorToggle.isOn)
                return addedChar;

            // Keep only valid characters for a color in hex format
            addedChar = char.ToUpperInvariant(addedChar);
            return hexChars.Contains(addedChar) ? addedChar : '\0';
        };

        valueField.onValueChanged.RemoveAllListeners();
        valueField.onValueChanged.AddListener(text =>
        {
            if (!overrideColorToggle.isOn)
            {
                // Read color
                var hex = System.Globalization.NumberStyles.HexNumber;
                int l = text.Length;
                var newColor = new Color32(
                    l > 0 ? byte.Parse(text.Substring(0, Mathf.Min(l, 2)), hex) : (byte)0,
                    l > 2 ? byte.Parse(text.Substring(2, Mathf.Min(l - 2, 2)), hex) : (byte)0,
                    l > 4 ? byte.Parse(text.Substring(4, Mathf.Min(l - 4, 2)), hex) : (byte)0,
                    0xFF);

                // Update
                var editColor = EditColor.FromColor(newColor);
                SetColor(editColor, skipText: true);
                setterAction?.Invoke(editColor);
            }
        });

        valueField.onEndEdit.RemoveAllListeners();
        valueField.onEndEdit.AddListener(text =>
        {
            // Pad with 0s once editing is done
            if ((!overrideColorToggle.isOn) && (text.Length < 6))
            {
                valueField.text = text + string.Join("", Enumerable.Repeat("0", 6 - text.Length));
            }
        });
    }

    void SetColor(EditColor newColor, bool skipText = false)
    {
        switch (newColor.type)
        {
            case EditColor.ColorType.RGB:
                {
                    overrideColorToggle.isOn = false;
                    var col32 = newColor.asColor32;
                    // Make sure we always use full alpha
                    col32.a = 255;
                    colorImage.color = col32;
                    valueField.enabled = true;
                    if (!skipText)
                        valueField.text = col32.r.ToString("X2") + col32.g.ToString("X2") + col32.b.ToString("X2");
                    disableOverlayImage.gameObject.SetActive(false);
                }
                break;
            case EditColor.ColorType.Face:
                {
                    overrideColorToggle.isOn = true;
                    colorImage.color = Color.grey;
                    valueField.enabled = false;
                    valueField.text = "N/A";
                    disableOverlayImage.gameObject.SetActive(true);
                }
                break;
            default:
                throw new System.NotImplementedException();
        }
    }
}
