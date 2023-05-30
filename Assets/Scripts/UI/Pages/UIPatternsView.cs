using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;

public class UIPatternsView
    : UIPage
{
    [Header("Controls")]
    public Transform contentRoot;
    public Button addPatternButton;

    [Header("Prefabs")]
    public UIPatternToken patternTokenPrefab;

    // The list of controls we have created to display patterns
    List<UIPatternToken> patterns = new List<UIPatternToken>();

    public override void Enter(object context)
    {
        base.Enter(context);
        if (AppSettings.Instance.animationsTutorialEnabled)
        {
            Tutorial.Instance.StartAnimationsTutorial();
        }
    }

    void OnEnable()
    {
        base.SetupHeader(true, false, "Animations", null);
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

    UIPatternToken CreatePatternToken(EditAnimation anim)
    {
        // Create the gameObject
        var ret = GameObject.Instantiate<UIPatternToken>(patternTokenPrefab, Vector3.zero, Quaternion.identity, contentRoot.transform);

        // When we click on the pattern main button, go to the edit page
        ret.onClick.AddListener(() => NavigationManager.Instance.GoToPage(UIPage.PageId.Pattern, anim));
        ret.onEdit.AddListener(() => NavigationManager.Instance.GoToPage(UIPage.PageId.Pattern, anim));
        ret.onDuplicate.AddListener(() => DuplicateAnimation(anim));
        ret.onRemove.AddListener(() => DeleteAnimation(anim));
        ret.onExport.AddListener(() => { PixelsApp.Instance.ExportPattern(anim); ret.Expand(false); });
        ret.onExpand.AddListener(() => ExpandAnimation(anim));

        // Initialize it
        ret.Setup(anim);
        return ret;
    }

    void Awake()
    {
        addPatternButton.onClick.AddListener(AddNewPattern);
    }

    void DestroyPatternToken(UIPatternToken die)
    {
        GameObject.Destroy(die.gameObject);
    }

    void RefreshView()
    {
        // Assume all patterns will be destroyed
        var toDestroy = new List<UIPatternToken>(patterns);
        foreach (var anim in AppDataSet.Instance.animations)
        {
            int prevIndex = toDestroy.FindIndex(a => a.editAnimation == anim);
            if (prevIndex == -1)
            {
                // New pattern
                var newPatternUI = CreatePatternToken(anim);
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
        // Create a new default animation
        var newAnim = AppDataSet.Instance.AddNewDefaultAnimation();
        AppDataSet.Instance.SaveData();
        NavigationManager.Instance.GoToPage(UIPage.PageId.Pattern, newAnim);
    }

    void DuplicateAnimation(EditAnimation anim)
    {
        AppDataSet.Instance.DuplicateAnimation(anim);
        patterns.Find(p => p.editAnimation == anim).Expand(false);
        AppDataSet.Instance.SaveData();
        RefreshView();
    }

    void DeleteAnimation(EditAnimation anim)
    {
        PixelsApp.Instance.ShowDialogBox("Delete Animation?", "Are you sure you want to delete " + anim.name + "?", "Ok", "Cancel", res =>
        {
            if (res)
            {
                var dependentPresets = AppDataSet.Instance.CollectPresetsForAnimation(anim);
                if (dependentPresets.Any())
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("The following profiles depend on ");
                    builder.Append(anim.name);
                    builder.AppendLine(":");
                    foreach (var b in dependentPresets)
                    {
                        builder.Append("\t");
                        builder.AppendLine(b.name);
                    }
                    builder.Append("Are you sure you want to delete it?");

                    PixelsApp.Instance.ShowDialogBox("Animation In Use!", builder.ToString(), "Ok", "Cancel", res2 =>
                    {
                        if (res2)
                        {
                            AppDataSet.Instance.DeleteAnimation(anim);
                            AppDataSet.Instance.SaveData();
                            RefreshView();
                        }
                    });
                }
                else
                {
                    AppDataSet.Instance.DeleteAnimation(anim);
                    AppDataSet.Instance.SaveData();
                    RefreshView();
                }
            }
        });
    }

    void ExpandAnimation(EditAnimation anim)
    {
        foreach (var uip in patterns)
        {
            if (uip.editAnimation == anim)
            {
                uip.Expand(!uip.isExpanded);
            }
            else
            {
                uip.Expand(false);
            }
        }
    }
}
