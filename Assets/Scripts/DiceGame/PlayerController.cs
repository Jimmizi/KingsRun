using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Picker picker { get; set; }
    public DiceRollRigger rigger { get; set; }

    private void Awake()
    {
        picker = GetComponent<Picker>();
        rigger = GetComponent<DiceRollRigger>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
