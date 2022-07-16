using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DiceGameHUD : MonoBehaviour
{
    DiceGameMode gameMode;

    [SerializeField]
    PageSwitcher pageSwitcher;

    [SerializeField]
    GameObject colorSelect;

    [SerializeField]
    GameObject gameHud;

    [SerializeField]
    GameObject victoryScreen;

    [SerializeField]
    TextMeshProUGUI roundText;

    [SerializeField]
    TextMeshProUGUI playerScoreText;

    [SerializeField]
    TextMeshProUGUI aiScoreText;

    [SerializeField]
    TextMeshProUGUI gameEndText;

    void Awake()
    {
        gameMode = FindObjectOfType<DiceGameMode>();
        gameMode.OnGameStateChanged += OnGameStateChanged;
    }    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnGameStateChanged(DiceGameMode.GameState newState, DiceGameMode.GameState oldState)
    {
        switch (newState)
        {
            case DiceGameMode.GameState.WaitingToStart:
                pageSwitcher.SetActivePage(colorSelect);
                break;

            case DiceGameMode.GameState.WhitePickingDice:
            case DiceGameMode.GameState.BlackPickingDice:
                UpdateRound();
                break;

            case DiceGameMode.GameState.WhiteThrowSettled:
            case DiceGameMode.GameState.BlackThrowSettled:
                UpdateScore();
                gameMode.NextPlayer();
                break;

            case DiceGameMode.GameState.GameEnd:
                SetGameEndScreen();
                break;
        }
    }

    public void StartGame(bool isPlayerFirst)
    {
        gameMode.StartGame(isPlayerFirst);
        pageSwitcher.SetActivePage(gameHud);
    }

    public void RestartGame()
    {
        gameMode.ResetGame();
    }

    public void UpdateRound()
    {
        string roundMsg = "Round " + gameMode.currentRound;

        bool isWhite = true;
        bool isInGame = false;
        switch (gameMode.gameState)
        {
            case DiceGameMode.GameState.WhitePickingDice:
            case DiceGameMode.GameState.WhiteDiceRolling:
            case DiceGameMode.GameState.WhiteThrowSettled:
                isWhite = true;
                isInGame = true;                
                break;

            case DiceGameMode.GameState.BlackPickingDice:
            case DiceGameMode.GameState.BlackDiceRolling:
            case DiceGameMode.GameState.BlackThrowSettled:
                isWhite = false;
                isInGame = true;                
                break;
        }

        if (isInGame)
        {
            if (gameMode.IsPlayerTurn())
            {
                roundMsg += "\nplayer";
            }
            else
            {
                roundMsg += "\nAI";
            }
        }

        roundText.color = isWhite ? Color.white : Color.black;
        roundText.text = roundMsg;
    }

    public void UpdateScore()
    {
        playerScoreText.text = "Player: " + gameMode.GetPlayerScore();
        aiScoreText.text = "AI: " + gameMode.GetAIScore();
    }

    public void SetGameEndScreen()
    {
        pageSwitcher.SetActivePage(victoryScreen);

        bool playerVictory = false;
        bool draw = false;
        switch (gameMode.gameResult)
        {
            case DiceGameMode.GameResult.Draw:
                draw = true;
                break;

            case DiceGameMode.GameResult.WhiteVictory:
                playerVictory = gameMode.isPlayerFirst;
                break;

            case DiceGameMode.GameResult.BlackVictory:
                playerVictory = !gameMode.isPlayerFirst;
                break;
        }

        if (draw)
        {
            gameEndText.text = "Draw";
        }
        else if (playerVictory)
        {
            gameEndText.text = "Victory";
        }
        else
        {
            gameEndText.text = "Defeat";
        }
    }
}
