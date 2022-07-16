using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(Button))]
public class QuitButton : MonoBehaviour
{
    public EaserEase MoveType;

    public float MoveTime = 1.0f;

    public RectTransform KeepWithinBounds;
    private Vector2 vTargetPosition = new Vector2();

    private Bounds activeBounds;
    private Rect activeBoundsRect;

    private Button uiButton;

    // Start is called before the first frame update
    void Start()
    {
        uiButton = GetComponent<Button>();

        activeBounds = GetRectTransformBounds(KeepWithinBounds);
        activeBoundsRect = new Rect(activeBounds.min, activeBounds.size);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void OnButtonClicked()
    {
        MoveToFreePlace();
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
        if (!uiButton.interactable)
        {
            return;
        }

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
            //rectTransform.anchoredPosition = vTargetPosition;
        }

        //float fFurthestDist = 0.0f;
        //Vector2 vFurthest = screenRect.center;

        //Vector2 vCursorPos = Input.mousePosition;

        //foreach (var point in validCentersStageTwo)
        //{
        //    float fDist = Vector2.Distance(point, vCursorPos);
        //    if (fDist > fFurthestDist)
        //    {
        //        fFurthestDist = fDist;
        //        vFurthest = point;
        //    }
        //}

        //Vector2 vDiff = screenRect.center - vFurthest;

        //vTargetPosition = rectTransform.anchoredPosition - vDiff;
        //rectTransform.anchoredPosition = vTargetPosition;
    }

    IEnumerator GoToTargetPosition()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (!rectTransform)
        {
            yield return null;
        }
        
        uiButton.interactable = false;

        Vector2 vStart = rectTransform.anchoredPosition;

        float fTime = 0.0f;
        while (fTime < MoveTime)
        {
            fTime += Time.deltaTime;

            float fGraphTime = fTime / MoveTime;
            rectTransform.anchoredPosition = Easer.EaseVector2(MoveType, vStart, vTargetPosition, fGraphTime);

            yield return new WaitForSeconds(Time.deltaTime);
        }

        rectTransform.anchoredPosition = vTargetPosition;
        uiButton.interactable = true;
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
