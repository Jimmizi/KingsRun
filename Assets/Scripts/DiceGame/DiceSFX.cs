using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class DiceSFX : MonoBehaviour
{
    [SerializeField]
    AudioClip[] shakeDiceClips;

    [SerializeField]
    AudioClip[] throwDiceClips;

    AudioSource audioSource;

    Vector3 previousDieVelocity;
    Vector3 previousAverageDiePosition;
    float recentSpeedSqr;


    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        Service.DiceGame.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnGameStateChanged(DiceGameMode.GameState newState, DiceGameMode.GameState oldState)
    {
        switch (Service.DiceGame.gameState)
        {
            case DiceGameMode.GameState.WhiteDiceRolling:
            case DiceGameMode.GameState.BlackDiceRolling:
                PlayDiceThrowSFX();
                break;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!Service.DiceGame)
        {
            return;
        }

        switch (Service.DiceGame.gameState)
        {
            case DiceGameMode.GameState.WhitePickingDice:
            case DiceGameMode.GameState.BlackPickingDice:
                PlayDiceShakeSFX();
                break;            
        }
    }

    void PlayDiceShakeSFX()
    {
        List<Pickup> pickedDice = Service.DiceGame.pickups;
        if (pickedDice.Count < 3)
        {
            return;
        }

        Vector3 averagePosition = Vector3.zero;
        for (int i = 0; i < pickedDice.Count; i++)
        {
            averagePosition += pickedDice[i].gameObject.transform.position;
        }

        averagePosition /= pickedDice.Count;
        
        Vector3 velocity = (averagePosition - previousAverageDiePosition) / Time.deltaTime;
        float diceMoveDot = Vector3.Dot(velocity.normalized, previousDieVelocity.normalized);

        if (diceMoveDot < 0.3f && velocity.sqrMagnitude > 1.0f)
        {
            if (!audioSource.isPlaying || audioSource.time > 0.5f)
            {
                int sfxIndex = Random.Range(0, shakeDiceClips.Length);
                transform.position = averagePosition;
                audioSource.clip = shakeDiceClips[sfxIndex];
                audioSource.Play();
            }
        }

        previousAverageDiePosition = averagePosition;
        previousDieVelocity = velocity;
    }

    void PlayDiceThrowSFX()
    {
        int sfxIndex = Random.Range(0, throwDiceClips.Length);

        audioSource.clip = throwDiceClips[sfxIndex];
        audioSource.PlayDelayed(0.05f);
    }
}
