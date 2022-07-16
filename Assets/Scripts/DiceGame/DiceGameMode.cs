using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceGameMode : MonoBehaviour
{
    [SerializeField]
    Die playerDiePrefab;

    [SerializeField]
    Die aiDiePrefab;

    PlayerController playerController;
    AIController aiController;

    DiceRollRigger rigger;

    private void Awake()
    {
        rigger = GetComponent<DiceRollRigger>();
        playerController = FindObjectOfType<PlayerController>();
        aiController = FindObjectOfType<AIController>();        
    }

    // Start is called before the first frame update
    void Start()
    {
        playerController.picker.OnObjectsThrown += OnPlayerDiceRolled;
        aiController.picker.OnObjectsThrown += OnPlayerDiceRolled;
    }

    private void OnPlayerDiceRolled(GameObject[] thrownObjects)
    {
        StartCoroutine(ThrowSim(thrownObjects));
    }

    IEnumerator ThrowSim(GameObject[] thrownObjects)
    {
        yield return new WaitForSeconds(Time.fixedDeltaTime * 10);

        rigger.SimulateThrow(thrownObjects);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
