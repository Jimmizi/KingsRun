using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if DEBUG
using System.IO;
using UnityEditor;
#endif
/// <summary>
/// Use this to generate formats for how the data should be set up
/// </summary>
public class JsonTest : MonoBehaviour
{ 
    public TextAsset fileForChoiceData;
    public TextAsset fileForEventData;
    public TextAsset fileForConversationData;
    public TextAsset fileForBarkData;

    public bool UpdateDefaultChoiceFormat;
    public bool UpdateDefaultEventFormat;
    public bool UpdateDefaultConversationFormat;
    public bool UpdateDefaultBarkFormat;

    void GenerateFileFormats()
    {
        ChoiceData choiceData = new ChoiceData();
        choiceData.Choices.Add(new ChoiceData.ChoiceOption("Sample text", "Next file to open"));
        choiceData.Choices.Add(new ChoiceData.ChoiceOption("Sample text 2", "Next file to open 2"));

        ConversationData convData = new ConversationData();
        convData.Lines.Add(new ConversationData.LineData("Speaker Name", "Speech text"));
        convData.Lines.Add(new ConversationData.LineData("Speaker Name 2", "Speech text 2"));

        convData.EventsToFire.Add("EventGiveWeapon");
        convData.EventsToFire.Add("EventLoadStageOne");
        convData.EventsToFire.Add("EventStartFight");

        EventData evtData = new EventData();
        evtData.DamagePartyMemberIndex = 0;
        evtData.DamageAmount = 50;
        evtData.InventoryItemName = "sword";

        evtData.EventsToFire.Add("EventGiveWeapon");
        evtData.EventsToFire.Add("EventLoadStageOne");
        evtData.EventsToFire.Add("EventStartFight");

        BarkData brkData = new BarkData();
        brkData.DreadText.Add("Oh god no...");
        brkData.DreadText.Add("...");
        brkData.ExpletivesText.Add("Holy Mother...");
        brkData.ExpletivesText.Add("$**t");
        brkData.HealMeText.Add("Top me up!");
        brkData.HealMeText.Add("Gimme meds!");
        brkData.HelpMeText.Add("Please help!");
        brkData.HelpMeText.Add("Mercy!");

        string choiceText = JsonUtility.ToJson(choiceData);
        string convText = JsonUtility.ToJson(convData);
        string evtText = JsonUtility.ToJson(evtData);
        string brkText = JsonUtility.ToJson(brkData);

#if DEBUG
        if (UpdateDefaultChoiceFormat)
        {
            File.WriteAllText(AssetDatabase.GetAssetPath(fileForChoiceData), choiceText);
            EditorUtility.SetDirty(fileForChoiceData);
        }

        if (UpdateDefaultEventFormat)
        {
            File.WriteAllText(AssetDatabase.GetAssetPath(fileForEventData), evtText);
            EditorUtility.SetDirty(fileForEventData);
        }

        if (UpdateDefaultConversationFormat)
        {
            File.WriteAllText(AssetDatabase.GetAssetPath(fileForConversationData), convText);
            EditorUtility.SetDirty(fileForConversationData);
        }

        if (UpdateDefaultBarkFormat)
        {
            File.WriteAllText(AssetDatabase.GetAssetPath(fileForBarkData), brkText);
            EditorUtility.SetDirty(fileForBarkData);
        }
#endif
    }

    // Start is called before the first frame update
    void Start()
    {
        //TextAsset pathTxt = (TextAsset)Resources.Load("Dialogue/EventDefaultFormat", typeof(TextAsset));

        GenerateFileFormats();




    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
