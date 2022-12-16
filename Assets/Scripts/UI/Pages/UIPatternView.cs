using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dice;
using Systemic.Unity.Pixels;
using Systemic.Unity.Pixels.Animations;

public class UIPatternView
    : UIPage
{
    [Header("Controls")]
    public RawImage previewImage;
    public RotationSlider rotationSlider;
    public UIParameterEnum animationSelector;
    public RectTransform parametersRoot;
    public Button playOnDieButton;
    public UIRotationControl rotationControl;

    public EditAnimation editAnimation { get; private set; }
    public SingleDiceRenderer dieRenderer { get; private set; }
    
    UIParameterManager.ObjectParameterList parameters;
    EditDie previewDie = null;
    bool previewDieConnected = false;

    public override void Enter(object context)
    {
        base.Enter(context);
        var anim = context as EditAnimation;
        if (anim != null)
        {
            Setup(anim);
        }

        if (AppSettings.Instance.animationTutorialEnabled)
        {
            Tutorial.Instance.StartAnimationTutorial();
        }
    }

    void OnDisable()
    {
        if (DiceRendererManager.Instance != null && this.dieRenderer != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(this.dieRenderer);
            this.dieRenderer = null;
        }

        foreach (var parameter in parameters.parameters)
        {
            GameObject.Destroy(parameter.gameObject);
        }
        parameters = null;

        if (previewDie?.die != null)
        {
            // Note: the die will revert to standard mode on disconnection
            DiceBag.DisconnectPixel(previewDie.die);
            previewDie = null;
            previewDieConnected = false;
        }
    }

    void Setup(EditAnimation anim)
    {
        SetupHeader(false, false, anim.name, SetName);
        editAnimation = anim;
        dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(anim.defaultPreviewSettings.design, 600);
        if (dieRenderer != null)
        {
            previewImage.texture = dieRenderer.renderTexture;
        }

        rotationSlider.Setup(this.dieRenderer);
        rotationControl.Setup(this.dieRenderer);

        animationSelector.Setup(
            "Lighting Pattern Type",
            () => editAnimation.type,
            (t) => SetAnimationType((AnimationType)t),
            null);

        // Setup all other parameters
        parameters = UIParameterManager.Instance.CreateControls(anim, parametersRoot);
        parameters.onParameterChanged += OnAnimParameterChanged;

        dieRenderer.SetAuto(true);
        dieRenderer.SetAnimation(anim);
        dieRenderer.Play(true);
    }

    void Awake()
    {
        playOnDieButton.onClick.AddListener(() => PreviewOnDie());
    }

    void OnAnimParameterChanged(EditObject animObject, UIParameter parameter, object newValue)
    {
        var theEditAnim = (EditAnimation)animObject;
        Debug.Assert(theEditAnim == editAnimation);
        dieRenderer.SetAnimation(theEditAnim);
        pageDirty = true;
    }

    void SetAnimationType(AnimationType newType)
    {
        if (newType != editAnimation.type)
        {
            // Change the type, which really means create a new animation and replace the old one
            var newEditAnimation = EditAnimation.Create(newType);

            // Copy over the few things we can
            newEditAnimation.duration = editAnimation.duration;
            newEditAnimation.name = editAnimation.name;
            newEditAnimation.defaultPreviewSettings = editAnimation.defaultPreviewSettings;

            // Replace the animation
            AppDataSet.Instance.ReplaceAnimation(editAnimation, newEditAnimation);

            // Setup the parameters again
            foreach (var parameter in parameters.parameters)
            {
                GameObject.Destroy(parameter.gameObject);
            }

            parameters = UIParameterManager.Instance.CreateControls(newEditAnimation, parametersRoot);
            parameters.onParameterChanged += OnAnimParameterChanged;

            dieRenderer.SetAuto(true);
            dieRenderer.SetAnimation(newEditAnimation);

            editAnimation = newEditAnimation;
            pageDirty = true;
        }
    }

    Coroutine PreviewOnDie()
    {
        return StartCoroutine(PreviewOnDieCr());
    }

    IEnumerator PreviewOnDieCr()
    {
        if (previewDie == null)
        {
            bool? previewDieSelected = null;
            PixelsApp.Instance.ShowDiePicker(
                "Select Die for Preview",
                null,
                (ed) =>  true,
                (res, newDie) =>
                {
                    previewDie = newDie;
                    previewDieSelected = res;
                });
            yield return new WaitUntil(() => previewDieSelected.HasValue);
        }

        if (previewDie != null)
        {
            if (previewDie.die == null)
            {
                previewDieConnected = false;
            }

            if (!previewDieConnected)
            {
                string error = null;
                yield return PixelsApp.Instance.ConnectDie(previewDie, gameObject,
                    onConnected: _ => previewDieConnected = true,
                    onFailed: (_, err) => error = err);

                if (!previewDieConnected)
                {
                    previewDie = null;
                    bool acknowledged = false;
                    PixelsApp.Instance.ShowDialogBox("Could not connect.", error, "Ok", null, _ => acknowledged = true);
                    yield return new WaitUntil(() => acknowledged);
                }
            }

            if (previewDie != null && previewDieConnected)
            {
                previewDie.die.SetLEDAnimatorMode();
                yield return new WaitForSeconds(0.5f); //TODO add acknowledge from die instead

                PixelsApp.Instance.ShowProgrammingBox("Uploading animation to " + previewDie.name);

                string error = null;
                try
                {
                    bool success = false;
                    var editSet = AppDataSet.Instance.ExtractEditSetForAnimation(editAnimation);
                    yield return previewDie.die.PlayTestAnimationAsync(
                        editSet.ToDataSet(),
                        (res, err) => (success, error) = (res, err),
                        (_, progress) => PixelsApp.Instance.UpdateProgrammingBox(progress));
                }
                finally
                {
                    PixelsApp.Instance.HideProgrammingBox();
                }

                if (error != null)
                {
                    bool acknowledged = false;
                    PixelsApp.Instance.ShowDialogBox($"Transfer Error", $"Could not play animation on {previewDie.name}: transfer error", "Ok", null, _ => acknowledged = true);
                    previewDie = null;
                    yield return new WaitUntil(() => acknowledged);
                }
            }
        }
    }

    void SetName(string newName)
    {
        editAnimation.name = newName;
        base.pageDirty = true;
    }
}
