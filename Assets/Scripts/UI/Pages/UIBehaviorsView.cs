using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Linq;

public class UIBehaviorsView
    : UIPage
{
    [Header("Controls")]
    public Transform contentRoot;
    public Button addBehaviorButton;
    public RectTransform spacer;

    [Header("Prefabs")]
    public UIBehaviorToken behaviorTokenPrefab;

    // The list of controls we have created to display behaviors
    List<UIBehaviorToken> behaviors = new List<UIBehaviorToken>();

    void OnEnable()
    {
        base.SetupHeader(true, false, "Profiles", null);
        RefreshView();
    }

    void OnDisable()
    {
        if (AppDataSet.Instance != null) // When quiting the app, it may be null
        {
            foreach (var uibehavior in behaviors)
            {
                DestroyBehaviorToken(uibehavior);
            }
            behaviors.Clear();
        }
    }

    UIBehaviorToken CreateBehaviorToken(EditProfile profile)
    {
        // Create the gameObject
        var ret = GameObject.Instantiate<UIBehaviorToken>(behaviorTokenPrefab, Vector3.zero, Quaternion.identity, contentRoot.transform);
        spacer.SetAsLastSibling();

        // When we click on the pattern main button, go to the edit page
        ret.onClick.AddListener(() => NavigationManager.Instance.GoToPage(UIPage.PageId.Behavior, profile));
        ret.onEdit.AddListener(() => NavigationManager.Instance.GoToPage(UIPage.PageId.Behavior, profile));
        ret.onDuplicate.AddListener(() => DuplicateBehavior(profile));
        ret.onRemove.AddListener(() => DeleteBehavior(profile));
        ret.onExpand.AddListener(() => ExpandBehavior(profile));

        addBehaviorButton.transform.SetAsLastSibling();
        // Initialize it
        ret.Setup(profile);
        return ret;
    }

    void Awake()
    {
        addBehaviorButton.onClick.AddListener(AddNewBehavior);
    }

    void DestroyBehaviorToken(UIBehaviorToken die)
    {
        GameObject.Destroy(die.gameObject);
    }

    void RefreshView()
    {
        // Assume all behavior will be destroyed
       var toDestroy = new List<UIBehaviorToken>(behaviors);
        foreach (var bh in AppDataSet.Instance.profiles)
        {
            int prevIndex = toDestroy.FindIndex(a => a.editBehavior == bh);
            if (prevIndex == -1)
            {
                // New behavior
                var newBehaviorUI = CreateBehaviorToken(bh);
                behaviors.Add(newBehaviorUI);
            }
            else
            {
                toDestroy.RemoveAt(prevIndex);
            }
        }

        // Remove all remaining behaviors
        foreach (var bh in toDestroy)
        {
            behaviors.Remove(bh);
            DestroyBehaviorToken(bh);
        }
    }

    void AddNewBehavior()
    {
        // Create a new default behavior
        var newBehavior = AppDataSet.Instance.AddNewDefaultProfile();
        AppDataSet.Instance.SaveData();
        NavigationManager.Instance.GoToPage(UIPage.PageId.Behavior, newBehavior);
    }

    void DuplicateBehavior(EditProfile profile)
    {
        AppDataSet.Instance.DuplicateProfile(profile);
        behaviors.Find(p => p.editBehavior == profile).Expand(false);
        AppDataSet.Instance.SaveData();
        RefreshView();
    }

    void DeleteBehavior(EditProfile profile)
    {
        PixelsApp.Instance.ShowDialogBox("Delete Profile?", "Are you sure you want to delete " + profile.name + "?", "Ok", "Cancel", res =>
        {
            if (res)
            {
                var dependentPresets = AppDataSet.Instance.CollectPresetsForBehavior(profile);
                if (dependentPresets.Any())
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("The following presets depend on ");
                    builder.Append(profile.name);
                    builder.AppendLine(":");
                    foreach (var b in dependentPresets)
                    {
                        builder.Append("\t");
                        builder.AppendLine(b.name);
                    }
                    builder.Append("Are you sure you want to delete it?");

                    PixelsApp.Instance.ShowDialogBox("Profile In Use!", builder.ToString(), "Ok", "Cancel", res2 =>
                    {
                        if (res2)
                        {
                            AppDataSet.Instance.DeleteProfile(profile);
                            AppDataSet.Instance.SaveData();
                            RefreshView();
                        }
                    });
                }
                else
                {
                    AppDataSet.Instance.DeleteProfile(profile);
                    AppDataSet.Instance.SaveData();
                    RefreshView();
                }
            }
        });
    }

    void ExpandBehavior(EditProfile profile)
    {
        foreach (var uip in behaviors)
        {
            if (uip.editBehavior == profile)
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
