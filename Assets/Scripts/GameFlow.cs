using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum CurrentTextFormat
{
    Event,
    Conversation,
    Choice
}

/// <summary>
/// Used to direct the flow of the game and how it plays out
/// </summary>
public class GameFlow : MonoBehaviour
{
    public enum State
    {

        WaitForGameStart,
        GameInit,
        GameUpdate,
        GameOver
    }


    private State gameStateInternal;
    public State GameState
    {
        get => gameStateInternal;
        set
        {
            Debug.Log($"Changing from game state {gameStateInternal.ToString()} to {value.ToString()}");
            gameStateInternal = value;
        }
    }



    /// <summary>
    /// Event File to start off the game
    /// </summary>
    public TextAsset StartEvent;

    private JsonDataExecuter mExecuter = new JsonDataExecuter();
    
    
    void Awake()
    {
        Service.Flow = this;
        Service.Executer = mExecuter;
    }
    
    void Start()
    {
        Assert.IsNotNull(StartEvent);

        Service.UI.OnFirstGameShown += OnGameStart;
    }

    void Update()
    {
        switch (GameState)
        {
            case State.WaitForGameStart:
                break;
            case State.GameInit:
                StateGameInit();
                break;
            case State.GameUpdate:
                StateGameUpdate();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnGameStart()
    {
        GameState = State.GameInit;
    }

    private void StateGameInit()
    {
        Debug.Assert(!mExecuter.Processing);
        mExecuter.GiveJsonToExecute(CurrentTextFormat.Event, StartEvent.text);

        GameState = State.GameUpdate;
    }

    private void StateGameUpdate()
    {
        if (mExecuter.Update())
        {
            GameState = State.GameOver;
        }
    }
}
