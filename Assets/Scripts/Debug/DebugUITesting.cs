using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugUITesting : MonoBehaviour
{
#if UNITY_EDITOR
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Service.UI.TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            Service.UI.ShowGame();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            Service.UI.HideGame();
        }
    }
#endif
}
