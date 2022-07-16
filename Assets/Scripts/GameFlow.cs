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

    [SerializeField]
    private bool startGameHidden;

    [SerializeField]
    [Tooltip("Time to delay at the start of the game before fading in (if startGameHidden set)")]
    private float startHiddenShowDelayTime = 1.0f;


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
        Service.UI.ProcessGameStartFade();
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
            case State.GameOver:
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
        JsonDataExecuter.GiveJsonToExecute(CurrentTextFormat.Event, StartEvent.text);

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
