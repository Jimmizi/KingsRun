using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationData
{
    [Serializable]
    public struct LineData
    {
        public LineData(string speakerStr, string speechStr)
        {
            Speaker = speakerStr;
            Speech = speechStr;
        }

        /// <summary>
        /// Speaker name to display at the top of the dialogue box or bark text.
        /// </summary>
        public string Speaker;

        /// <summary>
        /// What is the speaker saying.
        /// </summary>
        public string Speech;
    }

    public string Filepath = "";

    /// <summary>
    /// Lines in this section of dialogue
    /// </summary>

    // If we want to re-enable speaker names
    //public List<LineData> Lines = new List<LineData>();

    public List<string> Lines = new List<string>();

    // One liners specifically for dice game musings
    public List<string> Quips = new List<string>();

    /// <summary>
    /// List of events to fire. Only the first choice or conversation event is accepted if there are duplicates.
    /// </summary>
    public List<string> EventsToFire = new List<string>();
    
    public int ShowQuitButtonLine = -1;
    public bool ShowQuitButtonStartOfLine = false;

    public int HideQuitButtonLine = -1;
    public bool HideQuitButtonStartOfLine = false;

    public int QuitButtonInterruptLineToResetButton = -1;
    public bool MoveQuitButtonAtStartOfLine = false;
    public float QuitButtonMoveSpeedOverride = 0.0f;

    public int QuitButtonLineToActivate = -1;

    public bool QuitGameAfterConversationFinish = false;

    public string DataToSetAfterLines = "";
    public int DataValue = 0;

    // -1 = No game, 0 = Free Play, 1 = Win, 2 = Loss
    public int LaunchDiceGameMode = -1;

    // Scalar value of how intense (how close) a non free play dice game will be
    public float DiceGameIntensity = 0.0f;

    // When finishing up the dice game, add this conversation file
    public string ConversationToLaunchAfterDiceGame = "";

    // Hacked conversation route - easier than having to set up a whole bunch of events to fire off conversations
    //  NOTE will only work if we haven't added events to fire
    public string NextConversation = "";
    
    public int RainPtfxTurnOffAtStartOfLine = -1;
    
    public bool WillLaunchDiceGame()
    {
        return LaunchDiceGameMode >= 0 && LaunchDiceGameMode <= 2;
    }
}
