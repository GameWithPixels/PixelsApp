using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Systemic.Unity.Pixels.Profiles;
using Presets;
using System.Linq;
using Dice;

public class UIHomePage
    : UIPage
{
    [Header("Controls")]
    public Transform newsSection;
    public Transform presetsRoot;
    public Transform behaviorsRoot;
    public Button dismissMessagesButton;
    public Button editPresetsButton;
    public Button editProfilesButton;

    [Header("Prefabs")]
    public UIHomePresetToken presetTokenPrefab;
    public UIHomeBehaviorToken behaviorTokenPrefab;

    // The list of controls we have created to display presets
    List<UIHomePresetToken> presets = new List<UIHomePresetToken>();
    List<UIHomeBehaviorToken> behaviors = new List<UIHomeBehaviorToken>();

    public override void Enter(object context)
    {
        base.Enter(context);
        if (AppSettings.Instance.mainTutorialEnabled)
        {
            Tutorial.Instance.StartMainTutorial();
        }
        else if (AppSettings.Instance.homeTutorialEnabled)
        {
            Tutorial.Instance.StartHomeTutorial();
        }
    }

    void OnEnable()
    {
        base.SetupHeader(true, true, "Pixels", null);
        RefreshView();
    }

    void OnDisable()
    {
        if (AppDataSet.Instance != null) // When quiting the app, it may be null
        {
            foreach (var uipreset in presets)
            {
                DestroyPresetToken(uipreset);
            }
            presets.Clear();
            foreach (var uibehavior in behaviors)
            {
                DestroyBehaviorToken(uibehavior);
            }
            behaviors.Clear();
            //AppDataSet.Instance.OnChange -= OnDataSetChange;
        }
    }

    void Awake()
    {
        dismissMessagesButton.onClick.AddListener(CloseWhatsNew);
        editPresetsButton.onClick.AddListener(() => NavigationManager.Instance.GoToRoot(UIPage.PageId.Presets));
        editProfilesButton.onClick.AddListener(() => NavigationManager.Instance.GoToRoot(UIPage.PageId.Behaviors));
        PixelsApp.Instance.onDieProfileUpdatedEvent += (_, __) => UpdatePresetAndProfileStatuses();
    }

    UIHomePresetToken CreatePresetToken(EditPreset preset)
    {
        // Create the gameObject
        var ret = GameObject.Instantiate<UIHomePresetToken>(presetTokenPrefab, Vector3.zero, Quaternion.identity, presetsRoot.transform);

        // When we click on the pattern main button, go to the edit page
        ret.onClick.AddListener(() =>
        {
            ActivatePreset(preset);
        });

        // Initialize it
        ret.Setup(preset);
        return ret;
    }

    void DestroyPresetToken(UIHomePresetToken die)
    {
        GameObject.Destroy(die.gameObject);
    }

    UIHomeBehaviorToken CreateBehaviorToken(EditProfile profile)
    {
        // Create the gameObject
        var ret = GameObject.Instantiate<UIHomeBehaviorToken>(behaviorTokenPrefab, Vector3.zero, Quaternion.identity, behaviorsRoot.transform);

        // When we click on the pattern main button, go to the edit page
        ret.onClick.AddListener(() =>
        {
            ActivateBehavior(profile);
        });

        // Initialize it
        ret.Setup(profile);
        return ret;
    }

    void DestroyBehaviorToken(UIHomeBehaviorToken die)
    {
        GameObject.Destroy(die.gameObject);
    }

    void RefreshView()
    {
        newsSection.gameObject.SetActive(AppSettings.Instance.displayWhatsNew);

        // Assume all presets will be destroyed
        var toDestroy = new List<UIHomePresetToken>(presets);
        foreach (var preset in AppDataSet.Instance.presets)
        {
            int prevIndex = toDestroy.FindIndex(a => a.editPreset == preset);
            if (prevIndex == -1)
            {
                // New preset
                var newPresetUI = CreatePresetToken(preset);
                presets.Add(newPresetUI);
            }
            else
            {
                toDestroy.RemoveAt(prevIndex);
            }
        }

        // Remove all remaining presets
        foreach (var uipreset in toDestroy)
        {
            presets.Remove(uipreset);
            DestroyPresetToken(uipreset);
        }

        // Assume all behaviors will be destroyed
        var toDestroy2 = new List<UIHomeBehaviorToken>(behaviors);
        foreach (var behavior in AppDataSet.Instance.profiles)
        {
            int prevIndex = toDestroy2.FindIndex(a => a.editBehavior == behavior);
            if (prevIndex == -1)
            {
                // New behavior
                var newBehaviorUI = CreateBehaviorToken(behavior);
                behaviors.Add(newBehaviorUI);
            }
            else
            {
                toDestroy.RemoveAt(prevIndex);
            }
        }

        // Remove all remaining behaviors
        foreach (var uibehavior in toDestroy2)
        {
            behaviors.Remove(uibehavior);
            DestroyBehaviorToken(uibehavior);
        }

        UpdatePresetAndProfileStatuses();
    }

    void ActivatePreset(Presets.EditPreset editPreset)
    {
        PixelsApp.Instance.ShowDialogBox(
            "Activate " + editPreset.name + "?",
            "Do you want to switch to this preset?",
            "Yes",
            "Cancel",
            (res) =>
            {
                if (res)
                {
                    // Attempt to activate the preset
                    PixelsApp.Instance.UploadPreset(editPreset, (res2) =>
                    {
                        if (res2)
                        {
                            UpdatePresetAndProfileStatuses();
                        }
                    });
                }
            });
    }

    void ActivateBehavior(EditProfile profile)
    {
        PixelsApp.Instance.ActivateBehavior(profile, (die, res) =>
        {
            if (res)
            {
                UpdatePresetAndProfileStatuses();
            }
        });
    }

    void CloseWhatsNew()
    {
        newsSection.gameObject.SetActive(false);
        AppSettings.Instance.SetDisplayWhatsNew(false);
    }

    void UpdatePresetAndProfileStatuses()
    {
        foreach (var uipresetToken in presets)
        {
            uipresetToken.RefreshState();
        }
        foreach (var uibehaviorToken in behaviors)
        {
            uibehaviorToken.RefreshState();
        }
    }
}
