using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using Systemic.Unity.Pixels.Profiles;
using Systemic.Unity.Pixels.Animations;

[System.Serializable]
public class EditRule
    : EditObject
{
    public EditCondition condition;
    public List<EditAction> actions = new List<EditAction>();

    public Rule ToRule(EditDataSet editSet, DataSet set)
    {
        // Create our condition
        var cond = condition.ToCondition(editSet, set);
        set.conditions.Add(cond);
        int conditionIndex = set.conditions.Count - 1;

        // Create our action
        int actionOffset = set.actions.Count;
        foreach (var editAction in actions)
        {
            var act = editAction.ToAction(editSet, set);
            set.actions.Add(act);
        }

        return new Rule()
        {
            condition = (ushort)conditionIndex,
            actionOffset = (ushort)actionOffset,
            actionCount = (ushort)actions.Count,
        };
    }

    public EditRule Duplicate()
    {
        var actionsCopy = new List<EditAction>();
        foreach (var action in actions)
        {
            actionsCopy.Add(action.Duplicate());
        }
        return new EditRule()
        {
            condition = condition.Duplicate(),
            actions = actionsCopy
        };
    }

    public void CopyTo(EditRule dest)
    {
        dest.condition = condition.Duplicate();
        dest.actions.Clear();
        foreach (var action in actions)
        {
            dest.actions.Add(action.Duplicate());
        }
    }

    public void ReplaceAction(EditAction prevAction, EditAction newAction)
    {
        int index = actions.IndexOf(prevAction);
        actions[index] = newAction;
    }

    public void ReplaceAnimation(EditAnimation oldAnimation, EditAnimation newAnimation)
    {
        foreach (var action in actions)
        {
            action.ReplaceAnimation(oldAnimation, newAnimation);
        }
    }

    public void DeleteAnimation(EditAnimation animation)
    {
        foreach (var action in actions)
        {
            action.DeleteAnimation(animation);
        }
    }

    public bool DependsOnAnimation(EditAnimation animation)
    {
        return actions.Any(a => a.DependsOnAnimation(animation));
    }

    public void DeleteAudioClip(AudioClips.EditAudioClip clip)
    {
        foreach (var action in actions)
        {
            action.DeleteAudioClip(clip);
        }
    }

    public bool DependsOnAudioClip(AudioClips.EditAudioClip clip)
    {
        return actions.Any(a => a.DependsOnAudioClip(clip));
    }

    // Don't want to override Equals
    public bool IsSame(EditRule rule)
    {
        static bool IsSameCondition(EditCondition condition1, EditCondition condition2)
            => (condition1 == condition2) || ((condition1 != null) && (condition2 != null) && condition1.IsSame(condition2));

        static bool IsSameActionList(List<EditAction> actions1, List<EditAction> actions2)
            => (actions1 == actions2) || ((actions1 != null) && (actions2 != null) && actions1.SequenceEqual(actions2, new EditActionEqualityComparer()));

        return (this == rule) || ((rule != null) && IsSameCondition(condition, rule.condition) && IsSameActionList(actions, rule.actions));
    }

    public class EditActionEqualityComparer : IEqualityComparer<EditAction>
    {
        public bool Equals(EditAction x, EditAction y) => (x == y) || ((x != null) && (y != null) && x.IsSame(y));

        public int GetHashCode(EditAction obj) => obj.GetHashCode();
    }
}
