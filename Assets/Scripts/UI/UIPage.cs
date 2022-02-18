using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPage
    : MonoBehaviour
{

    public enum PageId
    {
        Home,
        DicePool,
        DicePoolScanning,
        Patterns,
        Pattern,
        Presets,
        Preset,
        Behaviors,
        Behavior,
        Rule,
        LiveView,
        GradientPatterns,
        AudioClips,
    }

    bool _pageDirty;
    public bool pageDirty
    {
        get => _pageDirty;
        protected set
        {
            if (_pageDirty != value)
            {
                string isNot = value ? "" : "not ";
                Debug.Log($"Page set to {isNot} dirty: {name}");
                _pageDirty = value;
            }
            NavigationManager.Instance.header.EnableSaveButton(_pageDirty);
        }
    }

    public virtual void Enter(object context)
    {
        gameObject.SetActive(true);
    }

    public virtual void OnBack()
    {
        if (pageDirty)
        {
            PixelsApp.Instance.ShowDialogBox(
                "Discard Changes",
                "You have unsaved changes, are you sure you want to discard them?",
                "Discard",
                "Cancel", discard => 
                {
                    if (discard)
                    {
                        // Reload from file
                        AppDataSet.Instance.LoadData();
                        pageDirty = false;
                        NavigationManager.Instance.GoBack();
                    }
                });
        }
        else
        {
            NavigationManager.Instance.GoBack();
        }
    }

    public virtual void OnSave()
    {
        pageDirty = false;
        AppDataSet.Instance.SaveData(); // Not sure about this one!
        NavigationManager.Instance.GoBack();
    }

    public virtual void Leave()
    {
        gameObject.SetActive(false);
    }

    public virtual void Push()
    {
        // This should give the page a chance to save to data
        // Default is just to leave the page as normal
        Leave();
    }

    public virtual void Pop(object context)
    {
        // Default is just to enter the page as normal
        Enter(context);
    }

    protected void SetupHeader(bool root, bool home, string title, System.Action<string> onTitleChanged)
    {
        NavigationManager.Instance.header.Setup(root, home, pageDirty, title, onTitleChanged);
    }

    protected void UpdateTitle(string title)
    {
        NavigationManager.Instance?.header.UpdateTitle(title);
    }

    protected void EnableSaveButton()
    {
        NavigationManager.Instance.header.EnableSaveButton(true);
    }
}

