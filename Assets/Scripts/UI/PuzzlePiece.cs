using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public AudioClip Note;
    public int ClickOrder = 0;

    private AudioSource audSource;

    public void ClickedPuzzlePiece()
    {
        audSource.PlayOneShot(Note);
        Service.Puzzle.SetPiecePressed(ClickOrder);
    }

    // Start is called before the first frame update
    void Start()
    {
        audSource = Camera.main.gameObject.GetComponent<AudioSource>();
        Debug.Assert(audSource);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
