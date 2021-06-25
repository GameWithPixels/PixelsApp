using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Animations;
using System.Text;
using System.Linq;

public class UIGradientPatternsView
    : UIPage
{
    [Header("Controls")]
    public Transform contentRoot;
    public Button addPatternButton;

    [Header("Prefabs")]
    public UIGradientPatternViewToken patternTokenPrefab;

    // The list of controls we have created to display patterns
    readonly List<UIGradientPatternViewToken> patterns = new List<UIGradientPatternViewToken>();

    void OnEnable()
    {
        base.SetupHeader(true, false, "LED Patterns", null);
        RefreshView();
    }

    void OnDisable()
    {
        if (AppDataSet.Instance != null) // When quiting the app, it may be null
        {
            foreach (var uipattern in patterns)
            {
                DestroyPatternToken(uipattern);
            }
            patterns.Clear();
        }
    }

    UIGradientPatternViewToken CreatePatternToken(EditPattern pattern)
    {
        // Create the gameObject
        var ret = GameObject.Instantiate<UIGradientPatternViewToken>(patternTokenPrefab, Vector3.zero, Quaternion.identity, contentRoot.transform);

        // When we click on the pattern main button, go to the edit page
        ret.onClick.AddListener(() => { ret.Expand(false); EditPattern(pattern); });
        ret.onEdit.AddListener(() => { ret.Expand(false); EditPattern(pattern); });
        ret.onRemove.AddListener(() => DeletePattern(pattern));
        ret.onExpand.AddListener(() => ExpandPattern(pattern));

        addPatternButton.transform.SetAsLastSibling();
        // Initialize it
        ret.Setup(pattern);
        return ret;
    }

    void Awake()
    {
        addPatternButton.onClick.AddListener(AddNewPattern);
    }

    void DestroyPatternToken(UIGradientPatternViewToken die)
    {
        GameObject.Destroy(die.gameObject);
    }

    void RefreshView()
    {
        // Assume all patterns will be destroyed
        var toDestroy = new List<UIGradientPatternViewToken>(patterns);
        foreach (var pattern in AppDataSet.Instance.patterns)
        {
            int prevIndex = toDestroy.FindIndex(a => a.editPattern == pattern);
            if (prevIndex == -1)
            {
                // New pattern
                var newPatternUI = CreatePatternToken(pattern);
                patterns.Add(newPatternUI);
            }
            else
            {
                toDestroy.RemoveAt(prevIndex);
            }
        }

        // Remove all remaining patterns
        foreach (var uipattern in toDestroy)
        {
            patterns.Remove(uipattern);
            DestroyPatternToken(uipattern);
        }
    }

    void AddNewPattern()
    {
        // Create a new default pattern
        var newPattern = AppDataSet.Instance.AddNewDefaultPattern();
        AppDataSet.Instance.SaveData();
        EditPattern(newPattern);
    }

    void EditPattern(EditPattern pattern)
    {
        // Keep a copy of the original pattern as the editor will modify the one it's given
        var patternCopy = pattern.Duplicate();
        PixelsApp.Instance.ShowPatternEditor(pattern.name, pattern, (r, p) => SetPattern(r, p, patternCopy));
    }

    void DeletePattern(EditPattern pattern)
    {
        PixelsApp.Instance.ShowDialogBox("Delete LED Pattern?", "Are you sure you want to delete " + pattern.name + "?", "Ok", "Cancel", res =>
        {
            if (res)
            {
                var dependentAnimations = AppDataSet.Instance.CollectAnimationsForPattern(pattern);
                if (dependentAnimations.Any())
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("The following animations depend on ");
                    builder.Append(pattern.name);
                    builder.AppendLine(":");
                    foreach (var b in dependentAnimations)
                    {
                        builder.Append("\t");
                        builder.AppendLine(b.name);
                    }
                    builder.Append("Are you sure you want to delete it?");

                    PixelsApp.Instance.ShowDialogBox("Pattern In Use!", builder.ToString(), "Ok", "Cancel", res2 =>
                    {
                        if (res2)
                        {
                            AppDataSet.Instance.DeletePattern(pattern);
                            AppDataSet.Instance.SaveData();
                            RefreshView();
                        }
                    });
                }
                else
                {
                    AppDataSet.Instance.DeletePattern(pattern);
                    AppDataSet.Instance.SaveData();
                    RefreshView();
                }
            }
        });
    }

    void ExpandPattern(EditPattern pattern)
    {
        foreach (var uip in patterns)
        {
            if (uip.editPattern == pattern)
            {
                uip.Expand(!uip.isExpanded);
            }
            else
            {
                uip.Expand(false);
            }
        }
    }

    void SetPattern(bool res, EditPattern pattern, EditPattern originalPatternCopy)
    {
        // Because the pattern is rendered, the UIPatternEditor is editing the original pattern
        // so we have to restore it if edition was canceled
        var newPattern = res ? pattern : originalPatternCopy;

        AppDataSet.Instance.ReplacePattern(pattern, newPattern);
        AppDataSet.Instance.SaveData();

        // Replace pattern token
        int i = patterns.FindIndex(a => a.editPattern == pattern);
        if (i >= 0)
        {
            int siblingIndex = patterns[i].transform.GetSiblingIndex();
            DestroyPatternToken(patterns[i]);
            patterns[i] = CreatePatternToken(newPattern);
            patterns[i].transform.SetSiblingIndex(siblingIndex);
        }

        RefreshView();
    }
}
