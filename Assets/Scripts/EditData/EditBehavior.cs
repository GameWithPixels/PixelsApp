using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systemic.Unity.Pixels;
using Systemic.Unity.Pixels.Animations;
using Systemic.Unity.Pixels.Profiles;

[System.Serializable]
public class EditBehavior
    : EditObject
{
    public string name;
    public string description;
    public readonly List<EditRule> rules = new List<EditRule>();

    public PreviewSettings defaultPreviewSettings = new PreviewSettings() { design = PixelDesignAndColor.V5_Grey };

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

    public EditBehavior Duplicate()
    {
        var ret = new EditBehavior
        {
            name = name,
            description = description
        };
        foreach (var r in rules)
        {
            ret.rules.Add(r.Duplicate());
        }
        return ret;
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

    public EditDataSet ToEditSet()
    {
        // Generate the data to be uploaded, based on a copy of the profile
        var editSet = new EditDataSet(Duplicate());

        // And add the animations that this behavior uses
        var animations = editSet.profile.CollectAnimations();

        // Add default rules and animations to behavior / set
        if (AppDataSet.Instance.defaultBehavior != null)
        {
            // Rules that are in fact copied over
            var copiedRules = new List<EditRule>();

            foreach (var rule in AppDataSet.Instance.defaultBehavior.rules)
            {
                if (!editSet.profile.rules.Any(r => r.condition.type == rule.condition.type))
                {
                    var ruleCopy = rule.Duplicate();
                    copiedRules.Add(ruleCopy);
                    editSet.profile.rules.Add(ruleCopy);
                }
            }

            // Copied animations
            var copiedAnims = new Dictionary<EditAnimation, EditAnimation>();

            // Add animations used by default rules
            foreach (var editAnim in AppDataSet.Instance.defaultBehavior.CollectAnimations())
            {
                foreach (var copiedRule in copiedRules)
                {
                    if (copiedRule.DependsOnAnimation(editAnim))
                    {
                        EditAnimation copiedAnim = null;
                        if (!copiedAnims.TryGetValue(editAnim, out copiedAnim))
                        {
                            copiedAnim = editAnim.Duplicate();
                            animations.Add(copiedAnim);
                            copiedAnims.Add(editAnim, copiedAnim);
                        }
                        copiedRule.ReplaceAnimation(editAnim, copiedAnim);
                    }
                }
            }
        }

        editSet.animations.AddRange(animations);

        foreach (var pattern in AppDataSet.Instance.patterns)
        {
            bool asRGB = false;
            if (animations.Any(anim => anim.DependsOnPattern(pattern, out asRGB)))
            {
                if (asRGB)
                {
                    editSet.rgbPatterns.Add(pattern);
                }
                else
                {
                    editSet.patterns.Add(pattern);
                }
            }
        }

        return editSet;
    }
}
