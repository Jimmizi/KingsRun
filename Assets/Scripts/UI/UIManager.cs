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

    // Called just after the game has been fully faded in
    public VoidCallback OnGameShown;

    // Called just after the game has been fully faded out
    public VoidCallback OnGameHidden;

    // Called just after the game has been paused
    public VoidCallback OnGamePaused;

    // Called just after the game has been resumed from a pause
    public VoidCallback OnGameResumed;

    // Public functions

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
    
    public bool IsGameVisible => currentFadeState == ScreenFadeState.GameVisible;
    public bool IsGamePaused => currentFadeState == ScreenFadeState.GamePaused;
    public bool IsGameHidden => currentFadeState == ScreenFadeState.GameHidden;
    public bool IsCurrentlyFading => currentFadeState is ScreenFadeState.ShowingGame or ScreenFadeState.HidingGame or ScreenFadeState.PausingGame or ScreenFadeState.ResumingGame;

    // Private vars

    private ScreenFadeState currentFadeState;

    [SerializeField]
    private bool startGameHidden;

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

#if UNITY_EDITOR
    private bool debugVisible = true;
#endif


    // Private functions

    private void Start()
    {
        IEnumerator DelayGameShow()
        {
            float fTime = 0.0f;
            while (fTime < startHiddenShowDelayTime)
            {
                fTime += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }

            StartCoroutine(Start_ShowGame());
        }

        if (startGameHidden)
        {
            BlackScreenGroup.alpha = 1.0f;
            currentFadeState = ScreenFadeState.GameHidden;
            StartCoroutine(DelayGameShow());
        }
        else
        {
            BlackScreenGroup.alpha = 0.0f;
        }
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
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
        {
            debugVisible = !debugVisible;
        }
#endif
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

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!debugVisible)
        {
            return;
        }

        Vector2 vPosition = new Vector2(5, 5);

        void IncrementVerticalPos()
        {
            vPosition.y += 14;
        }
        void AddText(string text)
        {
            GUI.Label(new Rect(vPosition.x, vPosition.y, 400, 24), text);
            IncrementVerticalPos();
        }

        AddText("UI Debug (F1)");
        IncrementVerticalPos();

        AddText($"Fade State: {currentFadeState.ToString()}");
    }
#endif
}
