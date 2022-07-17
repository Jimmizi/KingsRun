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

    // TODO add some keys for dice game "quips"
    // TODO add some keys for ending dice game



    // Update is called once per frame
    void Update()
    {
        if (Service.Flow.IsPlayingDice())
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                int randomQuip = Random.Range(0, 4);

                ConversationData data = new ConversationData();

                switch (randomQuip)
                {
                    case 0:
                        data.Lines.Add("Nice roll.^");
                        break;
                    case 1:
                        data.Lines.Add("Ahh too bad.^");
                        break;
                    case 2:
                        data.Lines.Add("Wow ok.^");
                        break;
                    case 3:
                        data.Lines.Add("Huh, interesting.^");
                        break;
                }

                Service.Text.StartChat(data);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                Service.Text.ResumeFromDiceGame();
            }
        }
    }
#endif
}
