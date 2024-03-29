﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Systemic.Unity.Pixels.Profiles;

public class UIRuleActionToken : MonoBehaviour
{
    [Header("Controls")]
    public UIParameterEnum actionSelector;
    public RectTransform parametersRoot;
    public Text labelText;
    public Button deleteButton;

    public EditRule parentRule { get; private set; }
    public EditAction editAction { get; private set; }
    public Button.ButtonClickedEvent onDelete => deleteButton.onClick;

    UIParameterManager.ObjectParameterList parameters;

    public delegate void ActionChangedEvent(EditRule rule, EditAction action);
    public ActionChangedEvent onActionChanged;

    void OnDestroy()
    {
        foreach (var parameter in parameters.parameters)
        {
            GameObject.Destroy(parameter.gameObject);
        }
        parameters.onParameterChanged -= OnActionChanged;
        parameters = null;
    }

    public void Setup(EditRule rule, EditAction action, bool first)
    {
        parentRule = rule;
        editAction = action;
        labelText.text = first ? "Then" : "And";
        actionSelector.Setup(
            "Action Type",
            () => editAction.type,
            (t) => SetActionType((ActionType)t),
            null);

        // Setup all other parameters
        parameters = UIParameterManager.Instance.CreateControls(action, parametersRoot);
        parameters.onParameterChanged += OnActionChanged;
    }

    void SetActionType(ActionType newType)
    {
        if (newType != editAction.type)
        {
            onActionChanged?.Invoke(parentRule, editAction);
    
            // Change the type, which really means create a new action and replace the old one
            var newAction = EditAction.Create(newType);

            // Replace the action
            parentRule.ReplaceAction(editAction, newAction);

            // Setup the parameters again
            foreach (var parameter in parameters.parameters)
            {
                GameObject.Destroy(parameter.gameObject);
            }
            parameters = UIParameterManager.Instance.CreateControls(newAction, parametersRoot);

            editAction = newAction;
    
            onActionChanged?.Invoke(parentRule, editAction);
        }
    }

    void OnActionChanged(EditObject parentObject, UIParameter parameter, object newValue)
    {
        onActionChanged?.Invoke(parentRule, editAction);
    }
}
