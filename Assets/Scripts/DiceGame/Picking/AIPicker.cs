using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIPicker : MonoBehaviour
{
    public delegate void OnObjectsThrowHandler(GameObject[] thrownObjects);
    public event OnObjectsThrowHandler OnObjectsThrown;

    public List<Pickup> heldPickups = new List<Pickup>();

    [SerializeField]
    AIPickLocator locator;

    public void Pickup(GameObject gameObject)
    {
        var pickup = new Pickup();
        pickup.gameObject = gameObject;
        pickup.pickPoint = gameObject.transform.position;

        heldPickups.Add(pickup);
    }

    public void Throw()
    {
        if (heldPickups.Count > 0)
        {
            Vector3 averageLocation = Vector3.zero;
            for (int i = 0; i < heldPickups.Count; i++)
            {
                averageLocation += heldPickups[i].gameObject.transform.position;
            }
            averageLocation /= heldPickups.Count;

            GameObject[] thrownObjects = new GameObject[heldPickups.Count];
            for (int i = 0; i < thrownObjects.Length; i++)
            {
                thrownObjects[i] = heldPickups[i].gameObject;
                var rigidBody = thrownObjects[i].GetComponent<Rigidbody>();
                if (rigidBody != null)
                {
                    rigidBody.AddExplosionForce(2000, averageLocation, 20, -1, ForceMode.Acceleration);
                    rigidBody.AddTorque(rigidBody.angularVelocity*10, ForceMode.Acceleration);
                }
            }

            if (OnObjectsThrown != null)
            {
                OnObjectsThrown(thrownObjects);
            }

            heldPickups.Clear();
        }
    }

    void Start()
    {   
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach (Pickup pickup in heldPickups)
        {
            var rigidBody = pickup.gameObject.GetComponent<Rigidbody>();
            if (rigidBody)
            {
                Vector3 force = locator.transform.position - pickup.gameObject.transform.position;

                force = pickup.holdPID.Update(force, Time.fixedDeltaTime);

                rigidBody.AddForceAtPosition(force * 0.01f, pickup.pickPoint);
                rigidBody.AddForce(force);
            }
            else
            {
                pickup.gameObject.transform.position = Vector3.Lerp(pickup.gameObject.transform.position, locator.transform.position, Time.deltaTime * 2.0f);
            }
        }
    }
}
