using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Pickup
{
    public Pickable pickable; //
    public Vector3 pickPoint; // pick up point in local space relative to pickable.transform
    public Vector3PID holdPID = new Vector3PID(3, 3, 0.2f);
}

public class Picker : MonoBehaviour
{
    List<Pickup> heldPickups = new List<Pickup>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CastRay();
        }

        if (Input.GetMouseButton(0) && Input.GetMouseButtonDown(1))
        {
            CastRay();
        }

        if (Input.GetMouseButtonUp(0))
        {
            heldPickups.Clear();
        }

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10;

        Vector3 newPos = Camera.main.ScreenToWorldPoint(mousePos);

        foreach (Pickup pickup in heldPickups)
        {
            var rigidBody = pickup.pickable.GetComponent<Rigidbody>();
            if (rigidBody)
            {
                Vector3 force = newPos - pickup.pickable.transform.TransformPoint(pickup.pickPoint);

                force = pickup.holdPID.Update(force, Time.deltaTime);

                rigidBody.AddForceAtPosition(force*0.01f, pickup.pickPoint);
                rigidBody.AddForce(force);
            }
            else
            {
                pickup.pickable.transform.position = Vector3.Lerp(pickup.pickable.transform.position, newPos, Time.deltaTime * 2.0f);
            }
        }
    }

    void CastRay()
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
                var pickable = hit.collider.GetComponent<Pickable>();
                if (pickable != null)
                {
                    Pickup heldPickup = heldPickups.Find((pickup) => pickup.pickable == pickable);
                    if (heldPickup != null)
                    {
                        // we're already holding this pickup check the next
                        continue;
                    }

                    heldPickup = new Pickup();
                    heldPickup.pickable = pickable;
                    heldPickup.pickPoint = pickable.transform.InverseTransformPoint(hit.point);
                    heldPickups.Add(heldPickup);
                    return;
                }
            }
        }
    }
}
