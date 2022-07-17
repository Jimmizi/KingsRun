using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

/// <summary>
/// This class runs through and executes the data events, conversations and choices
/// </summary>
public class JsonDataExecuter
{
    private static bool mProcessingJson;

    public bool Processing => mProcessingJson;

    private static string mLastSceneNameAdded = "";

    private static CurrentTextFormat mFormatProcessing;

    private static LinkedList<EventData> mQueuedEvents = new LinkedList<EventData>();
    private static EventData mCurrentEvent = null;
    private static bool processedConditionOnCurrentEvent = false;

    private static ConversationData mCurrentConversationData = null;
    private static ChoiceData mCurrentChoiceData = null;

    private static bool mDelayStarted;
    private static float mDelayTimer;

    public static void ClearOutQueuedEvents()
    {
        mQueuedEvents.Clear();
    }

    public static ConversationData MakeConversation(TextAsset jsonFile)
    {
        Debug.Assert(jsonFile != null);
        return MakeConversation(jsonFile.text);
    }
    public static ConversationData MakeConversation(string json)
    {
        ConversationData data = JsonUtility.FromJson<ConversationData>(json);
        if (data.Equals(new ConversationData()))
        {
            throw new Exception("Managed to attempt loading an invalid Conversation Data file. json = " + json);
        }

        foreach (var line in data.Lines)
        {
            if (line.Length >= 175)
            {
                // Not the biggest issue, but if much bigger than 130, once the font has resized and the spacing reduced we do have a chance to start overflowing
                Debug.LogWarning($"Warning: dialogue line more than 130 ({line.Length}) - Consider making this into multiple lines otherwise it will incur font resizing. Line: {line}");
            }
        }

        return data;
    }

    public static EventData MakeEvent(TextAsset jsonFile)
    {
        Debug.Assert(jsonFile != null);
        return MakeEvent(jsonFile.text);
    }
    public static EventData MakeEvent(string json)
    {
        EventData data = JsonUtility.FromJson<EventData>(json);
        if (data.Equals(new EventData()))
        {
            throw new Exception("Managed to attempt loading an invalid event Data file. json = " + json);
        }

        return data;
    }

    private static void LoadEvent(string json, bool addToFront)
    {
        Debug.Log("Loading EventData: " + json);

        if (!addToFront)
        {
            mQueuedEvents.AddLast(JsonUtility.FromJson<EventData>(json));
        }
        else
        {
            mQueuedEvents.AddFirst(JsonUtility.FromJson<EventData>(json));
        }

        if (mQueuedEvents.Last.Value.Equals(new EventData()))
        {
            throw new Exception("Managed to attempt loading an invalid Event Data file. json = " + json);
        }
    }
    private static void LoadConversation(string json)
    {
        Debug.Log("Loading Conversation: " + json);

        mCurrentConversationData = MakeConversation(json);
    }
    private static void LoadChoice(string json)
    {
        mCurrentChoiceData = JsonUtility.FromJson<ChoiceData>(json);

        if (mCurrentChoiceData.Equals(new ChoiceData()))
        {
            throw new Exception("Managed to attempt loading an invalid Choice Data file. json = " + json);
        }
    }

    /// <summary>
    /// Add the name of the event to the queue of events to process in order
    /// </summary>
    /// <param name="eventName">path and name of the event from the Resource/Dialogue/ folder</param>
    private static void AddEventNameToQueue(string eventName, bool addToFront)
    {
        var eventFile = (TextAsset)Resources.Load("Dialogue/" + eventName, typeof(TextAsset));

        Assert.IsNotNull(eventFile, "Unable to load event file " + eventName);

        LoadEvent(eventFile.text, addToFront);
    }
    private static void AddEventNamesToQueue(List<string> eventNames)
    {
        foreach (var evt in eventNames)
        {
            AddEventNameToQueue(evt, mCurrentEvent.AddsEventsToFrontOfQueue);
        }
    }
    
