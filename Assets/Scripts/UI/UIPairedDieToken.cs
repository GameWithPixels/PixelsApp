using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dice;
using System.Linq;
using System.Text;
using Systemic.Unity.Pixels;

public class UIPairedDieToken : MonoBehaviour
{
    // Controls
    [Header("Controls")]
    public Button mainButton;
    public Image backgroundImage;
    public Button expandButton;
    public Image expandButtonImage;
    public GameObject expandGroup;
    public UIPairedDieView dieView;

    [Header("ExpandedControls")]
    public Button statsButton;
    public Button renameButton;
    public Button forgetButton;
    public Button resetButton;
    public Button calibrateButton;
    public Button setDesignButton;
    public Button pingButton;
    public Button disconnectButton;

    [Header("Images")]
    public Sprite backgroundCollapsedSprite;
    public Sprite backgroundExpandedSprite;
    public Sprite buttonCollapsedSprite;
    public Sprite buttonExpandedSprite;

    public bool expanded => expandGroup.activeSelf;

    public EditDie die => dieView.die;

    public void Setup(EditDie die)
    {
        dieView.Setup(die);

        // Connect to all the dice in the pool if possible
        StartCoroutine(RefreshInfo());
    }

    void Awake()
    {
        // Hook up to events
        mainButton.onClick.AddListener(OnToggle);
        expandButton.onClick.AddListener(OnToggle);
        forgetButton.onClick.AddListener(OnForget);
        renameButton.onClick.AddListener(OnRename);
        calibrateButton.onClick.AddListener(OnCalibrate);
        setDesignButton.onClick.AddListener(OnSetDesign);
        pingButton.onClick.AddListener(OnPing);
        resetButton.onClick.AddListener(OnReset);
        disconnectButton.onClick.AddListener(OnDisconnect);
    }

    void OnToggle()
    {
        bool newActive = !expanded;
        expandGroup.SetActive(newActive);
        backgroundImage.sprite = newActive ? backgroundExpandedSprite : backgroundCollapsedSprite;
        expandButtonImage.sprite = newActive ? buttonExpandedSprite : buttonCollapsedSprite;
    }

    void OnForget()
    {
        PixelsApp.Instance.ShowDialogBox(
            "Forget " + die.name + "?",
            "Are you sure you want to remove it from your dice bag?",
            "Forget",
            "Cancel",
            res =>
            {
                if (res)
                {
                    var dependentPresets = AppDataSet.Instance.CollectPresetsForDie(die);
                    if (dependentPresets.Any())
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append("The following presets depend on ");
                        builder.Append(die.name);
                        builder.AppendLine(":");
                        foreach (var b in dependentPresets)
                        {
                            builder.Append("\t");
                            builder.AppendLine(b.name);
                        }
                        builder.Append("Are you sure you want to forget it?");

                        PixelsApp.Instance.ShowDialogBox("Die In Use!", builder.ToString(), "Ok", "Cancel", res2 =>
                        {
                            if (res2)
                            {
                                OnToggle();
                                PixelsApp.Instance.ForgetDie(die);
                            }
                        });
                    }
                    else
                    {
                        OnToggle();
                        PixelsApp.Instance.ForgetDie(die);
                    }
                }
            });
    }

    void OnRename()
    {
        if (die.die?.isReady ?? false)
        {
            var newName = Names.GetRandomName();
            StartCoroutine(die.die.RenameAsync(newName, (res, _) =>
            {
                if (res && die.die != null)
                {
                    die.die.name = newName;
                    die.name = newName;
                    AppDataSet.Instance.SaveData();
                    dieView.UpdateState();
                }
            }));
        }
    }

    void OnCalibrate()
    {
        if (die.die != null)
        {
            die.die.StartCalibration();
        }
    }

    void OnSetDesign()
    {
        if (die.die?.isReady ?? false)
        {
            PixelsApp.Instance.ShowEnumPicker("Select Design", die.designAndColor, (res, newDesign) =>
            {
                StartCoroutine(SetDesignCr());
                
                IEnumerator SetDesignCr()
                {
                    var designAndColor = (PixelDesignAndColor)newDesign;
                    bool success = false;
                    yield return die.die.SetDesignAndColorAsync(designAndColor, (res, _) => success = res);

                    if (success)
                    {
                        die.designAndColor = designAndColor;
                        AppDataSet.Instance.SaveData();
                        dieView.UpdateState();
                    }
                }
            },
            null);
        }
    }

    void OnPing()
    {
        if (die.die?.isReady ?? false)
        {
            StartCoroutine(die.die.BlinkLEDsAsync(Color.yellow / 3, 3));
        }
    }

    void OnReset()
    {
        if (die.die?.isReady ?? false)
        {
            die.die.ResetParameters();
        }
    }

    void OnDisconnect()
    {
        if (die.die != null && !die.die.isAvailable)
        {
            OnToggle();
            DiceBag.DisconnectPixel(die.die, forceDisconnect: true);
        }
    }

    IEnumerator RefreshInfo()
    {
        //TODO subscribe to die connection events instead of this coroutine

        var menuEntries = new[]
        {
            statsButton,
            renameButton,
            forgetButton,
            resetButton,
            calibrateButton,
            setDesignButton,
            pingButton,
            disconnectButton
        }
        .Select(btn => btn.transform.GetComponent<PoolDieMenuEntry>())
        .Where(e => e != null)
        .ToArray();

        bool wasReady = true; // Initially set to true to force an update
        while (true)
        {
            bool ready = die.die?.isReady ?? false;

            // Update menu entries
            foreach (var e in menuEntries)
            {
                e.EnableMenuEntry(ready);
            }

            // Start battery & RSSI automatic updates
            if (ready && !wasReady)
            {
                die.die.ReportRssi(true);
            }

            // Die might be destroyed (-> null) or change state at any time
            wasReady = ready;

            yield return null;
        }
    }
}
