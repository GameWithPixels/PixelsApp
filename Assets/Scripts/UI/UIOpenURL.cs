using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIOpenURL : MonoBehaviour
{
    public Button button;
    public string url;

    // Start is called before the first frame update
    void Start()
    {
        button.onClick.AddListener(() => Application.OpenURL(url));
    }
}
