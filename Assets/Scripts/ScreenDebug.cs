using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenDebug : MonoBehaviour
{
#if UNITY_EDITOR

    private bool debugVisible = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            debugVisible = !debugVisible;
        }
    }

    private void OnGUI()
    {
        if (!debugVisible)
        {
            return;
        }

        Vector2 vPosition = new Vector2(5, 5);

        void IncrementVerticalPos()
        {
            vPosition.y += 14;
        }
        void AddText(string text)
        {
            GUI.Label(new Rect(vPosition.x, vPosition.y, 400, 24), text);
            IncrementVerticalPos();
        }

        AddText("Debug (F1)");
        IncrementVerticalPos();

        AddText($"Fade: {Service.UI.FadeState.ToString()}");
        AddText($"Game: {Service.Flow.GameState.ToString()}");

    }
#endif
}
