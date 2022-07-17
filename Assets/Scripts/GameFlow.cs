using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

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
        Title,
        GameInit,
        GameUpdate,
        DiceGame,
        GameOver
    }

    // Random chance to play a dice roll quip
    public float DiceRollQuipChanceMin = 10.0f;
    public float DiceRollQuipChanceMax = 20.0f;

    // Random chance to play a conv quip
    public float ConvQuipChanceMin = 10.0f;
    public float ConvQuipChanceMax = 30.0f;

    // Random timer between conv quips trying
    public float TimeBetweenConvQuipsTestMin = 15.0f;
    public float TimeBetweenConvQuipsTestMax = 30.0f;

    public float TimeBetweenQuipChance => nextTimeBetweenConvQuips;

    private float nextTimeBetweenConvQuips = 0.0f;
    private bool allowQuips = false;

#if UNITY_EDITOR
    public float LastConvQuipChanceThreshold = 0.0f;
    public float LastConvQuipChance = 0.0f;
#endif

    public bool ShouldPlayDiceRollQuip()
    {
        float fRandomChanceThreshold = Random.Range(DiceRollQuipChanceMin, DiceRollQuipChanceMax);
        float fChance = Random.Range(0.0f, 100.0f);

        return fChance < fRandomChanceThreshold;
    }

    public bool ShouldPlayConvQuip()
    {
        float fRandomChanceThreshold = Random.Range(ConvQuipChanceMin, ConvQuipChanceMax);
        float fChance = Random.Range(0.0f, 100.0f);

#if UNITY_EDITOR
        LastConvQuipChanceThreshold = fRandomChanceThreshold;
        LastConvQuipChance = fChance;
#endif

        return fChance < fRandomChanceThreshold;
    }

    public bool IsPlayingDice()
    {
        return GameState == State.DiceGame;
    }

    public void SetIsPlayingDice(bool allowRandomQuips = true)
    {
        GameState = State.DiceGame;
        allowQuips = allowRandomQuips;

        nextTimeBetweenConvQuips = Random.Range(TimeBetweenConvQuipsTestMin, TimeBetweenConvQuipsTestMax);
    }


    public AudioSource PuzzleAudioSource;
    public AudioSource TextBoxAudioSource;
    public AudioSource DoorAudioSource;
    public AudioSource MusicAudioSource;
    public AudioSource RainAudioSource;
    public AudioSource ThunderAudioSource;

    public TextAsset JsonScriptToLoadDataMembers;

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

    
    
    public void QuitGameToTitle()
    {
        GameState = State.Title;
        Service.UI.OnGameHidden += OnGameHidden;
        Service.UI.HideGame();
    }

    private void OnGameHidden()
    {
        Service.UI.OnGameHidden -= OnGameHidden;
        Service.UI.GoBackToMenuFromGame();
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

        Service.UI.OnStartGame += OnGameStart;

        if (Service.UI.instantStartGame)
        {
            Service.UI.ProcessGameStartFade(true);
        }
    }

    void Update()
    {
        switch (GameState)
        {
            case State.Title:
                break;
            case State.GameInit:
                StateGameInit();
                break;
            case State.GameUpdate:
                StateGameUpdate();
                break;
            case State.DiceGame:
                StateDiceGame();
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
        mExecuter.Update();
    }

    private void StateDiceGame()
    {
        if (allowQuips)
        {
            nextTimeBetweenConvQuips -= Time.deltaTime;
            if (nextTimeBetweenConvQuips < 0.0f)
            {
                nextTimeBetweenConvQuips = Random.Range(TimeBetweenConvQuipsTestMin, TimeBetweenConvQuipsTestMax);
                Service.Text.TryPlayRandomConvQuip(ChatBox.RandomQuipType);
            }
        }
    }
}
