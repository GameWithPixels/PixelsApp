using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Systemic.Unity.Pixels.Profiles;
using System.Linq;

public class UIRuleView
    : UIPage
{
    [Header("Controls")]
    public RectTransform contentRoot;
    public Button addActionButton;

    [Header("Prefabs")]
    public UIRuleConditionToken conditionPrefab;
    public UIRuleActionToken actionPrefab;

    public EditRule editRule { get; private set; }

    EditRule workingRule;
    UIRuleConditionToken conditionToken;
    readonly List<UIRuleActionToken> actionTokens = new List<UIRuleActionToken>();

    public override void Enter(object context)
    {
        gameObject.SetActive(true);
        base.SetupHeader(false, false, "Edit Rule", null);
        if (context is EditRule rule)
        {
            editRule = rule;
            workingRule = editRule.Duplicate();
            SetupTokens();
        }

        if (AppSettings.Instance.ruleTutorialEnabled)
        {
            Tutorial.Instance.StartRuleTutorial();
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
        base.SetupHeader(false, false, "Edit Rule", null);
        ClearTokens();
        SetupTokens();
    }

    public override void Leave()
    {
        ClearTokens();
        gameObject.SetActive(false);
    }

    void ClearTokens()
    {
        if (conditionToken != null)
        {
            conditionToken.onConditionChanged -= OnConditionChange;
            GameObject.Destroy(conditionToken.gameObject);
            conditionToken = null;
        }
        foreach (var actionToken in actionTokens)
        {
            actionToken.onActionChanged -= OnActionChange;
            GameObject.Destroy(actionToken.gameObject);
        }
        actionTokens.Clear();
    }

    void SetupTokens()
    {
        conditionToken = GameObject.Instantiate<UIRuleConditionToken>(conditionPrefab, Vector3.zero, Quaternion.identity, contentRoot);
        conditionToken.Setup(workingRule, workingRule.condition);
        conditionToken.onConditionChanged += OnConditionChange;
        for (int i = 0; i < workingRule.actions.Count; ++i)
        {
            var action = workingRule.actions[i];
            AddActionToken(action, i == 0);
        }

        addActionButton.transform.SetAsLastSibling();
    }

    void Awake()
    {
        addActionButton.onClick.AddListener(AddAction);
    }

    public override void OnBack()
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
                        NavigationManager.Instance.GoBack();
                        pageDirty = false;
                    }
                });
        }
        else
        {
            NavigationManager.Instance.GoBack();
        }
    }

    public override void OnSave()
    {
        workingRule.CopyTo(editRule);
        //AppDataSet.Instance.SaveData(); // Change will be saved when parent preset is saved
        pageDirty = false;
        NavigationManager.Instance.GoBack();
    }

    void AddAction()
    {
        var action = EditAction.Create(ActionType.PlayAnimation);
        AddActionToken(action, false);
        workingRule.actions.Add(action);
        base.pageDirty = true;
    }

    void AddActionToken(EditAction action, bool first)
    {
        var actionToken = GameObject.Instantiate<UIRuleActionToken>(actionPrefab, Vector3.zero, Quaternion.identity, contentRoot);
        UpdateActionToken(actionToken, action, first);
        actionToken.onDelete.AddListener(() => DeleteAction(action));
        actionToken.onActionChanged += OnActionChange;
        actionTokens.Add(actionToken);
    }

    void UpdateActionToken(UIRuleActionToken token, EditAction action, bool first)
    {
        token.Setup(workingRule, action, first);
    }

    void DestroyActionToken(EditAction action)
    {
        var index = actionTokens.FindIndex(at => at.editAction == action);
        var token = actionTokens[index];
        GameObject.Destroy(token.gameObject);
        actionTokens.RemoveAt(index);
    }

    void DeleteAction(EditAction action)
    {
        if (workingRule.actions.Count > 1)
        {
            PixelsApp.Instance.ShowDialogBox("Delete Action?", "Are you sure you want to delete this action?", "Ok", "Cancel", res =>
            {
                if (res)
                {
                    DestroyActionToken(action);
                    workingRule.actions.Remove(action);
                    base.pageDirty = true;
                }
            });
        }
        else
        {
            PixelsApp.Instance.ShowDialogBox("Can't Delete last action", "You must have at least one action in a rule.");
        }
        // Else can't delete last action
    }

    void OnConditionChange(EditRule rule, EditCondition condition)
    {
        base.pageDirty = true;
    }

    void OnActionChange(EditRule rule, EditAction action)
    {
        base.pageDirty = true;
    }
}
