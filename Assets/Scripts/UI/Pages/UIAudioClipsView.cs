using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AudioClips;
using SimpleFileBrowser;
using System.Linq;
using System.Text;

public class UIAudioClipsView
    : UIPage
{
    [Header("Controls")]
    public Transform contentRoot;
    public Button addAudioClipButton;
    public RectTransform spacer;

    [Header("Prefabs")]
    public UIAudioClipsViewToken audioClipTokenPrefab;

    readonly List<UIAudioClipsViewToken> audioClips = new List<UIAudioClipsViewToken>();

    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        addAudioClipButton.onClick.AddListener(AddNewClip);
    }

    void OnEnable()
    {
        base.SetupHeader(true, false, "Audio Clips", null);
        RefreshView();
    }

    UIAudioClipsViewToken CreateClipToken(AudioClipManager.AudioClipInfo clip)
    {
        // Create the gameObject
        var ret = GameObject.Instantiate<UIAudioClipsViewToken>(audioClipTokenPrefab, Vector3.zero, Quaternion.identity, contentRoot.transform);
        spacer.SetAsLastSibling();

        // When we click on the pattern main button, go to the edit page
        ret.onPlay.AddListener(() => PlayClip(clip));
        ret.onRemove.AddListener(() => DeleteClip(clip));
        ret.onExpand.AddListener(() => ExpandClip(clip));
        ret.onClick.AddListener(() => ExpandClip(clip));

        ret.removeButton.gameObject.SetActive(!clip.builtIn);
        ret.builtInInfo.gameObject.SetActive(clip.builtIn);

        addAudioClipButton.transform.SetAsLastSibling();
        // Initialize it
        ret.Setup(clip);
        return ret;
    }

    void DestroyClipToken(UIAudioClipsViewToken clipToken)
    {
        GameObject.Destroy(clipToken.gameObject);
    }

    void RefreshView()
    {
        // Assume all pool dice will be destroyed
        List<UIAudioClipsViewToken> toDestroy = new List<UIAudioClipsViewToken>(audioClips);
        foreach (var clip in AudioClipManager.Instance.audioClips)
        {
            int prevIndex = toDestroy.FindIndex(a => a.clip == clip);
            if (prevIndex == -1)
            {
                // New clip
                var newClipUI = CreateClipToken(clip);
                audioClips.Add(newClipUI);
            }
            else
            {
                toDestroy.RemoveAt(prevIndex);
            }
        }

        // Remove all remaining clips
        foreach (var clip in toDestroy)
        {
            audioClips.Remove(clip);
            DestroyClipToken(clip);
        }
    }

    IEnumerator FileSelectedCr(string filePath)
    {
        Debug.Log("Audio file path: " + filePath);

        // Copy the file to the user directory
        string fileName = null;
        yield return AudioClipManager.Instance.AddUserClip(filePath, n => fileName = n);
        if (!string.IsNullOrEmpty(fileName))
        {
            AppDataSet.Instance.AddAudioClip(fileName);
            AppDataSet.Instance.SaveData();
            RefreshView();
        }
    }

    void FileSelected(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(filePath);
            if (AudioClipManager.Instance.audioClips.Any(ac => ac.clip.name == name))
            {
                PixelsApp.Instance.ShowDialogBox("Duplicated Audio Clip!", "There is already an Audio Clip with the same name.", "Ok");
            }
            else
            {
                StartCoroutine(FileSelectedCr(filePath));
            }
        }
    }

    void AddNewClip()
    {
#if UNITY_EDITOR
        var supportedFormats = string.Join(",", AudioClipManager.supportedExtensions.Select(ex => ex.TrimStart('.')));
        FileSelected(UnityEditor.EditorUtility.OpenFilePanelWithFilters("Select audio file", "", new[] { "Sound files", supportedFormats, "All files", "*" }));
#elif UNITY_STANDALONE_WIN
        // Set filters (optional)
		// It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
		// if all the dialogs will be using the same filters
		FileBrowser.SetFilters( true, new FileBrowser.Filter( "Audio files", ".wav" ));

		// Set default filter that is selected when the dialog is shown (optional)
		// Returns true if the default filter is set successfully
		// In this case, set Images filter as the default filter
		FileBrowser.SetDefaultFilter( ".wav" );
        FileBrowser.ShowLoadDialog((paths) => FileSelected(paths[0]), null, FileBrowser.PickMode.Files, false, null, null, "Select audio file", "Select");
#else
        var supportedFormats = AudioClipManager.supportedExtensions.Select(ex => NativeFilePicker.ConvertExtensionToFileType(ex.TrimStart('.')));
        NativeFilePicker.PickFile(FileSelected, supportedFormats.ToArray());
#endif
    }

    void DeleteClip(AudioClipManager.AudioClipInfo clip)
    {
        PixelsApp.Instance.ShowDialogBox("Delete Audio Clip?", "Are you sure you want to delete " + clip.clip.name + "?", "Ok", "Cancel", res =>
        {
            if (res)
            {
                var audioClip = AppDataSet.Instance.FindAudioClip(clip.clip.name);
                if (audioClip != null)
                {
                    void RemoveClip()
                    {
                        AudioClipManager.Instance.RemoveUserClip(clip.clip);
                        AppDataSet.Instance.DeleteAudioClip(audioClip);
                        AppDataSet.Instance.SaveData();
                        RefreshView();
                    }

                    var dependentPresets = AppDataSet.Instance.CollectPresetsForAudioClip(audioClip);
                    if (dependentPresets.Any())
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append("The following profiles depend on");
                        builder.Append(audioClip.name);
                        builder.AppendLine(":");
                        foreach (var b in dependentPresets)
                        {
                            builder.Append("\t");
                            builder.AppendLine(b.name);
                        }
                        builder.Append("Are you sure you want to delete it?");

                        PixelsApp.Instance.ShowDialogBox("Audio Clip In Use!", builder.ToString(), "Ok", "Cancel", res2 =>
                        {
                            if (res2)
                            {
                                RemoveClip();
                            }
                        });
                    }
                    else
                    {
                        RemoveClip();
                    }
                }
                else
                {
                    Debug.LogError("Cannot find audio clip " + clip.clip.name);
                }
            }
        });
    }

    void ExpandClip(AudioClipManager.AudioClipInfo clip)
    {
        foreach (var uip in audioClips)
        {
            if (uip.clip == clip)
            {
                uip.Expand(!uip.isExpanded);
            }
            else
            {
                uip.Expand(false);
            }
        }
    }

    void PlayClip(AudioClipManager.AudioClipInfo clip)
    {
        audioSource.PlayOneShot(clip.clip);
    }
}
