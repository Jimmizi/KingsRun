using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetPuzzleManager : MonoBehaviour
{
    public GameObject ThanksForPlayingGo;
    public GameObject LastPuzzleGo;

    public List<PuzzlePiece> PuzzlePieces = new List<PuzzlePiece>();
    public AudioClip NoteToPlayOnCompletion;

    public bool PuzzleDone = false;

    public int NextPieceNeeded = 0;
    private int numSuccessfulPressed = 0;

    private AudioSource audSource;

    public void SetPiecePressed(int pieceOrder)
    {
        if (numSuccessfulPressed < PuzzlePieces.Count)
        {
            if (NextPieceNeeded == pieceOrder)
            {
                NextPieceNeeded++;
                numSuccessfulPressed++;
            }
            else
            {
                NextPieceNeeded = 0;
                numSuccessfulPressed = 0;
            }
        }
    }

    void Awake()
    {
        Service.Puzzle = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        ThanksForPlayingGo.SetActive(false);

        audSource = Camera.main.gameObject.GetComponent<AudioSource>();
        Debug.Assert(audSource);
    }

    // Update is called once per frame
    void Update()
    {
        if (!PuzzleDone && numSuccessfulPressed >= PuzzlePieces.Count)
        {
            audSource.PlayOneShot(NoteToPlayOnCompletion);
            PuzzleDone = true;

            StartCoroutine(DelayReset());
        }
    }

    IEnumerator DelayReset()
    {
        numSuccessfulPressed = 0;
        PuzzleDone = false;

        yield return new WaitForSeconds(2.0f);

        ThanksForPlayingGo.SetActive(true);
        LastPuzzleGo.SetActive(false);

        yield return new WaitForSeconds(6.0f);
        
        ThanksForPlayingGo.SetActive(false);
        
        Service.Data.DeleteAllData();
        Service.UI.ResetMenu();
    }
}
