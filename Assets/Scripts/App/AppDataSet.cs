using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Systemic.Unity.Pixels.Animations;
using Systemic.Unity.Pixels.Profiles;
using Presets;
using Dice;
using AudioClips;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Systemic.Unity.Pixels;

[System.Serializable]
public class PreviewSettings
{
    public PixelDesignAndColor design;
}

public class AppDataSet : SingletonMonoBehaviour<AppDataSet>
{
    [System.Serializable]
    public class Data
    {
        public int jsonVersion = 1;
        public List<EditDie> dice = new List<EditDie>();
        public List<EditAudioClip> audioClips = new List<EditAudioClip>();
        public List<EditPattern> patterns = new List<EditPattern>();
        public List<EditAnimation> animations = new List<EditAnimation>();
        public List<EditProfile> behaviors = new List<EditProfile>();
        public List<EditPreset> presets = new List<EditPreset>();
        public EditProfile defaultBehavior = null;
        public uint nextAudioClipUniqueId = 0;

        public void Clear()
        {
            dice.Clear();
            audioClips.Clear();
            patterns.Clear();
            animations.Clear();
            behaviors.Clear();
            presets.Clear();
            defaultBehavior = null;
            nextAudioClipUniqueId = 0;
        }
    }

    readonly Data data = new Data();
    
    public string pathname => Path.Combine(Application.persistentDataPath, AppConstants.Instance.DataSetFilename);

    public List<EditDie> dice => data.dice;
    public List<EditAudioClip> audioClips => data.audioClips;
    public List<EditPattern> patterns => data.patterns;
    public List<EditAnimation> animations => data.animations;
    public List<EditProfile> profiles => data.behaviors;
    public List<EditPreset> presets => data.presets;
    public EditProfile defaultProfile { get { return data.defaultBehavior; } set { data.defaultBehavior = value; } }

    void OnEnable()
    {
        LoadData();
    }

    JsonSerializer CreateSerializer()
    {
        var serializer = new JsonSerializer();
        serializer.Converters.Add(new EditAnimationConverter());
        serializer.Converters.Add(new EditAnimationGradientPattern.Converter(this));
        serializer.Converters.Add(new EditAnimationKeyframed.Converter(this));
        serializer.Converters.Add(new EditActionConverter());
        serializer.Converters.Add(new EditActionPlayAnimation.Converter(this));
        serializer.Converters.Add(new EditActionPlayAudioClip.Converter(this));
        serializer.Converters.Add(new EditDieAssignmentConverter(this));
        serializer.Converters.Add(new EditConditionConverter());
        return serializer;
    }

    public void ToJson(JsonWriter writer, JsonSerializer serializer)
    {
        foreach (var editDie in dice)
        {
            editDie.OnBeforeSerialize();
        }
        serializer.Serialize(writer, data);
    }

    public void FromJson(JsonReader reader, JsonSerializer serializer)
    {
        data.Clear();
        serializer.Populate(reader, data);
        // Remove dice with no system id (previous versions had a device id instead)
        foreach (var d in dice.Where(d => string.IsNullOrEmpty(d.systemId)))
        {
            Debug.LogWarning($"Removing die {d.name} from data because it has no system id");
        }
        dice.RemoveAll(d => string.IsNullOrEmpty(d.systemId));
        // Remove dice with no system id from presets
        foreach (var pr in presets)
        {
            foreach (var assignement in pr.dieAssignments)
            {
                if (assignement.die != null && string.IsNullOrEmpty(assignement.die.systemId))
                {
                    assignement.die = null;
                }
            }
        }

        foreach (var editDie in data.dice)
        {
            editDie.OnAfterDeserialize();
        }
    }

    public EditDataSet ExtractEditSetForAnimation(EditAnimation animation)
    {
        // The EditDataSet that will only contain the given animation and its patterns
        var ret = new EditDataSet();

        // Add the single animation we need
        ret.animations.Add(animation);

        // Include all patterns used by the animations
        foreach (var pattern in patterns)
        {
            if (animation.DependsOnPattern(pattern, out bool asRGB))
            {
                if (asRGB)
                {
                    ret.rgbPatterns.Add(pattern);
                }
                else
                {
                    ret.patterns.Add(pattern);
                }
            }
        }

        return ret;
    }

