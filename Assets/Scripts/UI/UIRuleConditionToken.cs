using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Systemic.Unity.Pixels.Profiles;

public class UIRuleConditionToken : MonoBehaviour
{
    [Header("Controls")]
    public UIParameterEnum conditionSelector;
    public RectTransform parametersRoot;

    public EditRule parentRule { get; private set; }
    public EditCondition editCondition { get; private set; }

    UIParameterManager.ObjectParameterList parameters;

    public delegate void ConditionChangedEvent(EditRule rule, EditCondition condition);
    public ConditionChangedEvent onConditionChanged;

    void OnDestroy()
    {
        foreach (var parameter in parameters.parameters)
        {
            GameObject.Destroy(parameter.gameObject);
        }
        parameters.onParameterChanged -= OnConditionChanged;
        parameters = null;
    }

    public void Setup(EditRule rule, EditCondition condition)
    {
        parentRule = rule;
        editCondition = condition;
        conditionSelector.Setup(
            "Condition Type",
            () => editCondition.type,
            (t) => SetConditionType((ConditionType)t),
            null);

        // Setup all other parameters
        parameters = UIParameterManager.Instance.CreateControls(condition, parametersRoot);
        parameters.onParameterChanged += OnConditionChanged;
    }

    void SetConditionType(ConditionType newType)
    {
        if (newType != editCondition.type)
        {
            onConditionChanged?.Invoke(parentRule, editCondition);

            // Change the type, which really means create a new condition and replace the old one
            var newCondition = EditCondition.Create(newType);

            // Replace the condition
            parentRule.condition = newCondition;

            // Setup the parameters again
            foreach (var parameter in parameters.parameters)
            {
                GameObject.Destroy(parameter.gameObject);
            }
            parameters = UIParameterManager.Instance.CreateControls(newCondition, parametersRoot);
            parameters.onParameterChanged += OnConditionChanged;

            editCondition = newCondition;

            onConditionChanged?.Invoke(parentRule, editCondition);
        }
    }

    void OnConditionChanged(EditObject parentObject, UIParameter parameter, object newValue)
    {
        onConditionChanged?.Invoke(parentRule, editCondition);
    }
}
