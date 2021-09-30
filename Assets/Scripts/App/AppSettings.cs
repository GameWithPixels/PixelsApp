using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

public class AppSettings : SingletonMonoBehaviour<AppSettings>
{
    [System.Serializable]
    public class Data
    {
        public bool displayWhatsNew = true;

        // Tutorial is now disabled by default
        public bool mainTutorialEnabled = false;
        public bool homeTutorialEnabled = false;
        public bool presetsTutorialEnabled = false;
        public bool presetTutorialEnabled = false;
        public bool behaviorsTutorialEnabled = false;
        public bool behaviorTutorialEnabled = false;
        public bool ruleTutorialEnabled = false;
        public bool animationsTutorialEnabled = false;
        public bool animationTutorialEnabled = false;
    }
    
    readonly Data data = new Data();

    public string pathname => Path.Combine(Application.persistentDataPath, AppConstants.Instance.SettingsFilename);

    public bool displayWhatsNew => data.displayWhatsNew;
    public bool mainTutorialEnabled => data.mainTutorialEnabled;
    public bool homeTutorialEnabled => data.homeTutorialEnabled;
    public bool presetsTutorialEnabled => data.presetsTutorialEnabled;
    public bool presetTutorialEnabled => data.presetTutorialEnabled;
    public bool behaviorsTutorialEnabled => data.behaviorsTutorialEnabled;
    public bool behaviorTutorialEnabled => data.behaviorTutorialEnabled;
    public bool ruleTutorialEnabled => data.ruleTutorialEnabled;
    public bool animationsTutorialEnabled => data.animationsTutorialEnabled;
    public bool animationTutorialEnabled => data.animationTutorialEnabled;

    public void SetDisplayWhatsNew(bool value)
    {
        data.displayWhatsNew = value;
        SaveData();
    }

    public void SetMainTutorialEnabled(bool value)
    {
        data.mainTutorialEnabled = value;
        SaveData();
    }

    public void SetHomeTutorialEnabled(bool value)
    {
        data.homeTutorialEnabled = value;
        SaveData();
    }

    public void SetPresetsTutorialEnabled(bool value)
    {
        data.presetsTutorialEnabled = value;
        SaveData();
    }

    public void SetPresetTutorialEnabled(bool value)
    {
        data.presetTutorialEnabled = value;
        SaveData();
    }

    public void SetBehaviorsTutorialEnabled(bool value)
    {
        data.behaviorsTutorialEnabled = value;
        SaveData();
    }

    public void SetBehaviorTutorialEnabled(bool value)
    {
        data.behaviorTutorialEnabled = value;
        SaveData();
    }

    public void SetRuleTutorialEnabled(bool value)
    {
        data.ruleTutorialEnabled = value;
        SaveData();
    }

    public void SetAnimationsTutorialEnabled (bool value)
    {
        data.animationsTutorialEnabled = value;
        SaveData();
    }

    public void SetAnimationTutorialEnabled(bool value)
    {
        data.animationTutorialEnabled = value;
        SaveData();
    }

    public void EnableAllTutorials()
    {
        SetMainTutorialEnabled(true);
        SetHomeTutorialEnabled(true);
        SetPresetsTutorialEnabled(true);
        SetPresetTutorialEnabled(true);
        SetBehaviorsTutorialEnabled(true);
        SetBehaviorTutorialEnabled(true);
        SetRuleTutorialEnabled(true);
        SetAnimationsTutorialEnabled(true);
        SetAnimationTutorialEnabled(true);
    }

    public void DisableAllTutorials()
    {
        SetMainTutorialEnabled(false);
        SetHomeTutorialEnabled(false);
        SetPresetsTutorialEnabled(false);
        SetPresetTutorialEnabled(false);
        SetBehaviorsTutorialEnabled(false);
        SetBehaviorTutorialEnabled(false);
        SetRuleTutorialEnabled(false);
        SetAnimationsTutorialEnabled(false);
        SetAnimationTutorialEnabled(false);
    }

    JsonSerializer CreateSerializer()
    {
        var serializer = new JsonSerializer();
        return serializer;
    }

    public void ToJson(JsonWriter writer, JsonSerializer serializer)
    {
        serializer.Serialize(writer, data);
    }

    public void FromJson(JsonReader reader, JsonSerializer serializer)
    {
        serializer.Populate(reader, data); 
    }

    void OnEnable()
    {
        LoadData();
    }

    /// <summary>
    /// Load our pool from file
    /// </sumary>
    public void LoadData()
    {
        if (File.Exists(pathname))
        {
            var serializer = CreateSerializer();
            using (StreamReader sw = new StreamReader(pathname))
            using (JsonReader reader = new JsonTextReader(sw))
            {
                FromJson(reader, serializer);
            }
        }
    }

    /// <summary>
    /// Save our pool to file
    /// </sumary>
    public void SaveData()
    {
        var serializer = CreateSerializer();
        using (StreamWriter sw = new StreamWriter(pathname))
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            writer.Formatting = Formatting.Indented;
            ToJson(writer, serializer);
        }
    }
}
