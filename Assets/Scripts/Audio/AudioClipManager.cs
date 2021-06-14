using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Linq;

public class AudioClipManager : SingletonMonoBehaviour<AudioClipManager>
{
    [Header("Built-in Audio files")]
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

    class AudioFileImportInfo
    {
        public string fileName;
        public string filePath;
        public AudioType type;
    }

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
            var audioFileInfos = new List<AudioFileImportInfo>();
            DirectoryInfo info = new DirectoryInfo(userClipsRootPath);
            foreach (FileInfo item in info.GetFiles())
            {
                bool isWav = Path.GetExtension(item.Name) == wavExtension;
                bool isMp3 = Path.GetExtension(item.Name) == mp3Extension;
                bool isOgg = Path.GetExtension(item.Name) == oggExtension;
                if (isWav || isMp3 || isOgg)
                {
                    var type = isWav ? AudioType.WAV : isOgg ? AudioType.OGGVORBIS : AudioType.MPEG;
                    audioFileInfos.Add(new AudioFileImportInfo()
                    {
                        fileName = item.Name,
                        filePath = Path.Combine(userClipsRootPath, item.Name),
                        type = type
                    });
                }
            }

            foreach (var audioFileInfo in audioFileInfos)
            {
                string streamingPath = audioFileInfo.filePath;
#if UNITY_IOS
                streamingPath = "file://" + audioFileInfo.filePath;
#endif
                UnityWebRequest AudioFileRequest = UnityWebRequestMultimedia.GetAudioClip(streamingPath, audioFileInfo.type);
                yield return AudioFileRequest.SendWebRequest();
                if (AudioFileRequest.result != UnityWebRequest.Result.ConnectionError)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(AudioFileRequest);
                    clip.name = Path.GetFileNameWithoutExtension(audioFileInfo.fileName);
                    userClips.Add(clip);
                    Debug.Log("Imported user audio clip: " + audioFileInfo.filePath);
                }
            }
        }

        // Generate previews for all clips
        for (int i = 0; i < builtInClips.Count; ++i)
        {
            var clip = builtInClips[i];
            var preview = AudioUtils.PaintWaveformSpectrum(clip, 0.7f, 256, 256, AppConstants.Instance.AudioClipsWaveformColor);
            var hash = AudioUtils.GenerateWaveformHash(clip);
            audioClips.Add(new AudioClipInfo() {
                builtIn = true,
                clip = clip,
                preview = preview });

            // Wait until next frame to continue
            yield return null;
        }

        for (int i = 0; i < userClips.Count; ++i)
        {
            var clip = userClips[i];
            var preview = AudioUtils.PaintWaveformSpectrum(clip, 0.7f, 256, 256, AppConstants.Instance.AudioClipsWaveformColor);
            var hash = AudioUtils.GenerateWaveformHash(clip);
            audioClips.Add(new AudioClipInfo() {
                builtIn = false,
                clip = clip,
                preview = preview });

            // Wait until next frame to continue
            yield return null;
        }
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

    public IEnumerator AddUserClip(string path, System.Action<string> fileLoadedCallback)
    {
        if (!Directory.Exists(userClipsRootPath))
        {
            Directory.CreateDirectory(userClipsRootPath);
        }

        string clipName = Path.GetFileName(path);
        string destPath = Path.Combine(userClipsRootPath, clipName);
        File.Copy(path, destPath, true);
        bool isWav = Path.GetExtension(path) == wavExtension;
        bool isMp3 = Path.GetExtension(path) == mp3Extension;
        bool isOgg = Path.GetExtension(path) == oggExtension;
        if (isWav || isMp3 || isOgg)
        {
            Debug.Log("File path to import: " + destPath);
            string streamingPath = destPath;
#if UNITY_IOS
            streamingPath = "file://" + destPath;
#endif
            var type = isWav ? AudioType.WAV : isOgg ? AudioType.OGGVORBIS : AudioType.MPEG;
            UnityWebRequest AudioFileRequest = UnityWebRequestMultimedia.GetAudioClip(streamingPath, type);
            yield return AudioFileRequest.SendWebRequest();
            if (AudioFileRequest.result != UnityWebRequest.Result.ConnectionError)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(AudioFileRequest);
                clip.name = Path.GetFileNameWithoutExtension(clipName);
                userClips.Add(clip);
                Debug.Log("Imported user audio clip: " + destPath);

                var preview = AudioUtils.PaintWaveformSpectrum(clip, 0.7f, 256, 256, AppConstants.Instance.AudioClipsWaveformColor);
                var hash = AudioUtils.GenerateWaveformHash(clip);
                audioClips.Add(new AudioClipInfo()
                {
                    builtIn = false,
                    clip = clip,
                    preview = preview
                });

                fileLoadedCallback?.Invoke(clipName);
            }
            else
            {
                fileLoadedCallback?.Invoke(null);
            }
        }
        else
        {
            fileLoadedCallback?.Invoke(null);
        }
    }
}
