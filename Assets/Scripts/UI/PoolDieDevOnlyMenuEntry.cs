using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoolDieDevOnlyMenuEntry : MonoBehaviour
{
    public Color disabledColor;
    public Text menuText;
    public Image menuIcon;

    void OnEnable()
    {
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        menuText.color = disabledColor;
        menuIcon.color = disabledColor;
        GetComponent<Button>().interactable = false;
#endif
    }
}
