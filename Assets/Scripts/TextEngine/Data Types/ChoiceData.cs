using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoiceData
{
    [Serializable]
    public struct ChoiceOption
    {
        public ChoiceOption(string text, string evtfile)
        {
            Text = text;
            EventFile = evtfile;
        }

        /// <summary>
        /// Text for this option
        /// </summary>
        public string Text;

        /// <summary>
        /// What event file will we open when this option is picked
        /// </summary>
        public string EventFile;
    }

    public List<ChoiceOption> Choices = new List<ChoiceOption>();

    [NonSerialized] public int ChoiceTaken = 0;
}
