﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIProgrammingBox : MonoBehaviour
{
    [Header("Controls")]
    public Text description;
    public Image prograssBarBackground;
    public Image progressBar;
    public float defaultProgressSpeed = 0.2f; // pct/sec
    public float progressSpeedMin = 0.02f;
    public float progressMaxSpeed = 1.0f;

    public bool isShown => gameObject.activeSelf;
    float currentProgress = 0.0f;
    float currentDisplayProgress = 0.0f;
    float currentProgressSpeed = 1.0f;
    float lastUpdateTime = 0.0f;

    public void Show(string text)
    {
        gameObject.SetActive(true);
        currentProgress = 0.0f;
        currentDisplayProgress = 0.0f;
        currentProgressSpeed = defaultProgressSpeed;
        lastUpdateTime = Time.realtimeSinceStartup;
        SetProgress(0, text);
        var offsetMax = progressBar.rectTransform.offsetMax;
        offsetMax.x = prograssBarBackground.rectTransform.rect.width * currentDisplayProgress;
        progressBar.rectTransform.offsetMax = offsetMax;
    }

    public void SetProgress(float newProgress, string text = null)
    {
        float deltaTime = Time.realtimeSinceStartup - lastUpdateTime;
        if (deltaTime > 0.001f)
        {
            float deltaProgress = newProgress - currentDisplayProgress;
            currentProgressSpeed = Mathf.Clamp(deltaProgress / deltaTime, progressSpeedMin, progressMaxSpeed);

            lastUpdateTime = Time.realtimeSinceStartup;
        }
        currentProgress = newProgress;
        if (text != null)
        {
            description.text = text;
        }
    }

    void Update()
    {
        float maxDelta = currentProgressSpeed * Time.unscaledDeltaTime;
        currentDisplayProgress = Mathf.MoveTowards(currentDisplayProgress, currentProgress, maxDelta);
        var offsetMax = progressBar.rectTransform.offsetMax;
        offsetMax.x = prograssBarBackground.rectTransform.rect.width * currentDisplayProgress;
        progressBar.rectTransform.offsetMax = offsetMax;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

}
