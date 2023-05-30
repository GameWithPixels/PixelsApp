using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Systemic.Unity.Pixels.Animations;
using System.IO;
using SimpleFileBrowser;
using Systemic.Unity.Pixels;

public class UIPatternEditor : MonoBehaviour
{
    [Header("Controls")]
    public Button backButton;
    public InputField titleText;
    public Button saveButton;
    public RawImage diePreviewImage;
    public RawImage patternPreview;
    public Button loadFromFile;
    public Button reloadFromFile;

    Texture2D _texture;

    EditPattern currentPattern;
    System.Action<bool, EditPattern> closeAction;
    string currentFilepath;

    public SingleDiceRenderer dieRenderer { get; private set; }

    public bool isShown => gameObject.activeSelf;

    public bool isDirty => saveButton.gameObject.activeSelf;

    /// <summary>
    /// Invoke the color picker
    /// </sumary>
    public void Show(string title, EditPattern pattern, System.Action<bool, EditPattern> closeAction)
    {
        if (isShown)
        {
            Debug.LogWarning("Previous RGB Pattern picker still active");
            ForceHide();
        }

        dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(PixelDesignAndColor.V5Black);
        if (dieRenderer != null)
        {
            diePreviewImage.texture = dieRenderer.renderTexture;
        }

        gameObject.SetActive(true);
        currentPattern = pattern;
        titleText.text = title;

        RepaintPreview();

        this.closeAction = closeAction;
        saveButton.gameObject.SetActive(false);
        reloadFromFile.interactable = false;
    }

    /// <summary>
    /// If for some reason the app needs to close the dialog box, this will do it!
    /// Normally it closes itself when you tap ok or cancel
    /// </sumary>
    public void ForceHide()
    {
        Hide(false, currentPattern);
    }

    void Awake()
    {
        backButton.onClick.AddListener(DiscardAndBack);
        saveButton.onClick.AddListener(SaveAndBack);
        loadFromFile.onClick.AddListener(LoadFromFile);
        reloadFromFile.onClick.AddListener(UpdateFromCurrentFile);
        titleText.onEndEdit.AddListener(newName => currentPattern.name = newName);
		_texture = new Texture2D(512, 20, TextureFormat.ARGB32, false);
        _texture.filterMode = FilterMode.Point;
        _texture.wrapMode = TextureWrapMode.Clamp;
    }

	void OnDestroy()
	{
		patternPreview.texture = null;
		Object.Destroy(_texture);
		_texture = null;

        if (DiceRendererManager.Instance != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(dieRenderer);
            dieRenderer = null;
        }
    }

    void Hide(bool result, EditPattern pattern)
    {
        if (dieRenderer != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(dieRenderer);
            dieRenderer = null;
        }
        gameObject.SetActive(false);
        closeAction?.Invoke(result, pattern);
        closeAction = null;
    }

    void SaveAndBack()
    {
        Hide(true, currentPattern);
    }

    void DiscardAndBack()
    {
        if (isDirty)
        {
            PixelsApp.Instance.ShowDialogBox(
                "Discard Changes",
                "You have unsaved changes, are you sure you want to discard them?",
                "Discard",
                "Cancel", discard =>
                {
                    if (discard)
                    {
                        Hide(false, currentPattern);
                    }
                });
        }
        else
        {
            Hide(false, currentPattern);
        }
    }

    void FileSelected(string filePath)
    {
        Debug.Log("Selected image pattern file: " + filePath);
        if (!string.IsNullOrEmpty(filePath))
        {
            currentFilepath = filePath;
            UpdateFromCurrentFile();
        }
    }

    void UpdateFromCurrentFile()
    {
        byte[] fileData = File.ReadAllBytes(currentFilepath);
        var tex = new Texture2D(2, 2);
        if (!tex.LoadImage(fileData)) //..this will auto-resize the texture dimensions.
        {
            PixelsApp.Instance.ShowDialogBox("Error loading image", "Sorry the image you selected can't be loaded. Check that it is a valid image", "Ok", null, null);
        }
        else if (tex.height > 20 || tex.width > 1000)
        {
            PixelsApp.Instance.ShowDialogBox("Image too big", "Sorry the image you selected is too large. It should be smaller than 1000x20 pixels", "Ok", null, null);
        }
        else
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            currentPattern.FromTexture(tex);
            currentPattern.name = Path.GetFileNameWithoutExtension(currentFilepath);
            titleText.text = currentPattern.name;
            saveButton.gameObject.SetActive(true);
            reloadFromFile.interactable = false;
            RepaintPreview();
        }
        GameObject.Destroy(tex);
    }

    void LoadFromFile()
    {
#if UNITY_EDITOR
        FileSelected(UnityEditor.EditorUtility.OpenFilePanel("Select Image For Design", "", "png"));
#elif UNITY_STANDALONE_WIN
        // Set filters (optional)
		// It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
		// if all the dialogs will be using the same filters
		FileBrowser.SetFilters( true, new FileBrowser.Filter( "Images", ".png" ));

		// Set default filter that is selected when the dialog is shown (optional)
		// Returns true if the default filter is set successfully
		// In this case, set Images filter as the default filter
		FileBrowser.SetDefaultFilter( ".png" );
        FileBrowser.ShowLoadDialog((paths) => FileSelected(paths[0]), null, FileBrowser.PickMode.Files, false, null, null, "Select png", "Select");
#else
        NativeGallery.GetImageFromGallery(FileSelected, "Select Image For Design");
        // NativeFilePicker.PickFile( FileSelected, new string[] { NativeFilePicker.ConvertExtensionToFileType( "png" ) });
#endif
    }

    void RepaintPreview()
    {
		Object.Destroy(_texture);

        var anim = new EditAnimationKeyframed
        {
            name = "temp anim",
            pattern = currentPattern,
            duration = currentPattern.duration
        };

        dieRenderer.SetAuto(true);
        dieRenderer.SetAnimation(anim);
        dieRenderer.Play(true);

        _texture = currentPattern.ToTexture();
        patternPreview.texture = _texture;        
    }
}
