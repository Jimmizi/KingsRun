using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiceRollRigger : MonoBehaviour
{
    Scene predictionScene;

    public class Pairing
    {
        public GameObject realObject;
        public GameObject predictionObject;
        public Rigidbody realRigidBody;
        public Rigidbody predictedRigidBody;
        public Die realDie;
        public Die predictedDie;
        public List<Quaternion> simulatedRotations = new List<Quaternion>();
        public List<Vector3> simulatedPositions = new List<Vector3>();
        public int firstMovementFrame = -1;
        public int lastMovementFrame = -1;
        public float totalMovement = 0;
        public Quaternion rotationAdjustment = Quaternion.identity;
    }

    public Dictionary<string, Pairing> predictionPairings = new Dictionary<string, Pairing>();
    int simulatedFrame = -1;

    public bool isSimulating => simulatedFrame >= 0;

    // Start is called before the first frame update
    void Start()
    {
        Physics.autoSimulation = false;

        CreatePredictionScene();        
    }

    void CreatePredictionScene()
    {
        CreateSceneParameters physicsParams = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        predictionScene = SceneManager.CreateScene("PhysicsPrediction", physicsParams);

        Collider[] colliders = FindObjectsOfType<Collider>();
        foreach(Collider collider in colliders)
        {
            var rigidBody = collider.GetComponent<Rigidbody>();
            if (!rigidBody)
            {
                CreatePredictionReplica(collider.gameObject);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        Physics.defaultPhysicsScene.Simulate(Time.fixedDeltaTime);

        bool anySimulated = false;
        if (simulatedFrame >= 0)
        {
            foreach (Pairing pairing in predictionPairings.Values)
            {
                if (pairing.simulatedPositions.Count > simulatedFrame)
                {                                        
                    anySimulated = true;

                    int activeFrames = pairing.lastMovementFrame - pairing.firstMovementFrame;
                    float adjustBy = Mathf.Clamp(activeFrames * 0.5f, 1, 40 );
                    float adjustmentRate = Mathf.Clamp01(simulatedFrame - pairing.firstMovementFrame / adjustBy);
                    Quaternion.Slerp(Quaternion.identity, pairing.rotationAdjustment, adjustmentRate);
                    Quaternion rotation = pairing.rotationAdjustment;

                    pairing.realRigidBody.rotation = rotation * pairing.simulatedRotations[simulatedFrame];
                    pairing.realRigidBody.position = pairing.simulatedPositions[simulatedFrame];
                }
            }

            simulatedFrame++;

            if (!anySimulated)
            {
                simulatedFrame = -1;
                foreach (Pairing pairing in predictionPairings.Values)
                {
                    pairing.realRigidBody.isKinematic = false;

                    // Hack for testing purposes :)

                    /*
                    var die = pairing.realRigidBody.GetComponent<Die>();

                    Quaternion rotation = die.GetRequiredRotationToValue(3);
                    Quaternion newRotation = rotation * pairing.realRigidBody.rotation;

                    pairing.realRigidBody.rotation = newRotation;
                    pairing.realRigidBody.angularVelocity = Vector3.zero;
                    pairing.realRigidBody.velocity = Vector3.zero;
                    pairing.realObject.transform.rotation = pairing.realRigidBody.rotation;

                    pairing.realRigidBody.ResetInertiaTensor();
                    pairing.realRigidBody.Sleep();
                    */
                }
            }
        }
    }

    public void SimulateThrow(Die[] diceToSimulate)
    {
        List<Pairing> pairingsToSimulate = new List<Pairing>();
        simulatedFrame = 0;

        foreach (Pairing pairing in predictionPairings.Values)
        {
            pairing.simulatedPositions.Clear();
            pairing.simulatedRotations.Clear();
            pairing.firstMovementFrame = -1;
            pairing.lastMovementFrame = -1;
            pairing.totalMovement = 0;
        }

        foreach (Die die in diceToSimulate)
        {
            Pairing pairing = FindOrCreatePredictionPairing(die.gameObject);
            SyncPhysicsProperties(pairing.realObject, pairing.predictionObject);

            pairingsToSimulate.Add(pairing);
        }

        const int maxFrames = 100000;
        for (int i = 0; i < maxFrames; i++)
        {
            predictionScene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);

            bool isAnyMoving = false;
            foreach (Pairing pairing in pairingsToSimulate)
            {
                pairing.simulatedPositions.Add(pairing.predictionObject.transform.position);
                pairing.simulatedRotations.Add(pairing.predictionObject.transform.rotation);

                float movement =
                    pairing.predictedRigidBody.velocity.sqrMagnitude +
                    pairing.predictedRigidBody.angularVelocity.sqrMagnitude;

                if (movement > 0.01f)
                {
                    isAnyMoving = true;
                    pairing.totalMovement += movement;

                    pairing.lastMovementFrame = i;
                    if (pairing.firstMovementFrame < 0)
                    {
                        pairing.firstMovementFrame = i;
                    }
                }
            }

            if (!isAnyMoving)
            {
                break;
            }
        }

        foreach (Pairing pairing in predictionPairings.Values)
        {
            var rigidBody = pairing.realObject.GetComponent<Rigidbody>();
            if (rigidBody)
            {
                rigidBody.isKinematic = true;
            }

            pairing.rotationAdjustment = Quaternion.identity;
        }
    }

    public bool RigDieResult(Die die, int newSide)
    {
        Pairing pairing;
        if (predictionPairings.TryGetValue(die.gameObject.name, out pairing))
        {
            if (pairing.predictedDie)
            {
                pairing.rotationAdjustment = pairing.predictedDie.GetRequiredRotationToValue(newSide);
                return true;
            }
        }
        return false;
    }

    public int GetPredictedResult(Die die)
    {
        Pairing pairing;
        if (predictionPairings.TryGetValue(die.gameObject.name, out pairing))
        {
            if (pairing.predictedDie)
            {
                return pairing.predictedDie.value;
            }
        }
        return 0;
    }

    public float GetPredictedTotalMovement(Die die)
    {
        Pairing pairing;
        if (predictionPairings.TryGetValue(die.gameObject.name, out pairing))
        {
            if (pairing.predictedDie)
            {
                return pairing.totalMovement;
            }
        }
        return 0;
    }

    private Pairing FindOrCreatePredictionPairing(GameObject original)
    {
        if (!predictionPairings.ContainsKey(original.name))
        {
            Pairing pairing = new Pairing();
            pairing.realObject = original;
            pairing.predictionObject = CreatePredictionReplica(original);
            pairing.realRigidBody = pairing.realObject.GetComponent<Rigidbody>();
            pairing.predictedRigidBody = pairing.predictionObject.GetComponent<Rigidbody>();
            pairing.realDie = pairing.realObject.GetComponent<Die>();
            pairing.predictedDie = pairing.predictionObject.GetComponent<Die>();

            predictionPairings.Add(original.name, pairing);
        }

        return predictionPairings[original.name];
    }

    private GameObject CreatePredictionReplica(GameObject original)
    {
        GameObject predictionReplica = Instantiate(original);
        predictionReplica.name = original.name;

        var meshRenderer = predictionReplica.GetComponent<MeshRenderer>();
        if (meshRenderer)
        {
            Destroy(meshRenderer);
        }

        SceneManager.MoveGameObjectToScene(predictionReplica, predictionScene);
        return predictionReplica;
    }

    // Syncs the physics properties from source object to target object
    private void SyncPhysicsProperties(GameObject source, GameObject target)
    {
        target.SetActive(source.activeInHierarchy);
        target.transform.position = source.transform.position;
        target.transform.rotation = source.transform.rotation;

        var sourceRigidBody = source.GetComponent<Rigidbody>();
        var targetRigidBody = target.GetComponent<Rigidbody>();
        if (sourceRigidBody && targetRigidBody)
        {            
            targetRigidBody.position = sourceRigidBody.position;
            targetRigidBody.velocity = sourceRigidBody.velocity;
            targetRigidBody.drag = sourceRigidBody.drag;
            targetRigidBody.centerOfMass = sourceRigidBody.centerOfMass;
            targetRigidBody.inertiaTensor = sourceRigidBody.inertiaTensor;
            targetRigidBody.maxDepenetrationVelocity = sourceRigidBody.maxDepenetrationVelocity;

            targetRigidBody.rotation = sourceRigidBody.rotation;
            targetRigidBody.angularVelocity = sourceRigidBody.angularVelocity;            
            targetRigidBody.maxAngularVelocity = sourceRigidBody.maxAngularVelocity;            
            targetRigidBody.angularDrag = sourceRigidBody.angularDrag;
            targetRigidBody.inertiaTensorRotation = sourceRigidBody.inertiaTensorRotation;
        }
    }
}
