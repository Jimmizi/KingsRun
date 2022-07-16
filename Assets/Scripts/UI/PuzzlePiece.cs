using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public AudioClip Note;
    public int ClickOrder = 0;
    
    public void ClickedPuzzlePiece()
    {
        Service.Flow.PuzzleAudioSource.PlayOneShot(Note);
        Service.Puzzle.SetPiecePressed(ClickOrder);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
