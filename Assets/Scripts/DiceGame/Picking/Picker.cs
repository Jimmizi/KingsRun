using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Pickup
{
    public GameObject gameObject;
    public Vector3 pickPoint; // pick up point in local space relative to pickable.transform
    public Vector3PID holdPID = new Vector3PID(30, 20, 3);
}


public class Picker : MonoBehaviour
{
    [SerializeField]
    float holdDistance = 10;

    [SerializeField]
    public bool pickUpEnabled = true;

    public List<GameObject> validPickups = new List<GameObject>();
    List<Pickup> heldPickups = new List<Pickup>();
    
    public delegate void OnObjectsThrowHandler(GameObject[] thrownObjects);
    public event OnObjectsThrowHandler OnObjectsThrown;

    // Update is called once per frame
    void Update()
    {
        if (!pickUpEnabled)
        {
            if (heldPickups.Count > 0)
            {
                ThrowObjects();
            }
            return;
        }

        if (validPickups == null || validPickups.Count == 0)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            PickObject();
        }

        if (Input.GetMouseButton(0) && Input.GetMouseButtonDown(1))
        {
            PickObject();
        }

        if (Input.GetMouseButtonUp(0))
        {
            ThrowObjects();            
        }
    }

    private void FixedUpdate()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = holdDistance;

        Vector3 newPos = Camera.main.ScreenToWorldPoint(mousePos);

        foreach (Pickup pickup in heldPickups)
        {
            var rigidBody = pickup.gameObject.GetComponent<Rigidbody>();
            if (rigidBody)
            {
                Vector3 force = newPos - pickup.gameObject.transform.TransformPoint(pickup.pickPoint);

                force = pickup.holdPID.Update(force, Time.fixedDeltaTime);

                rigidBody.AddForceAtPosition(force * 0.01f, pickup.pickPoint);
                rigidBody.AddForce(force);
            }
            else
            {
                pickup.gameObject.transform.position = Vector3.Lerp(pickup.gameObject.transform.position, newPos, Time.deltaTime * 2.0f);
            }
        }
    }

    void PickObject()
    {
        Vector3 mousePos = Input.mousePosition;

        Vector3 rayOrigin = Camera.main.ScreenToWorldPoint(mousePos);
        Vector3 rayEnd = Camera.main.ScreenToWorldPoint(mousePos + Vector3.forward * 100);

        Vector3 direction = rayEnd - rayOrigin;
        float distance = direction.magnitude;
        direction /= distance;

        Debug.DrawLine(rayOrigin, rayEnd, Color.red, 10);

        LayerMask mask = LayerMask.GetMask("Dice");

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, direction, distance, mask);
        foreach( RaycastHit hit in hits)
        {
            if( hit.collider != null )
            {
                GameObject colliderObject = hit.collider.gameObject;
                if (validPickups.Contains(colliderObject))
                {
                    Pickup heldPickup = heldPickups.Find((pickup) => pickup.gameObject == colliderObject);
                    if (heldPickup != null)
                    {
                        // we're already holding this pickup check the next
                        continue;
                    }

                    heldPickup = new Pickup();
                    heldPickup.gameObject = colliderObject;
                    heldPickup.pickPoint = colliderObject.transform.InverseTransformPoint(hit.point);
                    heldPickups.Add(heldPickup);
                    return;
                }
            }
        }
    }

    void ThrowObjects()
    {
        if (heldPickups.Count > 0)
        {
            GameObject[] pickedObjects = new GameObject[heldPickups.Count];
            for (int i = 0; i < pickedObjects.Length; i++)
            {
                pickedObjects[i] = heldPickups[i].gameObject;
            }

            heldPickups.Clear();

            if (OnObjectsThrown!=null)
            {
                OnObjectsThrown(pickedObjects);
            }
        }
    }
}
