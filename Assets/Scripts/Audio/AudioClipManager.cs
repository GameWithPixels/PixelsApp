using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Linq;

public class AudioClipManager : SingletonMonoBehaviour<AudioClipManager>
{
    // The list of clips provided by the app
    public List<AudioClip> builtInClips;

    // The list of clips scanned from the app persistent storage
    public List<AudioClip> userClips = new List<AudioClip>();

    public class AudioClipInfo
    {
        public bool builtIn;
        public AudioClip clip;
        public Texture2D preview;
    }

    public List<AudioClipInfo> audioClips = new List<AudioClipInfo>();

    const string wavExtension = ".wav";
    const string mp3Extension = ".mp3";
    //const string m4aExtension = ".m4a";
    const string oggExtension = ".ogg";

    public static string[] supportedExtensions => new[] { wavExtension, mp3Extension, oggExtension };

    AudioSource audioSource;

    public AudioClipInfo FindClip(string name)
    {
        return audioClips.FirstOrDefault(a => string.Compare(a.clip.name, name, true) == 0);
    }

    string userClipsRootPath => Path.Combine(Application.persistentDataPath, AppConstants.Instance.AudioClipsFolderName);

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (!Directory.Exists(userClipsRootPath))
        {
            Directory.CreateDirectory(userClipsRootPath);
        }

        StartCoroutine(LoadUserFiles());
    }

    IEnumerator LoadUserFiles()
    {
        if (Directory.Exists(userClipsRootPath))
        {
            var dirInfo = new DirectoryInfo(userClipsRootPath);
            foreach (FileInfo item in dirInfo.GetFiles())
            {
                yield return LoadClip(Path.Combine(userClipsRootPath, item.Name));
            }
        }

        // Generate previews for all clips
        foreach (var clip in builtInClips)
        {
            CreatePreview(clip, builtIn: true);
            // Wait until next frame to continue
            yield return null;
        }

        foreach (var clip in userClips)
        {
            CreatePreview(clip);
            // Wait until next frame to continue
            yield return null;
        }
    }

    IEnumerator LoadClip(string filePath, System.Action<AudioClip> doneCb = null)
    {
        AudioClip clip = null;

        bool isWav = Path.GetExtension(filePath) == wavExtension;
        bool isMp3 = Path.GetExtension(filePath) == mp3Extension;
        bool isOgg = Path.GetExtension(filePath) == oggExtension;
        if (isWav || isMp3 || isOgg)
        {
#if !UNITY_IOS
            string streamingPath = filePath;
#else
            string streamingPath = "file://" + filePath;
#endif
            var audioType = isWav ? AudioType.WAV : isOgg ? AudioType.OGGVORBIS : AudioType.MPEG;
            UnityWebRequest AudioFileRequest = UnityWebRequestMultimedia.GetAudioClip(streamingPath, audioType);
            yield return AudioFileRequest.SendWebRequest();

            if (AudioFileRequest.result != UnityWebRequest.Result.ConnectionError)
            {
                clip = DownloadHandlerAudioClip.GetContent(AudioFileRequest);
                clip.name = Path.GetFileNameWithoutExtension(filePath);
                userClips.Add(clip);
                Debug.Log("Imported user audio clip: " + streamingPath);
            }
            else
            {
                Debug.LogError("Failed to load audio file: " + filePath);
            }
        }
        else
        {
            Debug.LogError("Unsupported audio format: " + filePath);
        }

        doneCb?.Invoke(clip);
    }

    void CreatePreview(AudioClip clip, bool builtIn = false)
    {
        var preview = AudioUtils.PaintWaveformSpectrum(clip, 0.7f, 256, 256, AppConstants.Instance.AudioClipsWaveformColor);
        //var hash = AudioUtils.GenerateWaveformHash(clip);
        audioClips.Add(new AudioClipInfo()
        {
            builtIn = builtIn,
            clip = clip,
            preview = preview
        });
    }

    public void PlayAudioClip(uint clipId)
    {
        var editClip = AppDataSet.Instance.FindAudioClip(clipId);
        if (editClip != null)
        {
            var clipInfo = FindClip(editClip.name);
            if (clipInfo != null)
            {
                audioSource.PlayOneShot(clipInfo.clip);
            }
            else
            {
                Debug.LogError("No matching clip info for Audio Clip id " + editClip.name);
            }
        }
        else
        {
            Debug.LogError("Unknown Audio Clip id " + clipId);
        }
    }

    // The action gets the user friendly filename
    public IEnumerator AddUserClip(string path, System.Action<string> fileLoadedCallback)
    {
        if (!Directory.Exists(userClipsRootPath))
        {
            Directory.CreateDirectory(userClipsRootPath);
        }

        string destPath = Path.Combine(userClipsRootPath, Path.GetFileName(path));
        File.Copy(path, destPath, true);

        Debug.Log("Copied audio file to: " + destPath);

        yield return LoadClip(destPath, clip =>
        {
            if (clip != null)
            {
                CreatePreview(clip);
                fileLoadedCallback?.Invoke(clip.name);
            }
            else
            {
                Debug.Log("Deleting audio file: " + destPath);
                File.Delete(destPath);
                fileLoadedCallback?.Invoke(null);
            }
        });
    }

    public void RemoveUserClip(AudioClip clip)
    {
        if (!userClips.Contains(clip))
        {
            return;
        }

        if (Directory.Exists(userClipsRootPath))
        {
            string pathname = Directory.EnumerateFiles(userClipsRootPath, clip.name + ".*").FirstOrDefault();
            if (!string.IsNullOrEmpty(pathname))
            {
                Debug.Log("Deleting audio file: " + pathname);
                try
                {
                    File.Delete(pathname);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to delete audio file");
                    Debug.LogException(e);
                }
            }
        }

        userClips.Remove(clip);
        audioClips.RemoveAll(c => c.clip == clip);
    }
}