    public EditDataSet ExtractEditSetForProfile(EditProfile profile)
    {
        if (!profiles.Contains(profile))
        {
            throw new System.ArgumentException(nameof(profile), "Profile not in AppDataSet");
        }

        // The EditDataSet that will only contain the animations and their patterns
        // for the given profile
        var editSet = new EditDataSet(profile.Duplicate());

        // And add the animations that the given profile uses
        var animations = editSet.profile.CollectAnimations();

        // Add default rules and animations to profile / set
        if (defaultProfile != null)
        {
            // Rules that are in fact copied over
            var copiedRules = new List<EditRule>();

            foreach (var rule in defaultProfile.rules)
            {
                if (rule.condition != null &&
                    !editSet.profile.rules.Any(r => r.condition?.type == rule.condition.type))
                {
                    var ruleCopy = rule.Duplicate();
                    copiedRules.Add(ruleCopy);
                    editSet.profile.rules.Add(ruleCopy);
                }
            }

            // Copied animations
            var copiedAnims = new Dictionary<EditAnimation, EditAnimation>();

            // Add animations used by default rules
            foreach (var editAnim in defaultProfile.CollectAnimations())
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

        // Copy our animations list to the resulting EditDataSet
        editSet.animations.AddRange(animations);

        // Include all patterns used by the animations
        foreach (var pattern in patterns)
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

    public EditDie GetEditDie(Pixel die)
    {
        return dice.FirstOrDefault(d => d.die == die);
    }

    public EditDie GetEditDie(string systemId)
    {
        return dice.FirstOrDefault(d => d.systemId == systemId);
    }

    public EditAnimation AddNewDefaultAnimation()
    {
        var newAnim = new EditAnimationSimple
        {
            duration = 3.0f,
            color = EditColor.FromColor(new Color32(0xFF, 0x30, 0x00, 0xFF)),
            faces = 0b11111111111111111111,
            name = "New Lighting Pattern"
        };
        animations.Add(newAnim);
        return newAnim;
    }

    public EditAnimation DuplicateAnimation(EditAnimation animation)
    {
        var newAnim = animation.Duplicate();
        animations.Add(newAnim);
        return newAnim;
    }

    public void ReplaceAnimation(EditAnimation oldAnimation, EditAnimation newAnimation)
    {
        foreach (var profile in profiles)
        {
            profile.ReplaceAnimation(oldAnimation, newAnimation);
        }
        int oldAnimIndex = animations.IndexOf(oldAnimation);
        animations[oldAnimIndex] = newAnimation;
    }

    public void DeleteAnimation(EditAnimation animation)
    {
        foreach (var profile in profiles)
        {
            profile.DeleteAnimation(animation);
        }
        animations.Remove(animation);
    }

    public EditPattern AddNewDefaultPattern()
    {
        var newPattern = new EditPattern();
        newPattern.name = "New Pattern";
        for (int i = 0; i < 20; ++i)
        {
            var grad = new EditRGBGradient();
            grad.keyframes.Add(new EditRGBKeyframe() { time = 0.0f, color = Color.black });
            grad.keyframes.Add(new EditRGBKeyframe() { time = 0.5f, color = Color.white });
            grad.keyframes.Add(new EditRGBKeyframe() { time = 1.0f, color = Color.black });
            newPattern.gradients.Add(grad);
        }
        patterns.Add(newPattern);
        return newPattern;
    }

    public void ReplacePattern(EditPattern oldPattern, EditPattern newPattern)
    {
        foreach (var animation in animations)
        {
            animation.ReplacePattern(oldPattern, newPattern);
        }
        int oldPatternIndex = patterns.IndexOf(oldPattern);
        patterns[oldPatternIndex] = newPattern;
    }

    public void DeletePattern(EditPattern pattern)
    {
        EditPattern replacementPattern = null;
        foreach (var animation in animations)
        {
            if (animation.DependsOnPattern(pattern, out bool _))
            {
                if (replacementPattern == null)
                {
                    // We can't have a null pattern in an animation so create a new one for the occasion
                    replacementPattern = AddNewDefaultPattern();
                }
                animation.ReplacePattern(pattern, replacementPattern);
            }
        }
        patterns.Remove(pattern);
    }

    public IEnumerable<EditAnimation> CollectAnimationsForPattern(EditPattern pattern)
    {
        return animations.Where(b => b.DependsOnPattern(pattern, out bool _));
    }

    public IEnumerable<EditProfile> CollectProfilesForAnimation(EditAnimation anim)
    {
        return profiles.Where(b => b.DependsOnAnimation(anim));
    }

    public IEnumerable<EditPreset> CollectPresetsForAnimation(EditAnimation anim)
    {
        var profiles = CollectProfilesForAnimation(anim);
        return presets.Where(p => p.dieAssignments.Any(da => profiles.Contains(da.behavior)));
    }

    public EditProfile AddNewDefaultProfile()
    {
        var newProfile = new EditProfile();
        newProfile.name = "New Profile";
        newProfile.rules.Add(new EditRule(new List<EditAction>()
        {
            new EditActionPlayAnimation()
            {
                animation = null,
                faceIndex = 0,
                loopCount = 1
            }
        })
        {
            condition = new EditConditionFaceCompare()
            {
                flags = FaceCompareFlags.Equal,
                faceIndex = 19
            },
        });
        profiles.Add(newProfile);
        return newProfile;
    }

    public EditProfile DuplicateProfile(EditProfile profile)
    {
        var newProfile = profile.Duplicate();
        profiles.Add(newProfile);
        return newProfile;
    }

    public void DeleteProfile(EditProfile profile)
    {
        foreach (var preset in presets)
        {
            preset.DeleteProfile(profile);
        }
        profiles.Remove(profile);
    }

    public IEnumerable<EditPreset> CollectPresetsForBehavior(EditProfile profile)
    {
        return presets.Where(b => b.DependsOnProfile(profile));
    }

    public bool CheckDependency(EditDie die)
    {
        bool dependencyFound = false;
        foreach (var preset in presets)
        {
            dependencyFound = dependencyFound | preset.CheckDependency(die);
        }
        return dependencyFound;
    }

    public EditPreset AddNewDefaultPreset()
    {
        var newPreset = new EditPreset();
        newPreset.name = "New Preset";
        newPreset.dieAssignments.Add(new EditDieAssignment()
        {
            die = null,
            behavior = null
        });
        presets.Add(newPreset);
        return newPreset;
    }

    public EditPreset DuplicatePreset(EditPreset editPreset)
    {
        var newPreset = editPreset.Duplicate();
        presets.Add(newPreset);
        return newPreset;
    }

    public void DeletePreset(EditPreset editPreset)
    {
        presets.Remove(editPreset);
    }

    public IEnumerable<EditPreset> CollectPresetsForDie(EditDie die)
    {
        return presets.Where(b => b.DependsOnDie(die));
    }

    public void DeleteDie(EditDie editDie)
    {
        foreach (var preset in presets)
        {
            preset.DeleteDie(editDie);
        }
        dice.Remove(editDie);
    }

    public EditAudioClip FindAudioClip(string fileName)
    {
        return audioClips.FirstOrDefault(a => a.name == fileName);
    }

    public EditAudioClip FindAudioClip(uint clipId)
    {
        return audioClips.FirstOrDefault(a => a.id == clipId);
    }

    public EditAudioClip AddAudioClip(string fileName)
    {
        var ret = new EditAudioClip()
        {
            name = fileName,
            id = data.nextAudioClipUniqueId++
        };
        audioClips.Add(ret);
        return ret;
    }

    public void DeleteAudioClip(EditAudioClip clip)
    {
        foreach (var profile in profiles)
        {
            profile.DeleteAudioClip(clip);
        }
        audioClips.Remove(clip);
    }

    public IEnumerable<EditProfile> CollectProfilesForAudioClip(EditAudioClip clip)
    {
        return profiles.Where(b => b.DependsOnAudioClip(clip));
    }

    public IEnumerable<EditPreset> CollectPresetsForAudioClip(EditAudioClip clip)
    {
        var profiles = CollectProfilesForAudioClip(clip).ToList();
        return presets.Where(p => p.dieAssignments.Any(da => profiles.Contains(da.behavior)));
    }

    /// <summary>
    /// Load our pool from file
    /// </sumary>
    public void LoadData()
    {
        var serializer = CreateSerializer();

        if (File.Exists(pathname))
        {
            Debug.Log("AppDataSet: loading user's file " + pathname);

            using StreamReader sw = new StreamReader(pathname);
            using JsonReader reader = new JsonTextReader(sw);
            FromJson(reader, serializer);
        }
        else
        {
            Debug.Log("AppDataSet: loading default contents");

            using StringReader sw = new StringReader(AppConstants.Instance.defaultDiceJson.text);
            using JsonReader reader = new JsonTextReader(sw);
            FromJson(reader, serializer);
        }
    }

    /// <summary>
    /// Save our pool to file
    /// </sumary>
    public void SaveData()
    {
        var stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();

        Debug.Log("AppDataSet: saving to user's file " + pathname);

        var serializer = CreateSerializer();
        using (StreamWriter sw = new StreamWriter(pathname))
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            writer.Formatting = Formatting.Indented;
            ToJson(writer, serializer);
        }

        stopWatch.Stop();
        Debug.Log($"AppDataSet: it took {stopWatch.Elapsed.TotalMilliseconds} ms to serialize data to JSON file");
    }

    public void ExportAnimation(EditAnimation animation, string jsonFilePath)
    {
        var editSet = ExtractEditSetForAnimation(animation);
        var serializer = CreateSerializer();
        using (StreamWriter sw = new StreamWriter(jsonFilePath))
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            writer.Formatting = Formatting.Indented;
            serializer.Serialize(writer, editSet);
        }
    }