    public static void GiveJsonToExecute(CurrentTextFormat jsonFormat, string json)
    {
        mFormatProcessing = jsonFormat;

        switch(jsonFormat)
        {
            case CurrentTextFormat.Event:
                LoadEvent(json, false);
                break;
            case CurrentTextFormat.Conversation:
                LoadConversation(json);
                break;
            case CurrentTextFormat.Choice:
                LoadChoice(json);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(jsonFormat), jsonFormat, null);
        }

        mProcessingJson = true;
    }

    #region Processing Events
    
    private static bool DoesEventConditionPass(EventData evt)
    {
        // Invalid condition settings - we're good to go
        if (evt.ConditionKey.Length == 0 || evt.Condition.Length == 0)
        {
            return true;
        }

        int? data = Service.Data.TryGetData(evt.ConditionKey);

        if (!data.HasValue)
        {
            Debug.LogWarning($"Failed to find condition key of {evt.ConditionKey}");
            return true;
        }

        int numConditions = 0;
        int indexOfCondition = 0;
        int indexOfNumber = 0;

        bool TryGetConditionType(string condition)
        {
            bool containsType = evt.Condition.Contains(condition);

            if (containsType)
            {
                numConditions++;

                // Nothing should be before the condition ">=55" (FOR NOW)
                indexOfCondition = evt.Condition.IndexOf(condition);
                Debug.Assert(indexOfCondition == 0);

                indexOfNumber = condition.Length;
            }

            return containsType;
        }

        bool equalsCondition = TryGetConditionType("==");
        bool moreThanCondition = TryGetConditionType(">");
        bool moreThanEqualCondition = TryGetConditionType(">=");
        bool lessThanCondition = TryGetConditionType("<");
        bool lessThanEqualCondition = TryGetConditionType("<=");
        bool doesNotEqualCondition = TryGetConditionType("!=");

        if (numConditions != 1)
        {
            Debug.LogError($"Found invalid number of conditions ({numConditions}) for evaluating {evt.ConditionKey} - Condition: {evt.Condition}");
            return true;
        }

        string strNumber = evt.Condition.Substring(indexOfNumber);
        int number = 0;

        try
        {
            number = Int32.Parse(strNumber);
        }
        catch (FormatException)
        {
            Debug.LogError($"Unable to parse '{strNumber}'");
            return true;
        }

        if (equalsCondition)
        {
            return data.Value == number;
        }
        else if (moreThanCondition)
        {
            return data.Value > number;
        }
        else if(moreThanEqualCondition)
        {
            return data.Value >= number;
        }
        else if(lessThanCondition)
        {
            return data.Value < number;
        }
        else if(lessThanEqualCondition)
        {
            return data.Value <= number;
        }
        else if(doesNotEqualCondition)
        {
            return data.Value != number;
        }

        Debug.LogError($"Shouldn't have gotten down here: {evt.ConditionKey} - {evt.Condition}");
        return false;
    }


    private static bool ProcessEvent_Damage(EventData evt)
    {
        

        return true;
    }

    private static bool ProcessEvent_Delay(EventData evt)
    {
        if (!mDelayStarted)
        {
            mDelayStarted = true;
            mDelayTimer = 0.0f;
        }

        if (mDelayTimer < evt.DelayTime)
        {
            mDelayTimer += Time.deltaTime;
            return false;
        }

        mDelayStarted = false;

        return true;
    }

    private static bool ProcessEvent_LoadRoom(EventData evt)
    {
        //Not the first load scene
        if (mLastSceneNameAdded.Length > 0)
        {
            if (Service.UI.IsGameVisible)
            {
                if (!Service.UI.IsCurrentlyFading)
                {
                    Service.UI.HideGame();
                    return false;
                }

                return false;
            }
            else if (Service.UI.IsCurrentlyFading)
            {
                return false;
            }
        }

        if(evt.SceneName.ToLower().Equals("credits")
        || evt.SceneName.ToLower().Equals("failed"))
        {
            //Service.Party().KillParty(true);
            //Service.UI.SetCameraFaderAlpha(false);

            //Service.Audio().StopGameLoopMusic();
            //Service.Audio().PlayMenuMusic();
        }
        
        //Make sure to unload the last scene
        if (mLastSceneNameAdded.Length > 0)
        {
            SceneManager.UnloadSceneAsync(mLastSceneNameAdded);
            mLastSceneNameAdded = "";
        }

        var scenesInBuild = new List<string>();
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            int lastSlash = scenePath.LastIndexOf("/");
            scenesInBuild.Add(scenePath.Substring(lastSlash + 1, scenePath.LastIndexOf(".") - lastSlash - 1));
        }

        Assert.IsTrue(scenesInBuild.Contains(evt.SceneName), evt.SceneName + " is not in the build list!");

        //Make sure the scene exists above, then load it additively if so
        SceneManager.LoadScene(evt.SceneName, LoadSceneMode.Additive);

        if (!Service.UI.IsCurrentlyFading)
        {
            Service.UI.ShowGame();
        }

        mLastSceneNameAdded = evt.SceneName;
        return true;
    }

    private static bool ProcessEvent_Conversation(EventData evt)
    {
        var convFile = (TextAsset)Resources.Load("Dialogue/" + evt.OpenConversationFile, typeof(TextAsset));

        Assert.IsNull(mCurrentConversationData, "conversation was already loaded when we tried to add a new one: " + evt.OpenConversationFile);
        Assert.IsNotNull(convFile, "failed to load conversation " + evt.OpenConversationFile);

        LoadConversation(convFile.text);

        return true;
    }

    private static bool ProcessEvent_Choice(EventData evt)
    {
        var choiceFile = (TextAsset)Resources.Load("Dialogue/" + evt.OpenChoiceFile, typeof(TextAsset));

        Assert.IsNull(mCurrentChoiceData, "choice was already loaded when we tried to add a new one: " + evt.OpenChoiceFile);
        Assert.IsNotNull(choiceFile, "failed to load choice " + evt.OpenChoiceFile);

        LoadChoice(choiceFile.text);

        return true;
    }

    private static bool ProcessEvent_Inventory(EventData evt)
    {
        return true;
    }

    private static bool ProcessEvent_Event(EventData evt)
    {
        //Just add the events in this to the queue and we're done with this event
        AddEventNamesToQueue(evt.EventsToFire);
        return true;
    }

    private static bool ProcessEvent_DeclareDataMembers(EventData evt)
    {
        if (evt.Keys.Count != evt.Values.Count)
        {
            Debug.LogError("DeclareDataMembers: number of keys and values do not match.");
            return true;
        }

        if (Service.Data.IsDataLoaded())
        {
            Debug.LogError("Trying to add data after having already loaded it all.");
            return true;
        }

        for (int i = 0; i < evt.Keys.Count; ++i)
        {
            Service.Data.AddDataMember(evt.Keys[i], evt.Values[i]);
        }

        Service.Data.LoadAll();

        return true;
    }

    private static bool ProcessEvent_SetData(EventData evt)
    {
        if (evt.Keys.Count != evt.Values.Count)
        {
            Debug.LogError("SetData: number of keys and values do not match.");
            return true;
        }

        if (!Service.Data.IsDataLoaded())
        {
            Debug.LogError("Trying to set data before loading it all.");
            return true;
        }

        for (int i = 0; i < evt.Keys.Count; ++i)
        {
            Service.Data.TrySetData(evt.Keys[i], evt.Values[i]);
        }

        return true;
    }

    private static bool ProcessEvent_IncrementData(EventData evt)
    {
        if (!Service.Data.IsDataLoaded())
        {
            Debug.LogError("Trying to set data before loading it all.");
            return true;
        }

        for (int i = 0; i < evt.Keys.Count; ++i)
        {
            int? data = Service.Data.TryGetData(evt.Keys[i]);
            Debug.Assert(data.HasValue);

            Service.Data.TrySetData(evt.Keys[i], data.Value + 1);
        }

        return true;
    }

    public static bool ProcessEvent(EventData evt, bool tryCondition)
    {
        if (tryCondition)
        {
            if (!DoesEventConditionPass(evt))
            {
                return true;
            }
            else if (evt.KillOtherEventsWhenConditionTrue)
            {
                mQueuedEvents.Clear();
            }
        }
        switch (evt.Type.ToLower())
        {
            case "damage": return ProcessEvent_Damage(evt);
            case "delay": return ProcessEvent_Delay(evt); 
            case "loadroom": return ProcessEvent_LoadRoom(evt); 
            case "conversation": return ProcessEvent_Conversation(evt); 
            case "choice": return ProcessEvent_Choice(evt); 
            case "inventory": return ProcessEvent_Inventory(evt); 
            case "event": return ProcessEvent_Event(evt); 
            case "declaredatamembers": return ProcessEvent_DeclareDataMembers(evt); 
            case "setdata": return ProcessEvent_SetData(evt);
            case "incrementdata": return ProcessEvent_IncrementData(evt);

            default: throw new Exception("Event of type " + evt.Type.ToLower() + " is not supported.");
        }

        return true;
    }
    
    private void ProcessCurrentEvent()
    {
        var doneProcessing = ProcessEvent(mCurrentEvent, !processedConditionOnCurrentEvent);
        processedConditionOnCurrentEvent = true;

        if (doneProcessing)
        {
            processedConditionOnCurrentEvent = false;
            mCurrentEvent = null;
        }
    }

    /// <summary>
    /// Dequeues and processes events in order until no more remain in the queue
    /// </summary>
    /// <returns></returns>
    private bool ProcessEventQueue()
    {
        if (mCurrentEvent == null && mQueuedEvents.Count > 0)
        {
            mCurrentEvent = mQueuedEvents.First.Value;
            mQueuedEvents.RemoveFirst();
        }

        if (mCurrentEvent != null)
        {
            ProcessCurrentEvent();
        }

        if(mCurrentEvent == null && mQueuedEvents.Count == 0)
        {
            processedConditionOnCurrentEvent = false;
            mCurrentEvent = null;
        }

        return mQueuedEvents.Count <= 0 && mCurrentEvent == null;
    }

    #endregion

    /// <summary>
    /// Update the current json
    /// </summary>
    /// <returns>true if switching json files</returns>
    public bool Update()
    {
        switch (mFormatProcessing)
        {
            case CurrentTextFormat.Event:

                if (mCurrentEvent != null)
                {
                    ProcessCurrentEvent();
                }
                else
                {
                    //True if no events left in the queue, move onto conversation or choice if valid
                    if (ProcessEventQueue())
                    {
                        if (mCurrentConversationData != null)
                        {
                            mFormatProcessing = CurrentTextFormat.Conversation;
                        }
                        else if (mCurrentChoiceData != null)
                        {
                            mFormatProcessing = CurrentTextFormat.Choice;
                        }
                        else
                        {
                            //done with the adventure
                            return true;
                        }
                    }
                }

                break;
            case CurrentTextFormat.Conversation:
                if (!Service.Text.Processing)
                {
                    if (!Service.Text.HasValidChat)
                    {
                        Service.Text.StartChat(mCurrentConversationData);
                    }
                    else
                    {
                        Service.Text.EndCleanup();

                        //Done with chatbox, fire any events off
                        AddEventNamesToQueue(mCurrentConversationData.EventsToFire);

                        //Conversations always launch events
                        mFormatProcessing = CurrentTextFormat.Event;
                        mCurrentConversationData = null;
                    }
                }
                break;
            case CurrentTextFormat.Choice:
                if (!Service.Text.Processing)
                {
                    if (!Service.Text.HasValidChat)
                    {
                        Service.Text.StartChat(mCurrentChoiceData);
                    }
                    else
                    {
                        Service.Text.EndCleanup();

                        //Done with chatbox, fire any events off
                        AddEventNameToQueue(mCurrentChoiceData.Choices[mCurrentChoiceData.ChoiceTaken].EventFile, false);

                        //Choices always launch events
                        mFormatProcessing = CurrentTextFormat.Event;
                        mCurrentChoiceData = null;
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return false;
    }

}
