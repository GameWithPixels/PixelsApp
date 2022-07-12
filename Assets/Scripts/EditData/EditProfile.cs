using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systemic.Unity.Pixels;
using Systemic.Unity.Pixels.Animations;
using Systemic.Unity.Pixels.Profiles;

[System.Serializable]
public class EditProfile
    : EditObject
{
    public string name;
    public string description;
    public readonly List<EditRule> rules;

    public readonly PreviewSettings defaultPreviewSettings = new PreviewSettings() { design = PixelDesignAndColor.V5_Grey };

    public EditProfile(List<EditRule> rules = null)
    {
        this.rules = rules ?? new List<EditRule>();
    }

    public Profile ToProfile(EditDataSet editSet, DataSet set)
    {
        // Add our rules to the set
        int rulesOffset = set.rules.Count;
        foreach (var editRule in rules)
        {
            var rule = editRule.ToRule(editSet, set);
            set.rules.Add(rule);
        }

        return new Profile()
        {
            rulesOffset = (ushort)rulesOffset,
            rulesCount = (ushort)rules.Count,
        };
    }

    public EditProfile Duplicate()
    {
        return new EditProfile(rules.Select(r => r.Duplicate()).ToList())
        {
            name = name,
            description = description,
        };
    }

    public EditRule AddNewDefaultRule()
    {
        var ret = new EditRule(new List<EditAction>()
        {
            new EditActionPlayAnimation()
            {
                animation = null,
                faceIndex = -1,
                loopCount = 1
            }
        })
        {
            condition = new EditConditionFaceCompare()
            {
                flags = ConditionFaceCompare_Flags.Equal,
                faceIndex = 19
            },
        };
        rules.Add(ret);
        return ret;
    }

    public void ReplaceAnimation(EditAnimation oldAnimation, EditAnimation newAnimation)
    {
        foreach (var rule in rules)
        {
            rule.ReplaceAnimation(oldAnimation, newAnimation);
        }
    }

    public void DeleteAnimation(EditAnimation animation)
    {
        foreach (var rule in rules)
        {
            rule.DeleteAnimation(animation);
        }
    }

    public bool DependsOnAnimation(EditAnimation animation)
    {
        return rules.Any(r => r.DependsOnAnimation(animation));
    }

    public bool DependsOnAudioClip(AudioClips.EditAudioClip clip)
    {
        return rules.Any(r => r.DependsOnAudioClip(clip));
    }

    public List<EditAnimation> CollectAnimations()
    {
        var ret = new List<EditAnimation>();
        foreach (var action in rules.SelectMany(r => r.actions))
        {
            foreach (var anim in action.CollectAnimations())
            {
                if (!ret.Contains(anim))
                {
                    ret.Add(anim);
                }
            }
        }
        return ret;
    }

    public void DeleteAudioClip(AudioClips.EditAudioClip clip)
    {
        foreach (var rule in rules)
        {
            rule.DeleteAudioClip(clip);
        }
    }

    public IEnumerable<AudioClips.EditAudioClip> CollectAudioClips()
    {
        foreach (var action in rules.SelectMany(r => r.actions))
        {
            foreach (var clip in action.CollectAudioClips())
            {
                yield return clip;
            }
        }
    }
}
