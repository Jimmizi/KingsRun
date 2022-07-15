using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigSettings : MonoBehaviour
{
    public bool AutomaticEndOfLineSkip;
    public bool InstantText;
    public bool IgnoreTextPauses;

    void Awake()
    {
        Service.Config = this;
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
