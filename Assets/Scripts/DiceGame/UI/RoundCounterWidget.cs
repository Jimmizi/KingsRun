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
    float flickerTimer = 1.0f;

    IEnumerator Start()
    {
        while (true)
        {
            SetNumTurns(activeTurn);
            yield return new WaitForSeconds(flickerTimer);

            SetNumTurns(activeTurn-1);
            yield return new WaitForSeconds(flickerTimer);
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
