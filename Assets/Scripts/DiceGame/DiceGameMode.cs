using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceGameMode : MonoBehaviour
{
    const int deadFace = 4;

    public enum GameResult
    {
        None,
        Draw,
        WhiteVictory,
        BlackVictory
    }

    public enum GameState
    {
        WaitingToStart,
        WhitePickingDice,
        WhiteDiceRolling,
        WhiteThrowSettled,
        WhiteDiceDestruction,
        BlackPickingDice,
        BlackDiceRolling,
        BlackThrowSettled,
        BlackDiceDestruction,
        GameEnd
    }

    class DieMetaData
    {
        public float movement;
        public int result;
        public Die die;
        public bool isPlayer;
        public Vector3 initialPosition;
        public Quaternion initialRotation;
    };

    class RigParameters
    {
        public int minScoreDelta = -50;
        public int maxScoreDelta = +50;

        public int minPlayerScore = 0;
        public int maxPlayerScore = 50;

        public int minAiScore = 0;
        public int maxAiScore = 50;
    };

    [SerializeField]
    Die[] playerDice;

    [SerializeField]
    Die[] aiDice;

    [SerializeField]
    Material whiteDieMaterial;

    [SerializeField]
    Material blackDieMaterial;

    [SerializeField]
    GameObject dieKillParticles;

    [SerializeField]
    float settleTime = 2.0f;
    float settleTimer = 2.0f;

    PlayerController playerController;
    AIController aiController;

    GameObject[] rollingDice = null;
    List<Die> allDice = new List<Die>();
    Dictionary<Die, Rigidbody> diceRigidBodies = new Dictionary<Die,Rigidbody>();
    Dictionary<Die, DieMetaData> diceMetaData = new Dictionary<Die, DieMetaData>();

    bool pendingPlayerAutoAdvance = false;

    public delegate void OnGameStateChangedHandler(GameState newState, GameState oldState);
    public event OnGameStateChangedHandler OnGameStateChanged;

    public int numRounds = 3;
    public int currentRound = 0;

    public bool isPlayerFirst = false;
    public bool autoStartGame = false;
    public bool autoAdvancePlayers = false;
    public GameState gameState = GameState.WaitingToStart;
    public GameResult gameResult = GameResult.None;

    DiceRollRigger rigger;

    private void Awake()
    {
        Service.DiceGame = this;

        rigger = GetComponent<DiceRollRigger>();
        playerController = FindObjectOfType<PlayerController>();
        aiController = FindObjectOfType<AIController>();
    }
    
    void Start()
    {        
        foreach (Die playerDie in playerDice)
        {
            playerController.picker.validPickups.Add(playerDie.gameObject);
            allDice.Add(playerDie);

            diceMetaData[playerDie] = new DieMetaData();            
            diceMetaData[playerDie].isPlayer = true;
        }

        foreach (Die aiDie in aiDice)
        {
            allDice.Add(aiDie);

            diceMetaData[aiDie] = new DieMetaData();
            diceMetaData[aiDie].isPlayer = false;
        }

        foreach (Die die in allDice)
        {
            diceRigidBodies[die] = die.GetComponent<Rigidbody>();

            diceMetaData[die].die = die;
            diceMetaData[die].initialPosition = die.transform.position;
            diceMetaData[die].initialRotation = die.transform.rotation;

            die.gameObject.SetActive(false);
        }

        playerController.picker.OnObjectsThrown += OnPlayerDiceRolled;
        aiController.picker.OnObjectsThrown += OnPlayerDiceRolled;

        ResetGame();

        if (autoStartGame)
        {
            StartGame(isPlayerFirst);
        }
    }

    public void ResetGame()
    {
        currentRound = 0;
        gameResult = GameResult.None;

        foreach (DieMetaData meta in diceMetaData.Values)
        {
            meta.die.transform.position = meta.initialPosition;
            meta.die.transform.rotation = meta.initialRotation;
            meta.die.gameObject.SetActive(false);
        }

        SetGameState(GameState.WaitingToStart);
    }

    public void StartGame(bool playerFirst)
    {
        currentRound = 1;
        isPlayerFirst = playerFirst;

        foreach (Die die in playerDice)
        {
            var dieRenderer = die.GetComponent<Renderer>();
            dieRenderer.sharedMaterial = playerFirst ? whiteDieMaterial : blackDieMaterial;
        }

        foreach (Die die in aiDice)
        {
            var dieRenderer = die.GetComponent<Renderer>();
            dieRenderer.sharedMaterial = playerFirst ? blackDieMaterial : whiteDieMaterial;            
        }

        foreach (Die die in allDice)
        {
            die.gameObject.SetActive(true);
            die.value = deadFace;
        }

        SetGameState(GameState.WhitePickingDice);
    }

    public void PresetGameWithRigParameters(int gameType, float intensity)
    {

    }

    public bool NextPlayer()
    {
        switch (gameState)
        {
            case GameState.WhiteThrowSettled:
                SetGameState(GameState.BlackPickingDice);
                return true;

            case GameState.BlackThrowSettled:
                SetGameState(GameState.WhitePickingDice);
                return true;
        }

        return false;
    }

    void SetGameState(GameState newState)
    {        
        if (gameState != newState)
        {
            GameState oldState = gameState;
            gameState = newState;
            switch(gameState)
            {
                case GameState.WhitePickingDice:
                    if (isPlayerFirst) HandlePlayerPickStart(); else HandleAIPickStart(); 
                    break;
                case GameState.BlackPickingDice:
                    if (!isPlayerFirst) HandlePlayerPickStart(); else HandleAIPickStart();
                    break;

                case GameState.WhiteDiceRolling:
                case GameState.BlackDiceRolling:
                    HandleDiceStartedRolling();
                    break;

                case GameState.WhiteDiceDestruction:
                case GameState.BlackDiceDestruction:
                    HandleDiceDestruction();
                    break;

                case GameState.WhiteThrowSettled:
                case GameState.BlackThrowSettled:
                    HandleDiceThrownResolved();
                    break;

                case GameState.GameEnd:
                    HandleGameEnd();
                    break;
            }

            if (OnGameStateChanged != null)
            {
                OnGameStateChanged(newState, oldState);
            }
        }

        if (pendingPlayerAutoAdvance)
        {
            pendingPlayerAutoAdvance = false;
            NextPlayer();
        }
    }

    public bool IsPlayerTurn()
    {
        switch(gameState)
        {
            case GameState.WhitePickingDice:
            case GameState.WhiteDiceRolling:
            case GameState.WhiteDiceDestruction:
            case GameState.WhiteThrowSettled:
                return isPlayerFirst;
            case GameState.BlackPickingDice:
            case GameState.BlackDiceRolling:
            case GameState.BlackDiceDestruction:
            case GameState.BlackThrowSettled:
                return !isPlayerFirst;
        }
        return false;
    }

    public int GetPlayerScore()
    {
        return GetDieTotal(playerDice);
    }

    public int GetAIScore()
    {
        return GetDieTotal(aiDice);
    }

    void HandlePlayerPickStart()
    {
        playerController.picker.pickUpEnabled = true;
        playerController.picker.pickAll = (currentRound == 1);
    }

    void HandleAIPickStart()
    {
        List<Die> diePickList = new List<Die>();

        List<Die> shuffledAiDice = new List<Die>(aiDice);
        ShuffleDieList(shuffledAiDice);

        foreach (Die die in shuffledAiDice)
        {
            if (!die.gameObject.activeInHierarchy)
                continue;

            if (die.value == deadFace)
            {
                diePickList.Add(die);
            }
            else if (die.value == 1 || die.value == 2)
            {
                diePickList.Add(die);
            }
            else if (Random.value > die.value / 6.5f)
            {
                diePickList.Add(die);
            }
        }

        if (diePickList.Count == 0)
        {
            diePickList.Add(shuffledAiDice[0]);
        }

        aiController.PickupDice(diePickList.ToArray(), currentRound == 1);
    }

    void HandleDiceStartedRolling()
    {
        playerController.picker.pickUpEnabled = false;
        StartCoroutine(ThrowSim(rollingDice));
    }

    void HandleDiceDestruction()
    {
        StartCoroutine(DiceDestructionSequence());
    }

    void HandleDiceThrownResolved()
    {
        if (gameState == GameState.BlackThrowSettled)
        {
            if (currentRound < numRounds)
            {
                currentRound++;
                if (autoAdvancePlayers)
                {
                    pendingPlayerAutoAdvance = true;
                }
            }
            else
            {
                SetGameState(GameState.GameEnd);
            }
        } 
        else if (gameState == GameState.WhiteThrowSettled)
        {
            if (autoAdvancePlayers)
            {
                pendingPlayerAutoAdvance = true;
            }
        }
    }

    void HandleGameEnd()
    {
        DetermineWinner();
    }

    void DetermineWinner()
    {
        int whiteDieTotal = GetDieTotal(isPlayerFirst ? playerDice : aiDice);
        int blackDieTotal = GetDieTotal(isPlayerFirst ? aiDice : playerDice);

        if (whiteDieTotal > blackDieTotal)
        {
            gameResult = GameResult.WhiteVictory;
        }
        else if (blackDieTotal > whiteDieTotal)
        {
            gameResult = GameResult.BlackVictory;
        }
        else
        {
            gameResult = GameResult.Draw;
        }
    }

    int GetDieTotal(Die[] dice)
    {        
        int totalDie = 0;
        foreach (Die die in dice)
        {
            if (die.value != deadFace)
            {
                totalDie += die.value;
            }
        }
        return totalDie;
    }


    void FixedUpdate()
    {
        if (gameState == GameState.WhiteDiceRolling
            || gameState == GameState.BlackDiceRolling)
        {
            UpdateRollingDice();
        }
    }

    void UpdateRollingDice()
    {
        float movementDelta = 0.001f;

        if (rigger.isSimulating)
        {
            return;
        }

        foreach (Die die in allDice)
        {
            Rigidbody dieRigidBody = diceRigidBodies[die];
            if (dieRigidBody 
                && dieRigidBody.velocity.sqrMagnitude > movementDelta
                && dieRigidBody.angularVelocity.sqrMagnitude > movementDelta)
            {
                settleTimer = settleTime;
                return;
            }
        }

        settleTimer -= Time.fixedDeltaTime;
        if (settleTimer < 0)
        {
            if (gameState == GameState.WhiteDiceRolling)
            {
                SetGameState(GameState.WhiteDiceDestruction);
            }
            else if (gameState == GameState.BlackDiceRolling)
            {
                SetGameState(GameState.BlackDiceDestruction);
            }
        }
    }

    private void OnPlayerDiceRolled(GameObject[] thrownObjects)
    {
        rollingDice = thrownObjects;
        if (rollingDice == null || rollingDice.Length == 0)
        {
            // Nothing thrown, try again
            return;
        }

        if (gameState == GameState.WhitePickingDice)
        {
            SetGameState(GameState.WhiteDiceRolling);
        }
        else if (gameState == GameState.BlackPickingDice)
        {
            SetGameState(GameState.BlackDiceRolling);
        }
    }

    IEnumerator ThrowSim(GameObject[] thrownObjects)
    {
        yield return new WaitForSeconds(Time.fixedDeltaTime * 10);

        rigger.SimulateThrow(allDice.ToArray());

        RigParameters rig = new RigParameters();
        rig.minPlayerScore = 1;
        rig.maxPlayerScore = 10;
        
        rig.minAiScore = 20;
        rig.maxAiScore = 40;

        RigResults(rig);
    }

    IEnumerator DiceDestructionSequence()
    {        
        bool preventBlackDestruction = currentRound == 1 && gameState == GameState.WhiteDiceDestruction;
        List<Die> blackDice = new List<Die>(isPlayerFirst ? aiDice : playerDice);

        List<Die> shuffledDice = new List<Die>(allDice);
        ShuffleDieList(shuffledDice);
        foreach (Die die in shuffledDice)
        {
            if (die.value == deadFace && die.gameObject.activeInHierarchy)
            {
                if (preventBlackDestruction && blackDice.Contains(die))
                {
                    continue;
                }

                DestroyDie(die);
                yield return new WaitForSeconds(0.4f);
            }
        }

        if (gameState == GameState.WhiteDiceDestruction)
        {
            SetGameState(GameState.WhiteThrowSettled);
        }
        else if (gameState == GameState.BlackDiceDestruction)
        {
            SetGameState(GameState.BlackThrowSettled);
        }
    }

    void DestroyDie(Die die)
    {
        die.gameObject.SetActive(false);
        if (dieKillParticles)
        {
            Instantiate(dieKillParticles, die.transform.position, Quaternion.identity);
        }
    }

    void ShuffleDieList(List<Die> diceList)
    {        
        for (int i = 0; i < diceList.Count; i++)
        {
            int j = Random.Range(0, diceList.Count - 1);
            Die tempDie = diceList[i];
            diceList[i] = diceList[j];
            diceList[j] = tempDie;
        }
    }

    void RigResults(RigParameters rigParams)
    {
        var sortedDice = new List<Die>(allDice.Count);
        int playerTotal = 0;
        int aiTotal = 0;

        foreach (Die die in allDice)
        {
            diceMetaData[die].movement = rigger.GetPredictedTotalMovement(die);
            diceMetaData[die].result = rigger.GetPredictedResult(die);

            if (diceMetaData[die].result != deadFace)
            {
                if (diceMetaData[die].isPlayer)
                {
                    playerTotal += diceMetaData[die].result;
                }
                else
                {
                    aiTotal += diceMetaData[die].result;
                }
            }

            if (diceMetaData[die].movement > 300)
            {
                sortedDice.Add(die);
            }
        }
        
        int deltaScore = playerTotal - aiTotal;
        int targetDeltaScore = Mathf.Clamp(deltaScore, rigParams.minScoreDelta, rigParams.maxScoreDelta);
        int deltaAdjustment = targetDeltaScore - deltaScore;

        int targetPlayerScore = Mathf.Clamp(playerTotal, rigParams.minPlayerScore, rigParams.maxPlayerScore);
        int playerScoreAdjustment = targetPlayerScore - playerTotal;

        int targetAiScore = Mathf.Clamp(aiTotal, rigParams.minAiScore, rigParams.maxAiScore);
        int aiScoreAdjustment = targetAiScore - aiTotal;

        sortedDice.Sort((a, b) => diceMetaData[b].movement.CompareTo(diceMetaData[a].movement));
        foreach (Die die in sortedDice)
        {
            bool isPlayer = diceMetaData[die].isPlayer;
            int direction = isPlayer ? 1 : -1;
            int targetResult = 0;

            if (isPlayer)
            {
                int dieDeltaAdjustment = Mathf.Clamp(diceMetaData[die].result + deltaAdjustment, 0, 6);
                int diePlayerAdjustment = Mathf.Clamp(diceMetaData[die].result + playerScoreAdjustment, 0, 6);
                targetResult = Mathf.Min(dieDeltaAdjustment, diePlayerAdjustment);
            }
            else
            {
                int dieDeltaAdjustment = Mathf.Clamp(diceMetaData[die].result - deltaAdjustment, 0, 6);
                int dieAiAdjustment = Mathf.Clamp(diceMetaData[die].result + aiScoreAdjustment, 0, 6);
                targetResult = Mathf.Min(dieDeltaAdjustment, dieAiAdjustment);
            }

            if (targetResult == deadFace)
            {
                --targetResult;
            }

            int targetSide = targetResult;

            if (targetResult == 0)
            {
                targetSide = deadFace;
            }

            if (diceMetaData[die].result != targetResult)
            {
                rigger.RigDieResult(die, targetSide);

                int dieAdjustment = diceMetaData[die].result - targetResult;                
                diceMetaData[die].result = targetResult;

                if (isPlayer)
                {
                    deltaAdjustment += dieAdjustment;
                    playerScoreAdjustment += dieAdjustment;
                }
                else
                {
                    deltaAdjustment -= dieAdjustment;
                    aiScoreAdjustment += dieAdjustment;
                }
            }
        }
    }
}
