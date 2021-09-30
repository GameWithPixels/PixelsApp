using System.Collections;
using System.Collections.Generic;
using Systemic.Unity.Pixels.Profiles;
using UnityEngine;

public abstract class UIRuleTokenConditionToken : MonoBehaviour
{
    public abstract IEnumerable<ConditionType> conditionTypes { get; }
    public abstract void Setup(EditCondition condition);
}
