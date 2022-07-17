using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoundCounterWidget : MonoBehaviour
{
    [SerializeField]
    Image[] turnImages;

    [SerializeField]
    Sprite offTurnImage;

    [SerializeField]
    Sprite onTurnImage;

    [SerializeField]
    public int activeTurn = 0;

    [SerializeField]
    float flickerTime = 1.0f;
    
    float flickerTimer = 1.0f;
    bool flickerToggle = false;
    int numOnTurns = -1;

    void Update()
    {
        if (flickerTimer >= 0)
        {
            flickerTimer -= Time.deltaTime;
            if (flickerTimer <= 0)
            {
                flickerTimer = flickerTime;
                flickerToggle = !flickerToggle;
            }
        }

        int newNumOnTurns = flickerToggle ? activeTurn : activeTurn - 1;
        if (newNumOnTurns != numOnTurns)
        {
            numOnTurns = newNumOnTurns;
            SetNumTurns(numOnTurns);
        }        
    }

    void SetNumTurns(int numTurns)
    {
        for(int i = 0; i < turnImages.Length; i++)
        {
            bool isOn = (i < numTurns);
            turnImages[i].sprite = isOn ? onTurnImage : offTurnImage;
        }
    }
}