    public void ImportAnimation(string jsonFilePath)
    {
        if (File.Exists(jsonFilePath))
        {
            var serializer = CreateSerializer();
            using (StreamReader sw = new StreamReader(jsonFilePath))
            using (JsonReader reader = new JsonTextReader(sw))
            {
                var editSet = new EditDataSet();
                serializer.Populate(reader, editSet);

                // Now merge the data into the app data set
                MergeEditSet(editSet);
                SaveData();
            }
        }
    }

    public void MergeEditSet(EditDataSet set)
    {
        data.patterns.AddRange(set.patterns);
        data.patterns.AddRange(set.rgbPatterns);
        data.animations.AddRange(set.animations);
        if (set.profile != null)
        {
            data.behaviors.Add(set.profile);
        }
    }
    
    public static AppDataSet CreateTestDataSet()
    {
        AppDataSet ret = new AppDataSet();

        // We only save the dice that we have indicated to be in the pool
        // (i.e. ignore dice that are 'new' and we didn't connect to)
        var die0 = new EditDie()
        {
            name = "Die 000",
            systemId = "test:123456789ABCDEF0",
            ledCount = 20,
            designAndColor = PixelDesignAndColor.V3Orange
        };
        ret.dice.Add(die0);
        var die1 = new EditDie()
        {
            name = "Die 001",
            systemId = "test:ABCDEF0123456789",
            ledCount = 20,
            designAndColor = PixelDesignAndColor.V5Black
        };
        ret.dice.Add(die1);
        var die2 = new EditDie()
        {
            name = "Die 002",
            systemId = "test:CDEF0123456789AB",
            ledCount = 20,
            designAndColor = PixelDesignAndColor.V5Grey
        };
        ret.dice.Add(die2);
        var die3 = new EditDie()
        {
            name = "Die 003",
            systemId = "test:EF0123456789ABCD",
            ledCount = 20,
            designAndColor = PixelDesignAndColor.V5Gold
        };
        ret.dice.Add(die3);
        
        EditAnimationSimple simpleAnim = new EditAnimationSimple();
        simpleAnim.duration = 1.0f;
        simpleAnim.color = EditColor.FromColor(Color.blue);
        simpleAnim.faces = 0b11111111111111111111;
        simpleAnim.name = "Simple Anim 1";
        ret.animations.Add(simpleAnim);

        EditProfile profile = new EditProfile();
        profile.rules.Add(new EditRule(new List<EditAction>()
        {
            new EditActionPlayAnimation() { animation = simpleAnim, faceIndex = 0, loopCount = 1 }
        }) {
            condition = new EditConditionRolling(),
        });
        profile.rules.Add(new EditRule(new List<EditAction>()
        {
            new EditActionPlayAnimation() { animation = simpleAnim, faceIndex = 19, loopCount = 1 }
        }) {
            condition = new EditConditionFaceCompare()
            {
                faceIndex = 19,
                flags = FaceCompareFlags.Equal
            },
        });
        profile.rules.Add(new EditRule(new List<EditAction>()
        {
            new EditActionPlayAnimation() { animation = simpleAnim, faceIndex = 2, loopCount = 1 }
        }) {
            condition = new EditConditionFaceCompare()
            {
                faceIndex = 0,
                flags = FaceCompareFlags.Less | FaceCompareFlags.Equal | FaceCompareFlags.Greater
            },
        });
        ret.profiles.Add(profile);

        ret.presets.Add(new EditPreset()
        {
            name = "Preset 0",
            dieAssignments = new List<EditDieAssignment>()
            {
                new EditDieAssignment()
                {
                    die = die0,
                    behavior = profile
                },
                new EditDieAssignment()
                {
                    die = die1,
                    behavior = profile
                }
            }
        });

        return ret;
    }
}
