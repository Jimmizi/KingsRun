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

        

        Vector2 vLeftPosition = new Vector2(5, 5);
        Vector2 vRightPosition = new Vector2(Screen.width - 405, 90);

        GUIStyle rightStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleRight
        };

        rightStyle.normal.textColor = Color.white;
        
        void AddTextLeft(string text)
        {
            GUI.Label(new Rect(vLeftPosition.x, vLeftPosition.y, 400, 24), text);
            vLeftPosition.y += 14;
        }
        void AddTextRight(string text)
        {
            GUI.Label(new Rect(vRightPosition.x, vRightPosition.y, 400, 24), text, rightStyle);
            vRightPosition.y += 14;
        }

        AddTextLeft("Debug (F1)");
        vLeftPosition.y += 14;

        AddTextLeft($"Fade: {Service.UI.FadeState.ToString()}");
        AddTextLeft($"Game: {Service.Flow.GameState.ToString()}");

        AddTextLeft($"Next Pressed: {(Service.Text.nextPressed ? "Y": "N")}");

        if (Service.Text.NextLineButton != null)
        {
            AddTextLeft($"Next Button Ready: {(Service.Text.NextLineButton.interactable ? "Y" : "N")}");
        }

        AddTextLeft($"Quip % Time: {Service.Flow.TimeBetweenQuipChance}");
        AddTextLeft($"Quip %: {Service.Flow.LastConvQuipChance} < {Service.Flow.LastConvQuipChanceThreshold}");
        AddTextLeft($"Last file quit from: {Service.Data.GetFileLastQuitFrom()}");

        // Right side

        if (GUI.Button(new Rect(Screen.width - 205, 5, 200, 24), "Reset Data"))
        {
            Service.Data.DeleteAllData();
        }

        if (GUI.Button(new Rect(Screen.width - 205, 30, 200, 24), "Save Data"))
        {
            Service.Data.SaveAll();
        }

        if (GUI.Button(new Rect(Screen.width - 205, 60, 200, 24), "Times played 1"))
        {
            Service.Data.TrySetData("NumTimesPlayed", 1);
        }

        var persData = Service.Data.DebugGetIntData();
        foreach (var data in persData)
        {
            AddTextRight($"{data.Key}:{data.Value}");
        }

    }
#endif
}
