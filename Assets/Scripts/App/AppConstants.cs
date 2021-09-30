using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppConstants : SingletonMonoBehaviour<AppConstants>
{
    public System.Version AppVersion = new System.Version(1, 3);
    public TextAsset defaultDiceJson;
    public string DataSetFilename = "dice_data.json";
    public string SettingsFilename = "settings.json";
    public float ScanTimeout = 5.0f;
    public float ConnectionTimeout = 10.0f;
    public float DiceRotationSpeedAvg = 10.0f;
    public float DiceRotationSpeedVar = 1.0f;
    public Color DieUnavailableColor = Color.gray;
    public float MultiDiceRootRotationSpeedAvg = 2.0f;
    public float MultiDiceRootRotationSpeedVar = 0.4f;
    public string AudioClipsFolderName = "AudioClips";
    public Color AudioClipsWaveformColor = Color.blue;
}

