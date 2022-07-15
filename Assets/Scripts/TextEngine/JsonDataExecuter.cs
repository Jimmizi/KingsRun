using System;
using System.Collections;
using System.Collections.Generic;
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

    private Queue<EventData> mQueuedEvents = new Queue<EventData>();
    private EventData mCurrentEvent = null;

    private ConversationData mCurrentConversationData = null;
    private ChoiceData mCurrentChoiceData = null;

    private bool mDelayStarted;
    private float mDelayTimer;

    private void LoadEvent(string json)
    {
        Debug.Log("Loading EventData: " + json);
        mQueuedEvents.Enqueue(JsonUtility.FromJson<EventData>(json));

        if (mQueuedEvents.Peek().Equals(new EventData()))
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
    private void AddEventNameToQueue(string eventName)
    {
        var eventFile = (TextAsset)Resources.Load("Dialogue/" + eventName, typeof(TextAsset));

        Assert.IsNotNull(eventFile, "Unable to load event file " + eventName);

        LoadEvent(eventFile.text);
    }
    private void AddEventNamesToQueue(List<string> eventNames)
    {
        foreach (var evt in eventNames)
        {
            AddEventNameToQueue(evt);
        }
    }

    public void GiveJsonToExecute(CurrentTextFormat jsonFormat, string json)
    {
        mFormatProcessing = jsonFormat;

        switch(jsonFormat)
        {
            case CurrentTextFormat.Event:
                LoadEvent(json);
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

    private void ProcessCurrentEvent()
    {
        var doneProcessing = false;

        // damage|delay|loadroom|conversation|choice|inventory|event
        switch (mCurrentEvent.Type.ToLower())
        {
            case "damage": doneProcessing = ProcessEvent_Damage();  break;
            case "delay": doneProcessing = ProcessEvent_Delay(); break;
            case "loadroom": doneProcessing = ProcessEvent_LoadRoom(); break;
            case "conversation": doneProcessing = ProcessEvent_Conversation(); break;
            case "choice": doneProcessing = ProcessEvent_Choice(); break;
            case "inventory": doneProcessing = ProcessEvent_Inventory(); break;
            case "event": doneProcessing = ProcessEvent_Event(); break;

            default: throw new Exception("Event of type " + mCurrentEvent.Type.ToLower() + " is not supported.");
        }

        if (doneProcessing)
        {
            mCurrentEvent = null;
        }
    }

    /// <summary>
    /// Dequeues and processes events in order until no more remain in the queue
    /// </summary>
    /// <returns></returns>
    private bool ProcessEventQueue()
    {
        if (mQueuedEvents.Count > 0)
        {
            mCurrentEvent = mQueuedEvents.Dequeue();
            ProcessCurrentEvent();
        }
        else
        {
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
                        AddEventNameToQueue(mCurrentChoiceData.Choices[mCurrentChoiceData.ChoiceTaken].EventFile);

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
