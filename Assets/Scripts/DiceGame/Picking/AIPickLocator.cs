using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIPickLocator : MonoBehaviour
{
    public Vector3 initialLocation { get; private set; }

    [SerializeField]
    Vector3 moveRange = new Vector3(10, 10, 10);

    Vector3 targetLocation;

    [SerializeField]
    float movementChangeTime = 0.5f;

    float movementChangeTimer = 0;


    // Start is called before the first frame update
    void Start()
    {
        initialLocation = transform.position;
        movementChangeTimer = movementChangeTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (movementChangeTimer > 0)
        {
            movementChangeTimer -= Time.deltaTime;
            if (movementChangeTimer < 0)
            {
                movementChangeTimer = movementChangeTime;
                targetLocation.x = initialLocation.x + Random.Range(-moveRange.x, moveRange.x);
                targetLocation.y = initialLocation.y + Random.Range(-moveRange.y, moveRange.y);
                targetLocation.z = initialLocation.z + Random.Range(-moveRange.z, moveRange.z);
            }
        }

        transform.position = Vector3.Lerp(transform.position, targetLocation, Time.deltaTime);
    }
}
