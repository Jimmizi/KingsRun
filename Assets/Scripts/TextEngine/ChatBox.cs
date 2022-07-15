using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ChatBox : MonoBehaviour
{
    private const int MAX_CHOICES = 4;
    #region Public settings

    public Text SpeakerTextComponent;
    public Text SpeechTextComponent;

    public Text NextLineMarker;
    private Vector3 mLineMarkerStartPos;
    private float mLineMarkerTimer;

    public Button[] ChoiceButtons = new Button[MAX_CHOICES];

    public CanvasGroup AlphaGroup;
    public CanvasGroup ButtonAlphaGroup;

    /// <summary>
    /// How long to wait between appending characters to the speech box
    /// </summary>
    public float TimeBetweenCharacters = 0.1f;

    public float UnderscorePauseTime = 0.75f;
    public float CommaPauseTime = 0.25f;
    public float PeriodPauseTime = 0.5f;

    #endregion

    private float mConvEndOfLineSkipTimer = 0.0f;

    private ConversationData mConversationToDisplay;

    private ConversationData mCurrentConversationData = null;
    private ChoiceData mCurrentChoiceData = null;

    /// <summary>
    /// Is the chat box currently displaying text of some sort
    /// </summary>
    private bool mProcessingChat;

    private bool mNeedEndOfLineMarker;

    /// <summary>
    /// How long to pause for a underscore
    /// </summary>
    private float mUnderscorePauseTimer = 0.0f;

    /// <summary>
    /// If true, this is a conversation, if false this is a choice
    /// </summary>
    private bool mIsConversation;

    private bool mTypingLoopPlaying;

    public bool Processing => mProcessingChat;
    public bool HasValidChat => mCurrentConversationData != null || mCurrentChoiceData != null;

    #region Conversation Vars

    /// <summary>
    /// Current line of the conversation
    /// </summary>
    private int mCurrentConvLine;

    /// <summary>
    /// Next character to append in the conversation line
    /// </summary>
    private int mCurrentLineChar;

    /// <summary>
    /// Time before displaying the next character
    /// </summary>
    private float mCharacterTimer;

    #endregion

    #region Choice Vars

    private int mChoicePicked = -1;

    #endregion

    private bool mNextLinePressed;
    private bool mLineComplete;

    private void SetSpeakerName()
    {
        //SpeakerTextComponent.text = mCurrentConversationData.Lines[mCurrentConvLine].Speaker;

        //var names = Service.Party().MemberNames;
        //var colors = Service.Party().MemberColours;

        //int speakerIndex = -1;

        //for (var i = 0; i < names.Length; i++)
        //{
        //    if (SpeakerTextComponent.text.ToLower().Equals(names[i].ToLower()))
        //    {
        //        speakerIndex = i;
        //        SpeakerTextComponent.color = colors[i];
        //        break;
        //    }
        //}

        //if (speakerIndex > -1)
        //{
        //    //Service.Party().SetMemberAsSpeaker(speakerIndex);
        //}
        //else
        //{
        //    var namesOther = Service.Party().OtherNames;
        //    var colorsOther = Service.Party().OtherColours;
        //    bool foundAnOtherColour = false;

        //    for (var i = 0; i < namesOther.Count; i++)
        //    {
        //        if (SpeakerTextComponent.text.ToLower().Equals(namesOther[i].ToLower()))
        //        {
        //            foundAnOtherColour = true;
        //            SpeakerTextComponent.color = colorsOther[i];
        //            break;
        //        }
        //    }

        //    if (!foundAnOtherColour)
        //    {
        //        SpeakerTextComponent.color = Color.white;
        //    }

        //    //No speaker name
        //    //Service.Party().ResetSpeakerMembers();
        //}
    }

    /// <summary>
    /// Reset the conversation text box, and set the speaker for the current line about to be spoken
    /// </summary>
    private void ResetConversationToCurrentLine()
    {
        SetSpeakerName();
        SpeechTextComponent.text = "";
        mCurrentLineChar = 0;
        mLineComplete = false;
    }

    /// <summary>
    /// Choice / Conversation init
    /// </summary>
    private void SetReadyToStart()
    {
        mCurrentConvLine = 0;
        mCurrentLineChar = 0;
        mProcessingChat = true;

        NextLineMarker.color = new Color(NextLineMarker.color.r,
                                         NextLineMarker.color.g,
                                         NextLineMarker.color.b,
                                         0f);
    }

    /// <summary>
    /// Prepare the chat box for a conversation
    /// </summary>
    /// <param name="conv"></param>
    public void StartChat(ConversationData conv)
    {
        mIsConversation = true;
        mCurrentConversationData = conv;
        mCurrentChoiceData = null;

        SetButtons(false);
        StartCoroutine(FadeChatGroup(0.0f, 1.0f, 0.5f));

        SetReadyToStart();
        ResetConversationToCurrentLine();
    }

    /// <summary>
    /// Prepare the chat box for a choice option
    /// </summary>
    /// <param name="choice"></param>
    public void StartChat(ChoiceData choice)
    {
        mIsConversation = false;
        mCurrentChoiceData = choice;
        mCurrentConversationData = null;

        SpeechTextComponent.text = "";
        //SpeakerTextComponent.text = "";
        mChoicePicked = -1;

        SetButtons(false, true);

        for (var i = 0; i < choice.Choices.Count; i++)
        {
            var textComp = ChoiceButtons[i].GetComponentInChildren<Text>();
            if (textComp != null)
            {
                textComp.text = String.Format("[{0}] " + choice.Choices[i].Text, i+1);
            }
        }

        StartCoroutine(FadeChoiceGroup(0.0f, 1.0f, 0.5f));

        SetReadyToStart();
    }

    public void EndCleanup()
    {
        mCurrentConversationData = null;
        mCurrentChoiceData = null;
    }

    public void ChoicePressed(int choice)
    {
        if (mCurrentChoiceData?.Choices == null
            || mCurrentChoiceData.Choices.Count == 0)
        {
            return;
        }

        Debug.Log("Choice pressed: " + choice);
        if (mChoicePicked == -1 && choice < mCurrentChoiceData.Choices.Count)
        {
            Debug.Log("Choice locked in");
            mChoicePicked = choice;
            SetButtons(false);
        }
    }

    void EndChat()
    {
        mProcessingChat = false;

        if (mIsConversation)
        {
            StartCoroutine(FadeChatGroup(1.0f, 0.0f, 0.5f));
            //Service.Party().ResetSpeakerMembers();
            StopTypingSoundIfPossible();
        }
        else
        {
            StartCoroutine(FadeChoiceGroup(1.0f, 0.0f, 0.5f));
        }
    }

    void StartTypingSoundIfPossible()
    {
        if (!mTypingLoopPlaying)
        {
            //Service.Audio().PlayTypingLoop();
            mTypingLoopPlaying = true;
        }
    }

    void StopTypingSoundIfPossible()
    {
        if (mTypingLoopPlaying)
        {
           // Service.Audio().StopTypingLoop();
            mTypingLoopPlaying = false;
        }
    }

    // If we want to re-enable speaker names
    //string GetCurrentSpeech() => mCurrentConversationData.Lines[mCurrentConvLine].Speech;
    //int GetCurrentSpeechLength() => mCurrentConversationData.Lines[mCurrentConvLine].Speech.Length;

    string GetCurrentSpeech() => mCurrentConversationData.Lines[mCurrentConvLine];
    int GetCurrentSpeechLength() => mCurrentConversationData.Lines[mCurrentConvLine].Length;

    private void ProcessConversation()
    {
        //if we have a valid conversation line, proceed with displaying it
        if (mCurrentConvLine >= 0)
        {
            //Iterate and append text until we've added it all
            if (!mLineComplete)
            {
                if (mUnderscorePauseTimer > 0.0f && !Service.Config.IgnoreTextPauses)
                {
                    mUnderscorePauseTimer -= Time.deltaTime;
                    return;
                }

                //If delay time is reached, append, if not add to the timer
                if (mCharacterTimer >= TimeBetweenCharacters)
                {
                    StartTypingSoundIfPossible();

                    if (mCurrentLineChar < GetCurrentSpeechLength())
                    {
                        mCharacterTimer = 0.0f;
                        bool italics;
                        bool testItalics = mCurrentLineChar == 0;

                        var nextChar = GetCurrentSpeech().Substring(mCurrentLineChar++, 1);

                        if (testItalics)
                        {
                            if (GetCurrentSpeech().Substring(0, 3).Equals("[i]"))
                            {
                                SpeechTextComponent.fontStyle = FontStyle.Italic;
                                mCurrentLineChar = 3;
                                return;
                            }
                            else
                            {
                                SpeechTextComponent.fontStyle = FontStyle.Normal;
                            }
                        }

                        //Completely gets skipped
                        if (nextChar.Equals("_"))
                        {
                            StopTypingSoundIfPossible();
                            mUnderscorePauseTimer = UnderscorePauseTime;
                            return;
                        }
                        else
                        {
                            //These only add to the pause timer, also get added
                            if (nextChar.Equals(","))
                            {
                                mUnderscorePauseTimer = CommaPauseTime;
                            }
                            else if (nextChar.Equals("."))
                            {
                                StopTypingSoundIfPossible();
                                mUnderscorePauseTimer = PeriodPauseTime;

                                //if there are more characters
                                if (mCurrentLineChar + 1 < GetCurrentSpeechLength()
                                    && mCurrentLineChar > 0)
                                {
                                    //If there isn't a period in front or behind us, double the length of the pause
                                    //  don't want to make "..." super long
                                    if (!GetCurrentSpeech().Substring(mCurrentLineChar, 1).Equals(".")
                                    && !GetCurrentSpeech().Substring(mCurrentLineChar-1, 1).Equals("."))
                                    {
                                        mUnderscorePauseTimer += PeriodPauseTime;
                                    }
                                }
                            }
                            else if (nextChar.Equals("!") || nextChar.Equals("?"))
                            {
                                StopTypingSoundIfPossible();
                                mUnderscorePauseTimer = PeriodPauseTime * 2;
                            }

                            SpeechTextComponent.text += nextChar;
                        }
                    }
                }
                else
                {
                    mCharacterTimer += Time.deltaTime;
                }

                if (Service.Config.InstantText)
                {
                    SpeechTextComponent.text = GetCurrentSpeech();
                }

                mLineComplete = mCurrentLineChar >= GetCurrentSpeechLength();

                if (mLineComplete)
                {
                    StopTypingSoundIfPossible();
                    NextLineMarker.transform.position = mLineMarkerStartPos;
                    StartCoroutine(FadeLineMarkerGroup(0.0f, 1.0f, 0.5f));
                    mLineMarkerTimer = 0;
                }

                return;
            }
            //If we're done with appending text, wait until the player has pressed something to advance text
            else if (!Input.anyKey)
            {
                mNextLinePressed = false;
                mNeedEndOfLineMarker = true;

                if (!Service.Config.AutomaticEndOfLineSkip)
                {
                    return;
                }
                else
                {
                    if (mConvEndOfLineSkipTimer < 0.75f)
                    {
                        mConvEndOfLineSkipTimer += Time.deltaTime;
                        return;
                    }

                    mConvEndOfLineSkipTimer = 0;
                }
            }

            //Waiting for anyKey to be released and this set to false
            if (mNextLinePressed)
            {
                return;
            }

            mNextLinePressed = true;
            mNeedEndOfLineMarker = false;

            StartCoroutine(FadeLineMarkerGroup(1.0f, 0.0f, 0.25f));

            if (mCurrentConvLine < mCurrentConversationData.Lines.Count - 1)
            {
                //Move to the next line, and reset the current line
                mCurrentConvLine++;
                
                ResetConversationToCurrentLine();
            }
            else
            {
                //Once we're out of lines, set to -1 to begin processing end of conversation events
                mCurrentConvLine = -1;
            }
            
        }
        //Otherwise we are done with this conversation
        else
        {
            EndChat();
        }
    }

    void ProcessChoice()
    {
        if (mChoicePicked == -1)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (mCurrentChoiceData.Choices.Count >= 1)
                {
                    Debug.Log("Choice key 1 pressed.");
                    mChoicePicked = 0;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (mCurrentChoiceData.Choices.Count >= 2)
                {
                    Debug.Log("Choice key 2 pressed.");
                    mChoicePicked = 1;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (mCurrentChoiceData.Choices.Count >= 3)
                {
                    Debug.Log("Choice key 3 pressed.");
                    mChoicePicked = 2;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                if (mCurrentChoiceData.Choices.Count >= 4)
                {
                    Debug.Log("Choice key 4 pressed.");
                    mChoicePicked = 3;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                if (mCurrentChoiceData.Choices.Count >= 5)
                {
                    Debug.Log("Choice key 5 pressed.");
                    mChoicePicked = 4;
                }
            }
        }
        else
        {
            mCurrentChoiceData.ChoiceTaken = mChoicePicked;
            EndChat();
        }
    }

    void Awake()
    {
        Service.Text = this;
        AlphaGroup.alpha = 0;
        ButtonAlphaGroup.alpha = 0;
        mLineMarkerStartPos = NextLineMarker.transform.position;
    }

    void Start()
    {

    }
    
    void Update()
    {
        if (mProcessingChat)
        {
            if (mIsConversation)
            {
                ProcessConversation();
            }
            else
            {
                ProcessChoice();
            }

            if (mNeedEndOfLineMarker)
            {
                if (mLineMarkerTimer >= 0.60f)
                {
                    mLineMarkerTimer = 0.0f;
                    if (Math.Abs(NextLineMarker.transform.position.y - mLineMarkerStartPos.y) < 0.01f)
                    {
                        NextLineMarker.transform.position = mLineMarkerStartPos + new Vector3(0, 10, 0);
                    }
                    else
                    {
                        NextLineMarker.transform.position = mLineMarkerStartPos;
                    }
                }

                mLineMarkerTimer += Time.deltaTime;
            }
        }
    }

    private void SetButtons(bool interactable, bool resetText = false)
    {
        int currentChoices = 0;

        if (mCurrentChoiceData?.Choices != null)
        {
            currentChoices = mCurrentChoiceData.Choices.Count;
        }

        for (var i = 0; i < ChoiceButtons.Length; i++)
        {
            var button = ChoiceButtons[i];
            button.interactable = interactable && i < currentChoices;

            //If this is going to be interactive
            button.gameObject.SetActive(i < currentChoices);
           
            if (resetText)
            {
                var textComp = button.GetComponentInChildren<Text>();
                if (textComp != null)
                {
                    textComp.text = "-";
                }
            }
        }
    }

    public IEnumerator FadeChatGroup(float startAlpha, float endAlpha, float duration)
    {
        if (Math.Abs(AlphaGroup.alpha - endAlpha) > 0.01f)
        {
            float elapsedTime = 0f;
            float totalDuration = duration;

            while (elapsedTime < totalDuration)
            {
                elapsedTime += Time.deltaTime;
                float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / totalDuration);

                AlphaGroup.alpha = currentAlpha;

                yield return null;
            }
        }
    }
    public IEnumerator FadeChoiceGroup(float startAlpha, float endAlpha, float duration)
    {
        if (Math.Abs(ButtonAlphaGroup.alpha - endAlpha) > 0.01f)
        {
            float elapsedTime = 0f;
            float totalDuration = duration;

            while (elapsedTime < totalDuration)
            {
                elapsedTime += Time.deltaTime;
                float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / totalDuration);

                ButtonAlphaGroup.alpha = currentAlpha;

                yield return null;
            }

            SetButtons(true);
        }
    }
    public IEnumerator FadeLineMarkerGroup(float startAlpha, float endAlpha, float duration)
    {
        if (Math.Abs(NextLineMarker.color.a - endAlpha) > 0.01f)
        {
            float elapsedTime = 0f;
            float totalDuration = duration;

            while (elapsedTime < totalDuration)
            {
                elapsedTime += Time.deltaTime;
                float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / totalDuration);

                NextLineMarker.color = new Color(NextLineMarker.color.r,
                                                 NextLineMarker.color.g,
                                                 NextLineMarker.color.b, 
                                                 currentAlpha);

                yield return null;
            }
        }
    }
}
