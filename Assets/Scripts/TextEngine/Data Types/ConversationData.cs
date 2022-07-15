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

    /// <summary>
    /// Lines in this section of dialogue
    /// </summary>
    public List<LineData> Lines = new List<LineData>();

    /// <summary>
    /// List of events to fire. Only the first choice or conversation event is accepted if there are duplicates.
    /// </summary>
    public List<string> EventsToFire = new List<string>();
}
