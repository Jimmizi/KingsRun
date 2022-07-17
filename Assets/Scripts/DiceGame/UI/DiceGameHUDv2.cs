using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class DiceGameHUDv2 : MonoBehaviour
{
    PageSwitcher pageSwitcher;

    [SerializeField]
    GameObject colorSelect;

    [SerializeField]
    GameObject gameHud;

    [SerializeField]
    GameObject victoryScreen;

    [SerializeField]
    TextMeshProUGUI playerScoreText;

    [SerializeField]
    TextMeshProUGUI aiScoreText;

    [SerializeField]
    TextMeshProUGUI gameEndText;

    [SerializeField]
    RoundCounterWidget roundCounter;

    int lastPlayerScore = 0;
    int lastAIScore = 0;

    private void Awake()
    {
        Service.DiceGameHUD = this;
        pageSwitcher = GetComponent<PageSwitcher>();
    }

    public void StartGame(bool isPlayerFirst)
    {
        Service.DiceGame.StartGame(isPlayerFirst);
        Service.DiceGame.autoAdvancePlayers = true;

        pageSwitcher.SetActivePage(gameHud);
        UpdateScore();
    }

    public void ShowColorSelection()
    {
        pageSwitcher.SetActivePage(colorSelect);
    }

    public void HideAll()
    {
        pageSwitcher.HideAllPages();
    }

    // Start is called before the first frame update
    void Start()
    {
        Service.DiceGame.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnGameStateChanged(DiceGameMode.GameState newState, DiceGameMode.GameState oldState)
    {
        switch (newState)
        {
            case DiceGameMode.GameState.WhitePickingDice:
                UpdateTurnCounter(0);
                break;

            case DiceGameMode.GameState.BlackPickingDice:
                UpdateTurnCounter(1);
                break;

            case DiceGameMode.GameState.WhiteThrowSettled:
            case DiceGameMode.GameState.BlackThrowSettled:
                UpdateScore();
                CheckQuip();
                break;

            case DiceGameMode.GameState.GameEnd:
                EndGame();
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateTurnCounter(int turn)
    {
        if (roundCounter)
        {
            roundCounter.activeTurn = 1 + turn + (Service.DiceGame.currentRound - 1 ) * 2;
        }
    }

    public void UpdateScore()
    {
        playerScoreText.text = "" + Service.DiceGame.GetPlayerScore();
        aiScoreText.text = "" + Service.DiceGame.GetAIScore();

        bool playerWhite = Service.DiceGame.isPlayerFirst;

        playerScoreText.color = playerWhite ? Color.white : Color.black;
        aiScoreText.color = playerWhite ? Color.black : Color.white;
    }

    public void CheckQuip()
    {
        int currentPlayerScore = Service.DiceGame.GetPlayerScore();
        int currentAIScore = Service.DiceGame.GetAIScore();

        if (currentPlayerScore > lastPlayerScore)
        {
            Service.Text.TryPlayRandomDiceRollQuip(ChatBox.QuipType.Positive);
        }
        else if (currentPlayerScore < currentAIScore)
        {
            Service.Text.TryPlayRandomDiceRollQuip(ChatBox.QuipType.Negative);
        }

        lastPlayerScore = currentPlayerScore;
        lastAIScore = currentAIScore;
    }

    public void EndGame()
    {
        StartCoroutine(EndGameRoutine());
    }

    IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        ShowEndGameScreen();
        yield return new WaitForSeconds(4f);

        Service.Text.ResumeFromDiceGame();
        Service.DiceGame.ResetGame();
        yield return new WaitForSeconds(2f);

        pageSwitcher.HideAllPages();
    }

    void ShowEndGameScreen()
    {
        pageSwitcher.SetActivePage(victoryScreen);

        bool playerVictory = false;
        bool draw = false;
        switch (Service.DiceGame.gameResult)
        {
            case DiceGameMode.GameResult.Draw:
                draw = true;
                break;

            case DiceGameMode.GameResult.WhiteVictory:
                playerVictory = Service.DiceGame.isPlayerFirst;
                break;

            case DiceGameMode.GameResult.BlackVictory:
                playerVictory = !Service.DiceGame.isPlayerFirst;
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
