using Behaviors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UIBehaviorView
    : UIPage
{
    [Header("Controls")]
    public InputField descriptionText;
    public RawImage previewImage;
    public RectTransform rulesRoot;
    public Button addRuleButton;
    public RectTransform spacer;
    public Button activateButton;

    public EditBehavior editBehavior { get; private set; }
    public SingleDiceRenderer dieRenderer { get; private set; }

    [Header("Prefabs")]
    public UIRuleToken ruleTokenPrefab;

    public class Context
    {
        public EditBehavior behavior;
        public Presets.EditPreset parentPreset;
        public Presets.EditDieAssignment dieAssignment;
    }

    // The list of controls we have created to display rules
    readonly List<UIRuleToken> rules = new List<UIRuleToken>();
    System.Func<bool> hasRuleChanged;

    public override void Enter(object context)
    {
        gameObject.SetActive(true);
        var bhv = context as EditBehavior;
        if (bhv != null)
        {
            SetupHeader(false, false, bhv.name, SetName);
            Setup(bhv);
        }

        if (AppSettings.Instance.behaviorTutorialEnabled)
        {
            Tutorial.Instance.StartBehaviorTutorial();
        }
    }

    public override void Push()
    {
        // Don't clean up
        gameObject.SetActive(false);
    }

    public override void Pop(object context)
    {
        // Leave everything as is...
        gameObject.SetActive(true);
        if (editBehavior != null)
        {
            SetupHeader(false, false, editBehavior.name, SetName);

            RefreshView();

            dieRenderer.SetAuto(true);
            dieRenderer.SetAnimations(editBehavior.CollectAnimations());
            dieRenderer.Play(true);

            if (hasRuleChanged?.Invoke() ?? false)
            {
                pageDirty = true;
                hasRuleChanged = null;
            }
        }
    }

    public override void Leave()
    {
        if (DiceRendererManager.Instance != null && dieRenderer != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(dieRenderer);
            dieRenderer = null;
        }

        foreach (var ruleui in rules)
        {
            GameObject.Destroy(ruleui.gameObject);
        }
        rules.Clear();
        gameObject.SetActive(false);
    }

    void Setup(EditBehavior behavior)
    {
        editBehavior = behavior;
        dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(editBehavior.defaultPreviewSettings.design, 300);
        if (dieRenderer != null)
        {
            previewImage.texture = dieRenderer.renderTexture;
        }
        // Generate a title for the page
        descriptionText.text = editBehavior.description;

        RefreshView();

        dieRenderer.SetAuto(true);
        dieRenderer.SetAnimations(editBehavior.CollectAnimations());
        dieRenderer.Play(true);
    }

    void Awake()
    {
        addRuleButton.onClick.AddListener(AddNewRule);
        descriptionText.onEndEdit.AddListener(SetDescription);
        activateButton.onClick.AddListener(ActivateBehavior);
    }

    void AddNewRule()
    {
        var newRule = editBehavior.AddNewDefaultRule();
        pageDirty = true;
        RefreshView();
        NavigationManager.Instance.GoToPage(UIPage.PageId.Rule, newRule);
    }

    void RefreshView()
    {
        // Assume all rule UIs will be destroyed
        List<UIRuleToken> toDestroy = new List<UIRuleToken>(rules);
        foreach (var rule in editBehavior.rules)
        {
            int prevIndex = toDestroy.FindIndex(r => r.editRule == rule);
            if (prevIndex == -1)
            {
                // New rule
                // Check if we should hide it
                if (!UIParameterEnum.ShouldSkipValue(rule.condition.type))
                {
                    var ruleui = GameObject.Instantiate<UIRuleToken>(ruleTokenPrefab, Vector3.zero, Quaternion.identity, rulesRoot);
                    ruleui.Setup(rule);
                    ruleui.onClick.AddListener(() => EditRule(rule));
                    ruleui.onEdit.AddListener(() => EditRule(rule));
                    ruleui.onMoveUp.AddListener(() => MoveUp(rule));
                    ruleui.onMoveDown.AddListener(() => MoveDown(rule));
                    ruleui.onDuplicate.AddListener(() => DuplicateRule(rule));
                    ruleui.onRemove.AddListener(() => DeleteRule(rule));
                    ruleui.onExpand.AddListener(() => ExpandRule(rule));
                    rules.Add(ruleui);
                    spacer.SetAsLastSibling();
                }
            }
            else
            {
                var ruleui = toDestroy[prevIndex];

                // Still there, don't update it
                toDestroy.RemoveAt(prevIndex);

                // However, it may need to refresh its display
                ruleui.Refresh();

                // Fix the sibling index
                int order = editBehavior.rules.IndexOf(rule);
                ruleui.transform.SetSiblingIndex(order);
            }
        }

        // Remove all remaining rule UIs
        foreach (var ruleui in toDestroy)
        {
            rules.Remove(ruleui);
            GameObject.Destroy(ruleui.gameObject);
        }
    }

    void EditRule(EditRule rule)
    {
        var ruleBeforeEditing = new EditRule();
        rule.CopyTo(ruleBeforeEditing);
        hasRuleChanged = () => !ruleBeforeEditing.IsSame(rule);
        NavigationManager.Instance.GoToPage(UIPage.PageId.Rule, rule);
    }

    void MoveUp(EditRule rule)
    {
        int index = editBehavior.rules.IndexOf(rule);
        if (index > 0)
        {
            editBehavior.rules.RemoveAt(index);
            editBehavior.rules.Insert(index - 1, rule);
            pageDirty = true;
            RefreshView();
        }
    }

    void MoveDown(EditRule rule)
    {
        int index = editBehavior.rules.IndexOf(rule);
        if (index < editBehavior.rules.Count - 1)
        {
            editBehavior.rules.RemoveAt(index);
            editBehavior.rules.Insert(index + 1, rule);
            pageDirty = true;
            RefreshView();
        }
    }

    void DuplicateRule(EditRule rule)
    {
        var newRule = rule.Duplicate();
        editBehavior.rules.Add(newRule);
        pageDirty = true;
        RefreshView();
    }

    void DeleteRule(EditRule rule)
    {
        PixelsApp.Instance.ShowDialogBox("Delete Rule?", "Are you sure you want to delete this rule?", "Ok", "Cancel", res =>
        {
            if (res)
            {
                editBehavior.rules.Remove(rule);
                pageDirty = true;
                RefreshView();
            }
        });
    }

    void ExpandRule(EditRule rule)
    {
        foreach (var uip in rules)
        {
            if (uip.editRule == rule)
            {
                uip.Expand(!uip.isExpanded);
            }
            else
            {
                uip.Expand(false);
            }
        }
    }

    void SetName(string newName)
    {
        if (editBehavior.name != newName)
        {
            editBehavior.name = newName;
            pageDirty = true;
        }
    }

    void SetDescription(string newDescription)
    {
        if (editBehavior.description != newDescription)
        {
            editBehavior.description = newDescription;
            pageDirty = true;
        }
    }

    void ActivateBehavior()
    {
        PixelsApp.Instance.ActivateBehavior(editBehavior, (die, res) =>
        {
            if (res)
            {
                AppDataSet.Instance.SaveData();
                pageDirty = false;
            }
        });
    }
}
