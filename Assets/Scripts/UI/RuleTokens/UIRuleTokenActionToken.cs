using System.Collections;
using System.Collections.Generic;
using Systemic.Unity.Pixels.Profiles;
using UnityEngine;

public abstract class UIRuleTokenActionToken : MonoBehaviour
{
    public abstract IEnumerable<ActionType> actionTypes { get; }
    public abstract void Setup(EditAction action, bool first);
}
