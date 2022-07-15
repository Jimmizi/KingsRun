using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneIdleCamera : MonoBehaviour
{
    private const float WANDER_EXTENT_X = 0.5f;
    private const float WANDER_EXTENT_Y = 0.1f;

    public static bool CanWander;

    private Vector3 mOriginalPosition;

    private Vector3 mNextTarget;
    private float mMoveTime;

    private Vector3 velocity = Vector3.zero;
    private float mMoveTimer;

    // Start is called before the first frame update
    void Start()
    {
        mOriginalPosition = new Vector3(0,0,-10);
        mNextTarget = mOriginalPosition;
    }

    void FindNextTarget()
    {
        mMoveTimer = 0.0f;
        mNextTarget = mOriginalPosition + new Vector3(Random.Range(-WANDER_EXTENT_X, WANDER_EXTENT_X), Random.Range(-WANDER_EXTENT_Y, WANDER_EXTENT_Y), 0);
        mMoveTime = Random.Range(5f, 15);
    }

    // Update is called once per frame
    void Update()
    {
        if (!CanWander)
        {
            return;
        }

        if(Vector3.Distance(transform.position, mNextTarget) > 0.05f
           && mMoveTimer < mMoveTime * 1.5f)
        { 
            transform.position = Vector3.SmoothDamp(transform.position, mNextTarget, ref velocity, mMoveTime);
        
        }
        else
        {
            FindNextTarget();
        }

        mMoveTimer += Time.deltaTime;
    }
}
