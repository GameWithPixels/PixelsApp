using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoolDieMenuEntry : MonoBehaviour
{
    public Color disabledColor;
    public Text menuText;
    public Image menuIcon;

    Color? enabledColor;

    public void EnableMenuEntry(bool enable = false)
    {
        if (!enabledColor.HasValue)
        {
            enabledColor = menuText.color;
        }

        if (enable)
        {
            menuText.color = enabledColor.Value;
            menuIcon.color = enabledColor.Value;
            GetComponent<Button>().interactable = true;
        }
        else
        {
            menuText.color = disabledColor;
            menuIcon.color = disabledColor;
            GetComponent<Button>().interactable = false;
        }
    }

    public void DisableMenuEntry()
    {
        EnableMenuEntry(false);
    }
}
