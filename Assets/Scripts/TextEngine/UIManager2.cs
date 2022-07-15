using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class UIManager2 : MonoBehaviour
{
    private enum Positioning
    {
        FadeIn,
        StartToIdle,
        Idling,
        IdleToEnd,
        Fade,
        BackToOrigin,
        Done
    }

    public float TitleScreenStartYPos;
    public float TitleScreenIdleYPos;
    public float TitleScreenEndYPos;

    public float StartToIdleTime = 3.0f;
    
    public CanvasGroup FaderForBackToOrigin;
    public List<GameObject> CameraFaderObjects = new List<GameObject>();

    [HideInInspector]
	public bool InGame = false;

    private Positioning mCurrentPosition = Positioning.FadeIn;

    private bool mTitleIdlingFlipDir = false;

    //private ShowPanels showPanels;										//Reference to ShowPanels script on UI GameObject, to show and hide panels
    private CanvasGroup[] menuCanvasGroup;

    private float mTimerBeforeMenuLaunch;

    public void SetCameraFaderAlpha(bool active)
    {
        foreach (var fader in CameraFaderObjects)
        {
            fader.SetActive(active);
        }
    }

    void Awake()
	{
        //Service.Provide(this);

        //Get a reference to ShowPanels attached to UI object
        //showPanels = GetComponent<ShowPanels> ();

		//Get all canvas grounds in my childen, we want to fade them all out at the same time
        menuCanvasGroup = GetComponentsInChildren<CanvasGroup>();

        SetCameraFaderAlpha(false);

        var pos = Camera.main.transform.position;
        pos.y = TitleScreenStartYPos;
        Camera.main.transform.position = pos;

        FaderForBackToOrigin.alpha = 1.0f;
    }

    void Start()
    {
        //if (Service.Test().SkipTitle)
        {
            Camera.main.transform.position = new Vector3(0, 0, -10);
            SetCameraFaderAlpha(true);
            StartCoroutine(FadeOutScreenFader(1f, 0f));
            //Service.Audio().PlayGameLoopMusic();
            InGame = true;
            SceneIdleCamera.CanWander = true;
            return;
        }

        SceneManager.LoadScene("TitleScreen", LoadSceneMode.Additive);
        //Service.Audio().PlayMenuMusic();
    }

    void Update()
    {

        if (mTimerBeforeMenuLaunch < 1.25f)
        {
            mTimerBeforeMenuLaunch += Time.deltaTime;
            return;
        }


        if (InGame)
        {
            return;
        }

        if (mCurrentPosition < Positioning.IdleToEnd && mCurrentPosition != Positioning.FadeIn)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ExitMenuOverlay();
               // Service.Audio().StopMenuMusic();
            }
        }

        //Hacky af but whatever
        switch (mCurrentPosition)
        {
            case Positioning.FadeIn:
            {
                StartCoroutine(FadeOutScreenFader(1f, 0f, true, 4.0f));
                mCurrentPosition = Positioning.StartToIdle;
                break;
            }
            case Positioning.StartToIdle:
            {
                var pos = Camera.main.transform.position;
                Camera.main.transform.position = new Vector3(pos.x, Mathf.Lerp(pos.y, TitleScreenIdleYPos, Time.deltaTime / StartToIdleTime), pos.z);

                if (Camera.main.transform.position.y <= TitleScreenIdleYPos + 0.5f)
                {
                    mCurrentPosition = Positioning.Idling;
                }

                break;
            }
                
            case Positioning.Idling:
            {
                if (mTitleIdlingFlipDir) //up
                {
                    var pos = Camera.main.transform.position;
                    Camera.main.transform.position = new Vector3(pos.x, Mathf.Lerp(pos.y, TitleScreenIdleYPos + 0.2f, Time.deltaTime / (StartToIdleTime * 2)), pos.z);

                    if (Camera.main.transform.position.y >= TitleScreenIdleYPos + 0.1f)
                    {
                        mTitleIdlingFlipDir = false;
                    }
                }
                else
                {
                    var pos = Camera.main.transform.position;
                    Camera.main.transform.position = new Vector3(pos.x, Mathf.Lerp(pos.y, TitleScreenIdleYPos - 0.2f, Time.deltaTime / (StartToIdleTime * 2)), pos.z);

                    if (Camera.main.transform.position.y <= TitleScreenIdleYPos - 0.1f)
                    {
                        mTitleIdlingFlipDir = true;
                    }
                }
                break;
            }
            case Positioning.IdleToEnd:
            {
                var pos = Camera.main.transform.position;
                Camera.main.transform.position = new Vector3(pos.x, Mathf.Lerp(pos.y, TitleScreenEndYPos, Time.deltaTime), pos.z);

                if (Camera.main.transform.position.y <= TitleScreenEndYPos + 0.5f)
                {
                    mCurrentPosition = Positioning.Fade;
                }
                
                break;
            }
            case Positioning.Fade:
            {
                FaderForBackToOrigin.alpha = Mathf.Lerp(FaderForBackToOrigin.alpha, 1.0f, Time.deltaTime * 2);

                if (FaderForBackToOrigin.alpha >= 0.6f)
                {
                    FaderForBackToOrigin.alpha = 1.0f;
                    mCurrentPosition = Positioning.BackToOrigin;
                    SceneManager.UnloadSceneAsync("TitleScreen");
                }
                break;
            }
            case Positioning.BackToOrigin:
            {
                Camera.main.transform.position = new Vector3(0, 0, -10);
                SetCameraFaderAlpha(true);
                InGame = true;
                StartCoroutine(FadeOutScreenFader(1f, 0f));
                mCurrentPosition = Positioning.Done;

                //Service.Audio().PlayGameLoopMusic();

                SceneIdleCamera.CanWander = true;
                
                break;
            }
        }
    }

	public void ExitMenuOverlay()
    {
        mCurrentPosition = Positioning.IdleToEnd;
        Debug.Log("Pressed Start");
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += SceneWasLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= SceneWasLoaded;
    }

    //Once the level has loaded, check if we want to call PlayLevelMusic
    void SceneWasLoaded(Scene scene, LoadSceneMode mode)
    {
		
	}

    public bool IsScreenFadedIn => Math.Abs(FaderForBackToOrigin.alpha) < 0.01f;
    public bool IsScreenFadeTransitioning => mScreenIsFading;

    private bool mScreenIsFading = false;

    public void FadeScreenOut()
    {
        StartCoroutine(FadeOutScreenFader(0f, 1f, false));
    }

    public void FadeScreenIn()
    {
        StartCoroutine(FadeOutScreenFader(1f, 0f, false));
    }

    public IEnumerator FadeOutScreenFader(float startAlpha, float endAlpha, bool fadeCharactersIn = true, float fadeTime = 1.0f)
    {
        mScreenIsFading = true;

        float elapsedTime = 0f;
        float totalDuration = fadeTime;
        bool fadedInCharacters = false;

        while (elapsedTime < totalDuration)
        {
            if (InGame && !fadedInCharacters && fadeCharactersIn)
            {
                if(elapsedTime >= totalDuration / 2)
                {
                    fadedInCharacters = true;
                   // Service.Party().FadeInParty();
                }
            }

            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / totalDuration);

            FaderForBackToOrigin.alpha = currentAlpha;

            yield return null;
        }

        mScreenIsFading = false;
    }

}
