using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Types

    public enum ScreenFadeState
    {
        TitleStart,
        TitleShowing,
        TitleFading,

        GameVisible,
        GameHidden,
        GamePaused,
        
        ShowingGame,
        HidingGame,
        PausingGame,
        ResumingGame,
    }

    public delegate void VoidCallback();

    // Public vars

    public VoidCallback OnStartGame;

    // Called just after the game has been fully faded in
    public VoidCallback OnGameShown;

    // Called just after the game has been fully faded out
    public VoidCallback OnGameHidden;

    // Called just after the game has been paused
    public VoidCallback OnGamePaused;

    // Called just after the game has been resumed from a pause
    public VoidCallback OnGameResumed;

    // Public functions

    public void GoBackToMenuFromGame()
    {
        currentFadeState = ScreenFadeState.TitleStart;
        isRunningAfterQuitGame = true;
    }

    public void ResetMenu()
    {
        currentFadeState = ScreenFadeState.TitleStart;
    }

    public void TogglePause()
    {
        if (currentFadeState == ScreenFadeState.GameVisible)
        {
            PauseGame();
        }
        else if (currentFadeState == ScreenFadeState.GamePaused)
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        Debug.Assert(currentFadeState == ScreenFadeState.GameVisible);
        if (currentFadeState == ScreenFadeState.GameVisible)
        {
            StartCoroutine(Start_PauseGame());
        }
    }
    public void ResumeGame()
    {
        Debug.Assert(currentFadeState == ScreenFadeState.GamePaused);
        if (currentFadeState == ScreenFadeState.GamePaused)
        {
            StartCoroutine(Start_ResumeGame());
        }
    }

    public void ShowGame()
    {
        Debug.Assert(currentFadeState == ScreenFadeState.GameHidden);
        if (currentFadeState == ScreenFadeState.GameHidden)
        {
            StartCoroutine(Start_ShowGame());
        }
    }
    public void HideGame()
    {
        Debug.Assert(currentFadeState == ScreenFadeState.GameVisible);
        if (currentFadeState == ScreenFadeState.GameVisible)
        {
            StartCoroutine(Start_HideGame());
        }
    }

    public void ButtonClickToStartGame()
    {
        if (currentFadeState == ScreenFadeState.TitleShowing)
        {
            StartCoroutine(HideTitleScreen());
        }
    }

    public void ProcessGameStartFade(bool instant = false)
    {
        Debug.Assert(!hasShownOnce);

        IEnumerator DelayGameShow()
        {
            float fTime = instant ? startHiddenShowDelayTime : 0.0f;
            while (fTime < startHiddenShowDelayTime)
            {
                fTime += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }

            StartCoroutine(Start_ShowGame());
        }

        // Hide the quit button - dialogue scripts will reveal this later on in the story
        Service.QuitButtonObj.SetGoActive(false);

        if (!instant)
        {
            BlackScreenGroup.alpha = 1.0f;
            currentFadeState = ScreenFadeState.GameHidden;
            StartCoroutine(DelayGameShow());
        }
        else
        {
            TitleScreenGroup.alpha = 0.0f;
            BlackScreenGroup.alpha = 0.0f;
            ThunderFlashGroup.alpha = 0.0f;
            CreditsGo.SetActive(false);

            currentFadeState = ScreenFadeState.GameVisible;
            
            hasShownOnce = true;
            OnStartGame?.Invoke();
        }
    }
    
    public bool IsGameVisible => currentFadeState == ScreenFadeState.GameVisible;
    public bool IsGamePaused => currentFadeState == ScreenFadeState.GamePaused;
    public bool IsGameHidden => currentFadeState == ScreenFadeState.GameHidden;
    public bool IsCurrentlyFading => currentFadeState is ScreenFadeState.ShowingGame or ScreenFadeState.HidingGame or ScreenFadeState.PausingGame or ScreenFadeState.ResumingGame;

    public ScreenFadeState FadeState => currentFadeState;

    // Private vars

    private ScreenFadeState currentFadeState;

    [SerializeField]
    public bool instantStartGame;

    [SerializeField] [Tooltip("Time to delay at the start of the game before fading in (if startGameHidden set)")]
    private float startHiddenShowDelayTime = 1.0f;

    [SerializeField]
    private float fadeTime = 1.0f;

    [SerializeField]
    private float pauseFadeTime = 0.5f;

    [SerializeField]
    private int fadeIterations = 5;

    [SerializeField]
    private CanvasGroup BlackScreenGroup;

    [SerializeField]
    private CanvasGroup TitleScreenGroup;

    [SerializeField]
    private CanvasGroup ThunderFlashGroup;

    [SerializeField]
    private float ThunderFlashTime = 0.5f;

    [SerializeField]
    private Button StartGameButton;

    [SerializeField]
    private GameObject CreditsGo;

    [SerializeField]
    private GameObject TitleGo;
    [SerializeField]
    private GameObject PressToPlayGo;

    [SerializeField]
    private GameObject ReturnedToTitleGo;
    [SerializeField]
    private GameObject SecondTimeLaunchingGo;
    [SerializeField]
    private GameObject LastTimeLaunchingGo;

    [SerializeField]
    private EaserEase ThunderFlashGraph;

    private bool hasShownOnce = false;
    private bool isRunningAfterQuitGame = false;

    // Private functions

    private void Start()
    {
        Debug.Assert(Service.Flow.JsonScriptToLoadDataMembers != null);
        EventData loadMemberEvent = JsonDataExecuter.MakeEvent(Service.Flow.JsonScriptToLoadDataMembers);
        Debug.Assert(loadMemberEvent != null);
        JsonDataExecuter.ProcessEvent(loadMemberEvent, false);

        int numTimesPlayed = GetNumTimesPlayed();
        int? quitViaButton = Service.Data.TryGetData("QuitViaButton");

        // If we've entered the game but we're restarting after having not pressed the button to quit, reset the times played
        if (numTimesPlayed > 0 && (!quitViaButton.HasValue || quitViaButton.Value == 0))
        {
            Debug.Log("Resetting NumTimesPlayed due to not having quit before via the button.");
            Service.Data.TrySetData("NumTimesPlayed", 0);
        }
    }

    int GetNumTimesPlayed()
    {
        int? timePlayed = Service.Data.TryGetData("NumTimesPlayed");
        return timePlayed.HasValue ? timePlayed.Value : 0;
    }

    private void Awake()
    {
        Debug.Assert(BlackScreenGroup);

        if (Service.UI == null)
        {
            DontDestroyOnLoad(gameObject);
            Service.UI = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        switch (currentFadeState)
        {
            case ScreenFadeState.TitleStart:
                StateTitleStart();
                break;
            case ScreenFadeState.TitleShowing:
                break;
            case ScreenFadeState.TitleFading:
                break;
            case ScreenFadeState.GameVisible:
                break;
            case ScreenFadeState.GameHidden:
                break;
            case ScreenFadeState.GamePaused:
                break;
            case ScreenFadeState.ShowingGame:
                break;
            case ScreenFadeState.HidingGame:
                break;
            case ScreenFadeState.PausingGame:
                break;
            case ScreenFadeState.ResumingGame:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void StateTitleStart()
    {
        StartCoroutine(BringInTitleScreen());
        currentFadeState = ScreenFadeState.TitleShowing;
    }

    private IEnumerator BringInTitleScreen()
    {
        // TODO Bring in rain/music sound

        Service.Puzzle.PuzzleDone = false;

        TitleScreenGroup.alpha = 0.0f;
        BlackScreenGroup.alpha = 1.0f;
        ThunderFlashGroup.alpha = 0;

        TitleGo.SetActive(false);
        CreditsGo.SetActive(false);
        PressToPlayGo.SetActive(false);
        ReturnedToTitleGo.SetActive(false);
        SecondTimeLaunchingGo.SetActive(false);
        LastTimeLaunchingGo.SetActive(false);
        
        if (!isRunningAfterQuitGame)
        {
            int iNumTimesPlayed = GetNumTimesPlayed();

            // We've quit once and come back in
            if (iNumTimesPlayed == 1)
            {
                SecondTimeLaunchingGo.SetActive(true);
                Service.QuitButtonObj.SetGoActive(false);
            }
            // We've quit again and are now blocked from entering
            else if(iNumTimesPlayed > 1)
            {
                LastTimeLaunchingGo.SetActive(true);
            }
            else
            {
                // First time
                CreditsGo.SetActive(true);
                PressToPlayGo.SetActive(true);
                TitleGo.SetActive(true);

                Service.QuitButtonObj.SetGoActive(true);
            }
        }
        else
        {
            TitleGo.SetActive(true);
            ReturnedToTitleGo.SetActive(true);
        }
        
        float fTime = 0.0f;
        yield return new WaitForSeconds(1.0f);

        // TODO Play sfx
        ThunderFlashGroup.alpha = 1.0f;
        TitleScreenGroup.alpha = 1.0f;

        StartGameButton.interactable = true;
        if (StartGameButton.GetComponentInChildren<Animator>() != null)
        {
            StartGameButton.GetComponentInChildren<Animator>().enabled = true;
        }

        while (fTime < ThunderFlashTime)
        {
            fTime += Time.deltaTime;

            float fGraphTime = fTime / ThunderFlashTime;
            ThunderFlashGroup.alpha = Easer.Ease(ThunderFlashGraph, 0.0f, 1.0f, fGraphTime);

            yield return new WaitForSeconds(Time.deltaTime);
        }

        ThunderFlashGroup.alpha = 0.0f;
    }

    private IEnumerator HideTitleScreen()
    {
        currentFadeState = ScreenFadeState.TitleFading;
        StartGameButton.interactable = false;
        TitleScreenGroup.alpha = 1.0f;

        if (StartGameButton.GetComponentInChildren<Animator>() != null)
        {
            StartGameButton.GetComponentInChildren<Animator>().enabled = false;
        }

        float fTime = 0.0f;
        float fTimeSegment = fadeTime / (float)fadeIterations;
        float fNextTimeTarget = fTimeSegment;
        float fFadeSegment = 1.0f / (float)fadeIterations;

        while (fTime < fadeTime && TitleScreenGroup.alpha > 0.0f)
        {
            fTime += Time.deltaTime;

            if (fTime >= fNextTimeTarget)
            {
                fNextTimeTarget += fTimeSegment;
                TitleScreenGroup.alpha -= fFadeSegment;
            }

            yield return new WaitForSeconds(Time.deltaTime);
        }

        TitleScreenGroup.alpha = 0.0f;
        CreditsGo.SetActive(false);

        ProcessGameStartFade();
    }

    private IEnumerator Start_ShowGame()
    {
        currentFadeState = ScreenFadeState.ShowingGame;
        BlackScreenGroup.alpha = 1.0f;

        float fTime = 0.0f;
        float fTimeSegment = fadeTime / (float) fadeIterations;
        float fNextTimeTarget = fTimeSegment;
        float fFadeSegment = 1.0f / (float)fadeIterations;

        while (fTime < fadeTime && BlackScreenGroup.alpha > 0.0f)
        {
            fTime += Time.deltaTime;

            if (fTime >= fNextTimeTarget)
            {
                fNextTimeTarget += fTimeSegment;
                BlackScreenGroup.alpha -= fFadeSegment;
            }
            
            yield return new WaitForSeconds(Time.deltaTime);
        }

        currentFadeState = ScreenFadeState.GameVisible;
        BlackScreenGroup.alpha = 0.0f;

        if (!hasShownOnce)
        {
            hasShownOnce = true;
            OnStartGame?.Invoke();
        }

        OnGameShown?.Invoke();
    }

    private IEnumerator Start_HideGame()
    {
        currentFadeState = ScreenFadeState.HidingGame;
        BlackScreenGroup.alpha = 0.0f;

        float fTime = 0.0f;
        float fTimeSegment = fadeTime / (float)fadeIterations;
        float fNextTimeTarget = fTimeSegment;
        float fFadeSegment = 1.0f / (float)fadeIterations;

        while (fTime < fadeTime && BlackScreenGroup.alpha < 1.0f)
        {
            fTime += Time.deltaTime;

            if (fTime >= fNextTimeTarget)
            {
                fNextTimeTarget += fTimeSegment;
                BlackScreenGroup.alpha += fFadeSegment;
            }

            yield return new WaitForSeconds(Time.deltaTime);
        }

        currentFadeState = ScreenFadeState.GameHidden;
        BlackScreenGroup.alpha = 1.0f;

        OnGameHidden?.Invoke();
    }

    private IEnumerator Start_PauseGame()
    {
        currentFadeState = ScreenFadeState.PausingGame;
        BlackScreenGroup.alpha = 0.0f;

        float fTime = 0.0f;
        float fTimeSegment = pauseFadeTime / (float)fadeIterations;
        float fNextTimeTarget = fTimeSegment;

        // Only fade half as much
        float fFadeSegment = 0.5f / (float)fadeIterations;

        while (fTime < pauseFadeTime)
        {
            fTime += Time.deltaTime;

            if (fTime >= fNextTimeTarget)
            {
                fNextTimeTarget += fTimeSegment;
                BlackScreenGroup.alpha += fFadeSegment;
            }

            yield return new WaitForSeconds(Time.deltaTime);
        }

        currentFadeState = ScreenFadeState.GamePaused;
        BlackScreenGroup.alpha = 0.5f;

        OnGamePaused?.Invoke();
    }

    private IEnumerator Start_ResumeGame()
    {
        currentFadeState = ScreenFadeState.ResumingGame;
        BlackScreenGroup.alpha = 0.5f;

        float fTime = 0.0f;
        float fTimeSegment = pauseFadeTime / (float)fadeIterations;
        float fNextTimeTarget = fTimeSegment;

        // Only fade half as much
        float fFadeSegment = 0.5f / (float)fadeIterations;

        while (fTime < pauseFadeTime)
        {
            fTime += Time.deltaTime;

            if (fTime >= fNextTimeTarget)
            {
                fNextTimeTarget += fTimeSegment;
                BlackScreenGroup.alpha -= fFadeSegment;
            }

            yield return new WaitForSeconds(Time.deltaTime);
        }

        currentFadeState = ScreenFadeState.GameVisible;
        BlackScreenGroup.alpha = 0.0f;

        OnGameResumed?.Invoke();
    }
    
}
