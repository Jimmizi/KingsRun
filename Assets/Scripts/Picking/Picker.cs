using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Picker : MonoBehaviour
{
    Pickable pickable;
    Vector3 pickPoint;
    bool justPicked = false;

    Vector3PID pickerPID = new Vector3PID(3,3,0.2f);

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

        if (Input.GetMouseButtonUp(0))
        {
            pickable = null;
        }

        if (pickable)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10;

            Vector3 newPos = Camera.main.ScreenToWorldPoint(mousePos);

            var rigidBody = pickable.GetComponent<Rigidbody>();
            if (rigidBody)
            {
                Vector3 force = newPos - pickable.transform.TransformPoint(pickPoint);

                force = pickerPID.Update(force, Time.deltaTime);

                rigidBody.AddForceAtPosition(force*0.01f, pickPoint);
                rigidBody.AddForce(force);
            }
            else
            {
                pickable.transform.position = Vector3.Lerp(pickable.transform.position, newPos, Time.deltaTime * 2.0f);
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

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, direction, out hit, distance))
        {
            pickable = hit.collider.GetComponent<Pickable>();
            pickPoint = pickable.transform.InverseTransformPoint(hit.point);
        }
    }
}
