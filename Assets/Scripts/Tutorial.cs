﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Tutorial : SingletonMonoBehaviour<Tutorial>
{
    [Header("Main Tutorial")]
    public RectTransform tutorialIntroRoot;
    public Button tutorialIntroNext;
    public Button tutorialIntroCancel;

    public RectTransform tutorialIntro2Root;
    public Button tutorialIntro2Next;

    public RectTransform scanningTutorialRoot;
    public Button scanningTutorialNext;

    public RectTransform poolTutorialRoot;
    public Button poolTutorialNext;

    public RectTransform pool2TutorialRoot;
    public Button pool2TutorialNext;

    public UIPage poolPage;

    [Header("Home Tutorial")]

    public RectTransform homeTutorialRoot;
    public Button homeTutorialNext;

    public RectTransform home2TutorialRoot;
    public Button home2TutorialNext;

    public UIPage homePage;

    [Header("Presets Tutorial")]
    public RectTransform presetsTutorialRoot;
    public Button presetsTutorialNext;

    public RectTransform presets2TutorialRoot;
    public Button presets2TutorialNext;

    public RectTransform presets3TutorialRoot;
    public Button presets3TutorialNext;

    public RectTransform presets4TutorialRoot;
    public Button presets4TutorialNext;

    [Header("Preset Tutorial")]
    public RectTransform presetTutorialRoot;
    public Button presetTutorialNext;

    public RectTransform preset2TutorialRoot;
    public Button preset2TutorialNext;

    [Header("Behaviors Tutorial")]
    public RectTransform behaviorsTutorialRoot;
    public Button behaviorsTutorialNext;

    public RectTransform behaviors2TutorialRoot;
    public Button behaviors2TutorialNext;

    [Header("Behavior Tutorial")]
    public RectTransform behaviorTutorialRoot;
    public Button behaviorTutorialNext;

    [Header("Rule Tutorial")]
    public RectTransform ruleTutorialRoot;
    public Button ruleTutorialNext;

    public RectTransform rule2TutorialRoot;
    public Button rule2TutorialNext;

    [Header("Animations Tutorial")]
    public RectTransform animationsTutorialRoot;
    public Button animationsTutorialNext;

    [Header("Animation Tutorial")]
    public RectTransform animationTutorialRoot;
    public Button animationTutorialNext;

    public RectTransform animation2TutorialRoot;
    public Button animation2TutorialNext;

    public void StartMainTutorial()
    {
        tutorialIntroRoot.gameObject.SetActive(true);
        tutorialIntroCancel.onClick.RemoveAllListeners();
        tutorialIntroCancel.onClick.AddListener(() =>
        {
            // Disable all tutorials
            AppSettings.Instance.DisableAllTutorials();
            tutorialIntroRoot.gameObject.SetActive(false);
        });
        tutorialIntroNext.onClick.RemoveAllListeners();
        tutorialIntroNext.onClick.AddListener(() =>
        {
            // Next step
            AppSettings.Instance.SetMainTutorialEnabled(false);
            tutorialIntroRoot.gameObject.SetActive(false);

            tutorialIntro2Root.gameObject.SetActive(true);
            tutorialIntro2Next.onClick.RemoveAllListeners();
            tutorialIntro2Next.onClick.AddListener(() =>
            {
                tutorialIntro2Root.gameObject.SetActive(false);
                NavigationManager.Instance.GoToRoot(UIPage.PageId.DicePool);
                NavigationManager.Instance.GoToPage(UIPage.PageId.DicePoolScanning, null);

                IEnumerator waitAndDisplayScanningTutorial()
                {
                    yield return new WaitForSeconds(0.25f);
                    scanningTutorialRoot.gameObject.SetActive(true);
                    scanningTutorialNext.onClick.RemoveAllListeners();
                    scanningTutorialNext.onClick.AddListener(() =>
                    {
                        scanningTutorialRoot.gameObject.SetActive(false);

                        // Now we wait until the user connects their dice
                        void checkCanGoToPage(UIPage page, object context, System.Action goToPage)
                        {
                            if (page != poolPage || (DiceManager.Instance.allDice.Count() == 0 && DiceManager.Instance.state != DiceManager.State.AddingDiscoveredDie))
                            {
                                PixelsApp.Instance.ShowDialogBox("Are you sure?", "You have not paired any die, are you sure you want to leave the tutorial?", "Yes", "Cancel", res =>
                                {
                                    if (res)
                                    {
                                        NavigationManager.Instance.checkCanGoToPage = null;
                                        NavigationManager.Instance.onPageEntered -= onPageChanged;
                                        goToPage.Invoke();
                                    }
                                });
                            }
                            else
                            {
                                goToPage?.Invoke();
                            }
                        }

                        void onPageChanged(UIPage newPage, object context)
                        {
                            IEnumerator waitUntilIdleAgainAndContinue()
                            {
                                yield return new WaitUntil(() => DiceManager.Instance.state == DiceManager.State.Idle);

                                // Check that we DO in fact have dice in the list
                                if (DiceManager.Instance.allDice.Count() > 0)
                                {
                                    // Automatically assign dice!
                                    var basicPreset = AppDataSet.Instance.presets.FirstOrDefault(p => p.name == "All dice basic");
                                    if (basicPreset == null)
                                    {
                                        basicPreset = new Presets.EditPreset
                                        {
                                            name = "All dice basic",
                                            description = "Sets all dice to the basic profile.",
                                        };
                                        AppDataSet.Instance.presets.Insert(0, basicPreset);
                                    }
                                    basicPreset.dieAssignments = DiceManager.Instance.allDice.Select(
                                        d => new Presets.EditDieAssignment
                                        {
                                            die = d,
                                            behavior = AppDataSet.Instance.behaviors?.FirstOrDefault(),
                                        }).ToList();

                                    poolTutorialRoot.gameObject.SetActive(true);
                                    poolTutorialNext.onClick.RemoveAllListeners();
                                    poolTutorialNext.onClick.AddListener(() =>
                                    {
                                        poolTutorialRoot.gameObject.SetActive(false);
                                        pool2TutorialRoot.gameObject.SetActive(true);
                                        pool2TutorialNext.onClick.RemoveAllListeners();
                                        pool2TutorialNext.onClick.AddListener(() =>
                                        {
                                            pool2TutorialRoot.gameObject.SetActive(false);
                                            AppSettings.Instance.SetMainTutorialEnabled(false);

                                            // Now we wait until the user connects their dice
                                            void checkCanGoToPage2(UIPage page2, object context2, System.Action goToPage2)
                                            {
                                                if (page2 == homePage)
                                                {
                                                    goToPage2?.Invoke();
                                                }
                                            }

                                            void onPageChanged2(UIPage newPage2, object context2)
                                            {
                                                NavigationManager.Instance.onPageEntered -= onPageChanged2;
                                                NavigationManager.Instance.checkCanGoToPage = null;
                                            }

                                            NavigationManager.Instance.onPageEntered += onPageChanged2;
                                            NavigationManager.Instance.checkCanGoToPage = checkCanGoToPage2;
                                        });
                                    });
                                }
                            }
                            NavigationManager.Instance.onPageEntered -= onPageChanged;
                            NavigationManager.Instance.checkCanGoToPage = null;
                            StartCoroutine(waitUntilIdleAgainAndContinue());
                        }

                        NavigationManager.Instance.onPageEntered += onPageChanged;
                        NavigationManager.Instance.checkCanGoToPage = checkCanGoToPage;
                    });
                }

                StartCoroutine(waitAndDisplayScanningTutorial());
            });
        });
    }

    public void StartHomeTutorial()
    {
        IEnumerator waitAndDisplayHomeTutorial()
        {
            NavigationManager.Instance.GoToRoot(UIPage.PageId.Home);
            yield return new WaitForSeconds(0.25f);
            homeTutorialRoot.gameObject.SetActive(true);
            homeTutorialNext.onClick.RemoveAllListeners();
            homeTutorialNext.onClick.AddListener(() =>
            {
                homeTutorialRoot.gameObject.SetActive(false);
                home2TutorialRoot.gameObject.SetActive(true);
                home2TutorialNext.onClick.RemoveAllListeners();
                home2TutorialNext.onClick.AddListener(() =>
                {
                    home2TutorialRoot.gameObject.SetActive(false);
                    AppSettings.Instance.SetHomeTutorialEnabled(false);
                });
            });
        }
        StartCoroutine(waitAndDisplayHomeTutorial());
    }

    public void StartPresetsTutorial()
    {
        presetsTutorialRoot.gameObject.SetActive(true);
        presetsTutorialNext.onClick.RemoveAllListeners();
        presetsTutorialNext.onClick.AddListener(() =>
        {
            presetsTutorialRoot.gameObject.SetActive(false);
            presets2TutorialRoot.gameObject.SetActive(true);
            presets2TutorialNext.onClick.RemoveAllListeners();
            presets2TutorialNext.onClick.AddListener(() =>
            {
                presets2TutorialRoot.gameObject.SetActive(false);
                presets3TutorialRoot.gameObject.SetActive(true);
                presets3TutorialNext.onClick.RemoveAllListeners();
                presets3TutorialNext.onClick.AddListener(() =>
                {
                    presets3TutorialRoot.gameObject.SetActive(false);
                    presets4TutorialRoot.gameObject.SetActive(true);
                    presets4TutorialNext.onClick.RemoveAllListeners();
                    presets4TutorialNext.onClick.AddListener(() =>
                    {
                        presets4TutorialRoot.gameObject.SetActive(false);
                        AppSettings.Instance.SetPresetsTutorialEnabled(false);
                    });
                });
            });
        });
    }

    public void StartPresetTutorial()
    {
        presetTutorialRoot.gameObject.SetActive(true);
        presetTutorialNext.onClick.RemoveAllListeners();
        presetTutorialNext.onClick.AddListener(() =>
        {
            presetTutorialRoot.gameObject.SetActive(false);
            preset2TutorialRoot.gameObject.SetActive(true);
            preset2TutorialNext.onClick.RemoveAllListeners();
            preset2TutorialNext.onClick.AddListener(() =>
            {
                preset2TutorialRoot.gameObject.SetActive(false);
                AppSettings.Instance.SetPresetTutorialEnabled(false);
            });
        });
    }

    public void StartBehaviorsTutorial()
    {
        behaviorsTutorialRoot.gameObject.SetActive(true);
        behaviorsTutorialNext.onClick.RemoveAllListeners();
        behaviorsTutorialNext.onClick.AddListener(() =>
        {
            behaviorsTutorialRoot.gameObject.SetActive(false);
            behaviors2TutorialRoot.gameObject.SetActive(true);
            behaviors2TutorialNext.onClick.RemoveAllListeners();
            behaviors2TutorialNext.onClick.AddListener(() =>
            {
                behaviors2TutorialRoot.gameObject.SetActive(false);
                AppSettings.Instance.SetBehaviorsTutorialEnabled(false);
            });
        });
    }

    public void StartBehaviorTutorial()
    {
        behaviorTutorialRoot.gameObject.SetActive(true);
        behaviorTutorialNext.onClick.RemoveAllListeners();
        behaviorTutorialNext.onClick.AddListener(() =>
        {
            behaviorTutorialRoot.gameObject.SetActive(false);
            AppSettings.Instance.SetBehaviorTutorialEnabled(false);
        });
    }

    public void StartRuleTutorial()
    {
        ruleTutorialRoot.gameObject.SetActive(true);
        ruleTutorialNext.onClick.RemoveAllListeners();
        ruleTutorialNext.onClick.AddListener(() =>
        {
            ruleTutorialRoot.gameObject.SetActive(false);
            rule2TutorialRoot.gameObject.SetActive(true);
            rule2TutorialNext.onClick.RemoveAllListeners();
            rule2TutorialNext.onClick.AddListener(() =>
            {
                rule2TutorialRoot.gameObject.SetActive(false);
                AppSettings.Instance.SetRuleTutorialEnabled(false);
            });
        });
    }

    public void StartAnimationsTutorial()
    {
        animationsTutorialRoot.gameObject.SetActive(true);
        animationsTutorialNext.onClick.RemoveAllListeners();
        animationsTutorialNext.onClick.AddListener(() =>
        {
            animationsTutorialRoot.gameObject.SetActive(false);
            AppSettings.Instance.SetAnimationsTutorialEnabled(false);
        });
    }

    public void StartAnimationTutorial()
    {
        animationTutorialRoot.gameObject.SetActive(true);
        animationTutorialNext.onClick.RemoveAllListeners();
        animationTutorialNext.onClick.AddListener(() =>
        {
            animationTutorialRoot.gameObject.SetActive(false);
            animation2TutorialRoot.gameObject.SetActive(true);
            animation2TutorialNext.onClick.RemoveAllListeners();
            animation2TutorialNext.onClick.AddListener(() =>
            {
                animation2TutorialRoot.gameObject.SetActive(false);
                AppSettings.Instance.SetAnimationTutorialEnabled(false);
            });
        });
    }

}
