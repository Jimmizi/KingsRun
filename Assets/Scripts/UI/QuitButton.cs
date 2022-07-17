using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.UI;
using RectTransform = UnityEngine.RectTransform;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(Button))]
public class QuitButton : MonoBehaviour
{
    public bool EnableDebugTesting = false;

    public List<TextAsset> InterruptDialogue = new List<TextAsset>();
    private int timesPressed = 0;

    public EaserEase MoveType;

    public float MoveTime = 1.0f;

    public RectTransform KeepWithinBounds;
    private Vector2 vTargetPosition = new Vector2();
    private Vector2 vStartPosition;

    private Bounds activeBounds;
    private Rect activeBoundsRect;

    private bool currentlyMoving = false;

    private Button uiButton;

    void Awake()
    {
        Service.QuitButtonObj = this;
    }

    void Start()
    {
        uiButton = GetComponent<Button>();

        activeBounds = GetRectTransformBounds(KeepWithinBounds);
        activeBoundsRect = new Rect(activeBounds.min, activeBounds.size);
        vStartPosition = (transform as RectTransform)?.anchoredPosition ?? Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetGoActive(bool b)
    {
        gameObject.SetActive(b);
    }

    public void SetButtonActive(bool b)
    {
        uiButton.interactable = b;
    }

    public void ResetPosition(float overrideSpeed = 0.0f)
    {
        MoveToStartingPosition(overrideSpeed);
    }

    public void OnButtonClicked()
    {
        if (currentlyMoving)
        {
            return;
        }

#if UNITY_EDITOR
        if (EnableDebugTesting)
        {
            MoveToFreePlace();
            return;
        }
#endif

        Debug.Assert(timesPressed < InterruptDialogue.Count);

        currentlyMoving = true;

        Service.Text.SetPausePlayback();
        StartCoroutine(OnQuitButtonPressed(true));
    }

    IEnumerator OnQuitButtonPressed(bool doPause)
    {
        bool actuallyQuit = timesPressed == InterruptDialogue.Count - 1;

        if (!actuallyQuit)
        {
            MoveToFreePlace();
        }
        else
        {
            Service.Data.TrySetData("QuitViaButton", 1);
            Service.Data.SetQuitFromFile(JsonDataExecuter.LastConversationFileLoaded);
            uiButton.interactable = false;
        }

        if(doPause)
        {
            float fTime = 0.0f;
            while (fTime < 1.0f)
            {
                fTime += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }
        }

        ConversationData data = JsonDataExecuter.MakeConversation(InterruptDialogue[timesPressed++], false);
        Service.Text.UnpausePlayback();
        Service.Text.StartOrInterruptChat(data); // Will resume processing if paused first time in here
    }

    private bool IsPointInBounds(Vector2 point, float padding = 0.0f)
    {
        if (point.x < activeBoundsRect.min.x + padding)
        {
            return false;
        }
        else if (point.x > activeBoundsRect.max.x - padding)
        {
            return false;
        }

        if (point.y < activeBoundsRect.min.y + padding)
        {
            return false;
        }
        else if (point.y > activeBoundsRect.max.y - padding)
        {
            return false;
        }

        return true;
    }

    private Vector2 GetNearestPointInBounds(Vector2 point, float size)
    {
        if (IsPointInBounds(point))
        {
            return point;
        }
        
        if (point.x < activeBoundsRect.min.x)
        {
            point.x = activeBoundsRect.min.x + (size / 2);
        }
        else if (point.x > activeBoundsRect.max.x)
        {
            point.x = activeBoundsRect.max.x - (size / 2);
        }

        if (point.y < activeBoundsRect.min.y)
        {
            point.y = activeBoundsRect.min.y + (size / 2);
        }
        else if (point.y > activeBoundsRect.max.y)
        {
            point.y = activeBoundsRect.max.y - (size / 2);
        }

        return point;
    }

    private void MoveToFreePlace()
    {
        List<Vector2> directionsToTest = new List<Vector2>()
        {
            new Vector2(1.0f, 0.0f), // Right
            new Vector2(-1.0f, 0.0f), // Left
            new Vector2(0.0f, 1.0f), // Up
            new Vector2(0.0f, -1.0f), // Down

            new Vector2(-1.0f, 1.0f), // Top left
            new Vector2(1.0f, 1.0f), // Top right
            new Vector2(-1.0f, -1.0f), // Bottom left
            new Vector2(1.0f, -1.0f), // Bottom right

        };

        RectTransform rectTransform = transform as RectTransform;

        if (!rectTransform)
        {
            return;
        }

        Bounds buttonBounds = GetRectTransformBounds(rectTransform);
        Rect screenRect = new Rect(buttonBounds.min, buttonBounds.size);

        float width = screenRect.width;
        float halfWidth = width / 2.0f;

        Vector2 vCenterInBounds = GetNearestPointInBounds(screenRect.center, width);

        List<Vector2> validCentersStageOne = new List<Vector2>();
        List<Vector2> validCentersStageTwo = new List<Vector2>();

        foreach (var dir in directionsToTest)
        {
            Vector2 vNewPointToTest = vCenterInBounds + (dir * (width * 1.2f));

            if (IsPointInBounds(vNewPointToTest, halfWidth))
            {
                validCentersStageOne.Add(vNewPointToTest);
            }
        }

        foreach (var point in validCentersStageOne)
        {
            for(int i = 0; i < 4; ++i)
            {
                Vector2 vAdjustedPoint = point + new Vector2(Random.Range(-halfWidth, halfWidth), Random.Range(-halfWidth, halfWidth));
                if (IsPointInBounds(vAdjustedPoint, halfWidth))
                {
                    validCentersStageTwo.Add(vAdjustedPoint);
                }
            }
        }

        if (validCentersStageTwo.Count > 0)
        {
            int randomPoint = Random.Range(0, validCentersStageTwo.Count);


            Vector2 vDiff = screenRect.center - validCentersStageTwo[randomPoint];

            vTargetPosition = rectTransform.anchoredPosition - vDiff;
            StartCoroutine(GoToTargetPosition());
        }
    }

    private void MoveToStartingPosition(float overrideSpeed = 0.0f)
    {
        if (!currentlyMoving)
        {
            Debug.Assert(vStartPosition != Vector2.zero);
            vTargetPosition = vStartPosition;
            StartCoroutine(GoToTargetPosition(default, overrideSpeed));
        }
    }

    IEnumerator GoToTargetPosition(bool setInteractableAfterMove = false, float overrideSpeed = 0.0f)
    {
        RectTransform rectTransform = transform as RectTransform;
        if (!rectTransform)
        {
            yield return null;
        }
        
        uiButton.interactable = false;
        currentlyMoving = true;

        float timeToMove = overrideSpeed > 0.0f ? overrideSpeed : MoveTime;

        Vector2 vStart = rectTransform.anchoredPosition;

        float fTime = 0.0f;
        while (fTime < timeToMove)
        {
            fTime += Time.deltaTime;

            float fGraphTime = fTime / timeToMove;
            rectTransform.anchoredPosition = Easer.EaseVector2(MoveType, vStart, vTargetPosition, fGraphTime);

            yield return new WaitForSeconds(Time.deltaTime);
        }

        rectTransform.anchoredPosition = vTargetPosition;
        uiButton.interactable = setInteractableAfterMove;
        currentlyMoving = false;

#if UNITY_EDITOR
        if (EnableDebugTesting)
        {
            uiButton.interactable = true;
        }
#endif
    }

    private static Vector3[] WorldCorners = new Vector3[4];
    public static Bounds GetRectTransformBounds(RectTransform transform)
    {
        transform.GetWorldCorners(WorldCorners);
        Bounds bounds = new Bounds(WorldCorners[0], Vector3.zero);
        for (int i = 1; i < 4; ++i)
        {
            bounds.Encapsulate(WorldCorners[i]);
        }
        return bounds;
    }
    
}
