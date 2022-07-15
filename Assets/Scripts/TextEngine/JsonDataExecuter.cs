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
    private bool mProcessingJson;

    public bool Processing => mProcessingJson;

    private string mLastSceneNameAdded = "";

    private CurrentTextFormat mFormatProcessing;

    private LinkedList<EventData> mQueuedEvents = new LinkedList<EventData>();
    private EventData mCurrentEvent = null;
    private bool processedConditionOnCurrentEvent = false;

    private ConversationData mCurrentConversationData = null;
    private ChoiceData mCurrentChoiceData = null;

    private bool mDelayStarted;
    private float mDelayTimer;
    
    private void LoadEvent(string json, bool addToFront)
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
    private void LoadConversation(string json)
    {
        mCurrentConversationData = JsonUtility.FromJson<ConversationData>(json);

        if (mCurrentConversationData.Equals(new ConversationData()))
        {
            throw new Exception("Managed to attempt loading an invalid Conversation Data file. json = " + json);
        }
    }
    private void LoadChoice(string json)
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
    private void AddEventNameToQueue(string eventName, bool addToFront)
    {
        var eventFile = (TextAsset)Resources.Load("Dialogue/" + eventName, typeof(TextAsset));

        Assert.IsNotNull(eventFile, "Unable to load event file " + eventName);

        LoadEvent(eventFile.text, addToFront);
    }
    private void AddEventNamesToQueue(List<string> eventNames)
    {
        foreach (var evt in eventNames)
        {
            AddEventNameToQueue(evt, mCurrentEvent.AddsEventsToFrontOfQueue);
        }
    }

    public void GiveJsonToExecute(CurrentTextFormat jsonFormat, string json)
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
    
    private bool DoesEventConditionPass()
    {
        // Invalid condition settings - we're good to go
        if (mCurrentEvent.ConditionKey.Length == 0 || mCurrentEvent.Condition.Length == 0)
        {
            return true;
        }

        int? data = Service.Data.TryGetData(mCurrentEvent.ConditionKey);

        if (!data.HasValue)
        {
            Debug.LogWarning($"Failed to find condition key of {mCurrentEvent.ConditionKey}");
            return true;
        }

        int numConditions = 0;
        int indexOfCondition = 0;
        int indexOfNumber = 0;

        bool TryGetConditionType(string condition)
        {
            bool containsType = mCurrentEvent.Condition.Contains(condition);

            if (containsType)
            {
                numConditions++;

                // Nothing should be before the condition ">=55" (FOR NOW)
                indexOfCondition = mCurrentEvent.Condition.IndexOf(condition);
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
            Debug.LogError($"Found invalid number of conditions ({numConditions}) for evaluating {mCurrentEvent.ConditionKey} - Condition: {mCurrentEvent.Condition}");
            return true;
        }

        string strNumber = mCurrentEvent.Condition.Substring(indexOfNumber);
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

        Debug.LogError($"Shouldn't have gotten down here: {mCurrentEvent.ConditionKey} - {mCurrentEvent.Condition}");
        return false;
    }


    private bool ProcessEvent_Damage()
    {
        

        return true;
    }

    private bool ProcessEvent_Delay()
    {
        if (!mDelayStarted)
        {
            mDelayStarted = true;
            mDelayTimer = 0.0f;
        }

        if (mDelayTimer < mCurrentEvent.DelayTime)
        {
            mDelayTimer += Time.deltaTime;
            return false;
        }

        mDelayStarted = false;

        return true;
    }

    private bool ProcessEvent_LoadRoom()
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

        if(mCurrentEvent.SceneName.ToLower().Equals("credits")
        || mCurrentEvent.SceneName.ToLower().Equals("failed"))
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

        Assert.IsTrue(scenesInBuild.Contains(mCurrentEvent.SceneName), mCurrentEvent.SceneName + " is not in the build list!");

        //Make sure the scene exists above, then load it additively if so
        SceneManager.LoadScene(mCurrentEvent.SceneName, LoadSceneMode.Additive);

        if (!Service.UI.IsCurrentlyFading)
        {
            Service.UI.ShowGame();
        }

        mLastSceneNameAdded = mCurrentEvent.SceneName;
        return true;
    }

    private bool ProcessEvent_Conversation()
    {
        var convFile = (TextAsset)Resources.Load("Dialogue/" + mCurrentEvent.OpenConversationFile, typeof(TextAsset));

        Assert.IsNull(mCurrentConversationData, "conversation was already loaded when we tried to add a new one: " + mCurrentEvent.OpenConversationFile);
        Assert.IsNotNull(convFile, "failed to load conversation " + mCurrentEvent.OpenConversationFile);

        LoadConversation(convFile.text);

        return true;
    }

    private bool ProcessEvent_Choice()
    {
        var choiceFile = (TextAsset)Resources.Load("Dialogue/" + mCurrentEvent.OpenChoiceFile, typeof(TextAsset));

        Assert.IsNull(mCurrentChoiceData, "choice was already loaded when we tried to add a new one: " + mCurrentEvent.OpenChoiceFile);
        Assert.IsNotNull(choiceFile, "failed to load choice " + mCurrentEvent.OpenChoiceFile);

        LoadChoice(choiceFile.text);

        return true;
    }

    private bool ProcessEvent_Inventory()
    {
        return true;
    }

    private bool ProcessEvent_Event()
    {
        //Just add the events in this to the queue and we're done with this event
        AddEventNamesToQueue(mCurrentEvent.EventsToFire);
        return true;
    }

    private bool ProcessEvent_DeclareDataMembers()
    {
        if (mCurrentEvent.Keys.Count != mCurrentEvent.Values.Count)
        {
            Debug.LogError("DeclareDataMembers: number of keys and values do not match.");
            return true;
        }

        if (Service.Data.IsDataLoaded())
        {
            Debug.LogError("Trying to add data after having already loaded it all.");
            return true;
        }

        for (int i = 0; i < mCurrentEvent.Keys.Count; ++i)
        {
            Service.Data.AddDataMember(mCurrentEvent.Keys[i], mCurrentEvent.Values[i]);
        }

        Service.Data.LoadAll();

        return true;
    }

    private bool ProcessEvent_SetData()
    {
        if (mCurrentEvent.Keys.Count != mCurrentEvent.Values.Count)
        {
            Debug.LogError("SetData: number of keys and values do not match.");
            return true;
        }

        if (!Service.Data.IsDataLoaded())
        {
            Debug.LogError("Trying to set data before loading it all.");
            return true;
        }

        for (int i = 0; i < mCurrentEvent.Keys.Count; ++i)
        {
            Service.Data.TrySetData(mCurrentEvent.Keys[i], mCurrentEvent.Values[i]);
        }

        return true;
    }
    
    private void ProcessCurrentEvent()
    {
        var doneProcessing = false;

        if (!processedConditionOnCurrentEvent)
        {
            processedConditionOnCurrentEvent = true;
            if (!DoesEventConditionPass())
            {
                doneProcessing = true;
            }
            else if(mCurrentEvent.KillOtherEventsWhenConditionTrue)
            {
                mQueuedEvents.Clear();
            }
        }

        if(!doneProcessing)
        {
            switch (mCurrentEvent.Type.ToLower())
            {
                case "damage": doneProcessing = ProcessEvent_Damage();  break;
                case "delay": doneProcessing = ProcessEvent_Delay(); break;
                case "loadroom": doneProcessing = ProcessEvent_LoadRoom(); break;
                case "conversation": doneProcessing = ProcessEvent_Conversation(); break;
                case "choice": doneProcessing = ProcessEvent_Choice(); break;
                case "inventory": doneProcessing = ProcessEvent_Inventory(); break;
                case "event": doneProcessing = ProcessEvent_Event(); break;
                case "declaredatamembers": doneProcessing = ProcessEvent_DeclareDataMembers(); break;
                case "setdata": doneProcessing = ProcessEvent_SetData(); break;

                default: throw new Exception("Event of type " + mCurrentEvent.Type.ToLower() + " is not supported.");
            }
        }
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
