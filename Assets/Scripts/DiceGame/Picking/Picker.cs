using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup
{
    public GameObject gameObject;
    public Vector3 pickPoint; // pick up point in local space relative to pickable.transform
    public Vector3PID holdPID = new Vector3PID(30, 20, 3);
    public Rigidbody rigidBody = null;
}


public class Picker : MonoBehaviour
{
    [SerializeField]
    float holdDistance = 10;

    [SerializeField]
    public bool pickUpEnabled = true;

    [SerializeField]
    public bool pickAll = false;

    public List<GameObject> validPickups = new List<GameObject>();
    public List<Pickup> heldPickups = new List<Pickup>();
    
    public delegate void OnObjectsThrowHandler(GameObject[] thrownObjects);
    public event OnObjectsThrowHandler OnObjectsThrown;

    public BoxCollider DiceHoldBoundsBC;
    private Bounds diceHoldBounds;

    private Vector3 lastDiceBoundsHitPos = new Vector3();

    void Start()
    {
        if (DiceHoldBoundsBC != null)
        {
            diceHoldBounds = DiceHoldBoundsBC.bounds;
            lastDiceBoundsHitPos = diceHoldBounds.center;
        }
        else
        {
            Debug.LogError("No Dice Hold Bounds specified.");
        }
    }

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
            if (pickAll)
            {
                PickAllObjects();
            }
            else
            {
                PickObject();
            }
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
        Vector3 rayOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 rayEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 100);
        Vector3 direction = rayEnd - rayOrigin;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, direction, out hit, Mathf.Infinity, LayerMask.GetMask("DiceBounds")))
        {
            lastDiceBoundsHitPos = hit.point;
        }

        foreach (Pickup pickup in heldPickups)
        {
            if (pickup.rigidBody)
            {
                Vector3 force = lastDiceBoundsHitPos - pickup.gameObject.transform.TransformPoint(pickup.pickPoint);

                force = pickup.holdPID.Update(force, Time.fixedDeltaTime);

                pickup.rigidBody.AddForceAtPosition(force * 0.01f, pickup.pickPoint);
                pickup.rigidBody.AddForce(force);
            }
            else
            {
                pickup.gameObject.transform.position = Vector3.Lerp(pickup.gameObject.transform.position, lastDiceBoundsHitPos, Time.deltaTime * 2.0f);
            }
        }
    }

    void PickAllObjects()
    {
        heldPickups.Clear();

        foreach (GameObject pickable in validPickups)
        {
            if (pickable.activeInHierarchy)
            {
                var heldPickup = new Pickup();
                heldPickup.gameObject = pickable;
                heldPickup.rigidBody = pickable.GetComponent<Rigidbody>();
                heldPickup.pickPoint = Random.insideUnitCircle * 0.1f;
                heldPickups.Add(heldPickup);
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
                    heldPickup.rigidBody = colliderObject.GetComponent<Rigidbody>();
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

                // Anti-cheese, if the player's not adding force, we will
                if (heldPickups[i].rigidBody.velocity.sqrMagnitude < 50)
                {
                    heldPickups[i].rigidBody.AddExplosionForce(100, Camera.main.transform.position, 100, 2, ForceMode.Impulse);
                    heldPickups[i].rigidBody.AddRelativeTorque(Random.insideUnitSphere * 10, ForceMode.Impulse);
                }
            }

            heldPickups.Clear();

            if (OnObjectsThrown!=null)
            {
                OnObjectsThrown(pickedObjects);
            }
        }
    }
}
