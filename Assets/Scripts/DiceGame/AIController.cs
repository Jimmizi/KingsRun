using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public AIPicker picker { get; set; }

    // Start is called before the first frame update
    void Awake()
    {
        picker = GetComponent<AIPicker>();
    }

    // Start is called before the first frame update
    void Start()
    {        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PickupDice(Die[] diceToPick)
    {
        StartCoroutine(AIPickupRoutine(diceToPick));
    }

    IEnumerator AIPickupRoutine(Die[] diceToPick)
    {
        yield return new WaitForSeconds(3);

        for (int i = 0; i < diceToPick.Length; i++)
        {
            picker.Pickup(diceToPick[i].gameObject);
            yield return new WaitForSeconds(1);
        }

        yield return new WaitForSeconds(2);
        picker.Throw();

        yield return null;
    }
}
