using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ChatBox : MonoBehaviour
{
    private const int MAX_CHOICES = 4;
    #region Public settings

    public AudioClip TextDisplayClip;

    public Text SpeakerTextComponent;
    public Text SpeechTextComponent;
    public TMP_Text SpeechTextProComponent;

    public CanvasGroup NextLineMarkerGroup;
    public GameObject NextLineMarker;
    private Vector3 mLineMarkerStartPos;
    private float mLineMarkerTimer;

    public Button[] ChoiceButtons = new Button[MAX_CHOICES];
    public Button NextLineButton;

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

    private ConversationData conversationToGoBackToAfterInterrupt = null;

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

    public bool PausePlayback;

    public bool Processing
    {
        get => mProcessingChat;
        private set => mProcessingChat = value;
    }
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

    private int interruptedLineChar;
    private int interruptedConvLine;

    /// <summary>
    /// Time before displaying the next character
    /// </summary>
    private float mCharacterTimer;

    private int speedUpToChar = 0;
    private bool autoSkipEndOfLine = false;

    #endregion

    #region Choice Vars

    private int mChoicePicked = -1;

    #endregion

    private bool mNextLinePressed;
    private bool mLineComplete;

    public bool nextPressed;

    public void GoToNext()
    {
        nextPressed = true;
        NextLineButton.interactable = false;
    }

    void SetText(string text)
    {
        SpeechTextComponent.text = text;
        SpeechTextProComponent.text = text;
    }
    public void AddText(string text)
    {
        SpeechTextComponent.text += text;
        SpeechTextProComponent.text += text;
    }

    void SetItalics(bool b)
    {
        SpeechTextComponent.fontStyle = b ? FontStyle.Italic : FontStyle.Normal;
        SpeechTextProComponent.fontStyle = b ? FontStyles.Italic : FontStyles.Normal;
    }

    /// <summary>
    /// Reset the conversation text box, and set the speaker for the current line about to be spoken
    /// </summary>
    private void ResetConversationToCurrentLine()
    {
        SetText("");
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

        NextLineMarkerGroup.alpha = 0.0f;
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

    public void StartOrInterruptChat(ConversationData newConv)
    {
        if (mCurrentConversationData == null)
        {
            StartChat(newConv);
        }
        else if(conversationToGoBackToAfterInterrupt == null)
        {
            conversationToGoBackToAfterInterrupt = mCurrentConversationData;
            interruptedLineChar = mCurrentLineChar;
            interruptedConvLine = mCurrentConvLine;
            StartChat(newConv);
        }
        else // Don't go back to interrupting text if also interrupting that
        {
            StartChat(newConv);
        }
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

        SetText("");
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
        conversationToGoBackToAfterInterrupt = null;
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
            if (conversationToGoBackToAfterInterrupt != null)
            {
                // Manually fire off events
                foreach (var eventName in mCurrentConversationData.EventsToFire)
                {
                    var eventFile = (TextAsset)Resources.Load("Dialogue/" + eventName, typeof(TextAsset));

                    Debug.AssertFormat(eventFile != null, $"Failed to load in event resource file: {eventName}");

                    EventData evt = JsonDataExecuter.MakeEvent(eventFile);
                    if (evt != null)
                    {
                        JsonDataExecuter.ProcessEvent(evt, true);
                    }
                }

                StartChat(conversationToGoBackToAfterInterrupt);

                conversationToGoBackToAfterInterrupt = null;

                speedUpToChar = interruptedLineChar;
                mCurrentConvLine = interruptedConvLine;
            }
            else
            {
                StartCoroutine(FadeChatGroup(1.0f, 0.0f, 0.5f));
                StopTypingSoundIfPossible();
            }
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
            Service.Flow.TextBoxAudioSource.Play();
            //Service.Audio().PlayTypingLoop();
            mTypingLoopPlaying = true;
        }
    }

    void StopTypingSoundIfPossible()
    {
        if (mTypingLoopPlaying)
        {
            Service.Flow.TextBoxAudioSource.Stop();
            // Service.Audio().StopTypingLoop();
            mTypingLoopPlaying = false;
        }
    }
    
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
                bool bReachedSpeedUpChar = (speedUpToChar == 0 || speedUpToChar <= mCurrentLineChar);

                // no pausing until reached speed up point
                if (bReachedSpeedUpChar && mUnderscorePauseTimer > 0.0f && !Service.Config.IgnoreTextPauses)
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
                                SetItalics(true);
                                //SpeechTextComponent.fontStyle = FontStyle.Italic;
                                mCurrentLineChar = 3;
                                return;
                            }
                            else
                            {
                                SetItalics(false);
                                //SpeechTextComponent.fontStyle = FontStyle.Normal;
                            }
                        }

                        if (nextChar.Equals("^"))
                        {
                            autoSkipEndOfLine = true;
                            return;
                        }
                        //Completely gets skipped
                        else if (nextChar.Equals("_"))
                        {
                            StopTypingSoundIfPossible();

                            if (bReachedSpeedUpChar)
                            {
                                mUnderscorePauseTimer = UnderscorePauseTime;
                            }

                            return;
                        }
                        else
                        {
                            //These only add to the pause timer, also get added
                            if (nextChar.Equals(","))
                            {
                                if(bReachedSpeedUpChar)
                                    mUnderscorePauseTimer = CommaPauseTime;
                            }
                            else if (nextChar.Equals("."))
                            {
                                StopTypingSoundIfPossible();

                                if (bReachedSpeedUpChar)
                                {
                                    mUnderscorePauseTimer = PeriodPauseTime;

                                    //if there are more characters
                                    if (mCurrentLineChar + 1 < GetCurrentSpeechLength()
                                        && mCurrentLineChar > 0)
                                    {
                                        //If there isn't a period in front or behind us, double the length of the pause
                                        //  don't want to make "..." super long
                                        if (!GetCurrentSpeech().Substring(mCurrentLineChar, 1).Equals(".")
                                            && !GetCurrentSpeech().Substring(mCurrentLineChar - 1, 1).Equals("."))
                                        {
                                            mUnderscorePauseTimer += PeriodPauseTime;
                                        }
                                    }
                                }
                            }
                            else if (nextChar.Equals("!") || nextChar.Equals("?"))
                            {
                                StopTypingSoundIfPossible();

                                if(bReachedSpeedUpChar)
                                    mUnderscorePauseTimer = PeriodPauseTime * 2;
                            }

                            AddText(nextChar);
                            //SpeechTextComponent.text += nextChar;
                        }
                    }
                }
                else
                {
                    mCharacterTimer += Time.deltaTime * (bReachedSpeedUpChar ? 1.0f : 3.0f);
                }

                if (Service.Config.InstantText)
                {
                    SetText(GetCurrentSpeech());
                }

                mLineComplete = mCurrentLineChar >= GetCurrentSpeechLength();

                if (mLineComplete)
                {
                    StopTypingSoundIfPossible();

                    if (!autoSkipEndOfLine)
                    {
                        NextLineMarker.transform.position = mLineMarkerStartPos;
                        StartCoroutine(FadeLineMarkerGroup(0.0f, 1.0f, 0.5f));
                    }

                    mLineMarkerTimer = 0;

                    if (!mCurrentConversationData.MoveQuitButtonAtStartOfLine)
                    {
                        if (mCurrentConversationData.QuitButtonInterruptLineToResetButton == mCurrentConvLine)
                        {
                            Service.QuitButtonObj.ResetPosition(mCurrentConversationData.QuitButtonMoveSpeedOverride);
                        }
                    }
                }

                return;
            }
            //If we're done with appending text, wait until the player has pressed something to advance text
            else if (!nextPressed)
            {
                mNeedEndOfLineMarker = true;

                if (!Service.Config.AutomaticEndOfLineSkip && !autoSkipEndOfLine)
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
            
            nextPressed = false;
            mNextLinePressed = true;
            mNeedEndOfLineMarker = false;
            autoSkipEndOfLine = false;
            speedUpToChar = 0;

            StartCoroutine(FadeLineMarkerGroup(1.0f, 0.0f, 0.25f));

            if (mCurrentConvLine < mCurrentConversationData.Lines.Count - 1)
            {
                //Move to the next line, and reset the current line
                mCurrentConvLine++;
                
                ResetConversationToCurrentLine();

                if (mCurrentConversationData.MoveQuitButtonAtStartOfLine)
                {
                    if (mCurrentConversationData.QuitButtonInterruptLineToResetButton == mCurrentConvLine)
                    {
                        Service.QuitButtonObj.ResetPosition(mCurrentConversationData.QuitButtonMoveSpeedOverride);
                    }
                }
            }
            else
            {
                if (mCurrentConversationData.DataToSetAfterLines.Length > 0)
                {
                    Service.Data.TrySetData(mCurrentConversationData.DataToSetAfterLines, mCurrentConversationData.DataValue);
                }

                if (mCurrentConversationData.QuitButtonInterruptLineToResetButton != -1)
                {
                    Debug.Log("Setting quit button active after conversation");
                    Service.QuitButtonObj.SetButtonActive(true);
                }

                if (mCurrentConversationData.QuitGameAfterConversationFinish)
                {
                    Debug.Log("Qutting game after conversation");
                    Service.Flow.QuitGameToTitle();
                    conversationToGoBackToAfterInterrupt = null;
                    EndChat();
                    return;
                }

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
        NextLineMarkerGroup.alpha = 0;
        mLineMarkerStartPos = NextLineMarker.transform.position;
    }

    void Start()
    {
        Service.Flow.TextBoxAudioSource.clip = TextDisplayClip;
        Service.Flow.TextBoxAudioSource.loop = true;

        NextLineButton = NextLineMarker.GetComponent<Button>();
        Debug.Assert(NextLineButton != null);

        NextLineButton.interactable = false;
    }
    
    void Update()
    {
        if (mProcessingChat && !PausePlayback)
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
        NextLineButton.interactable = false;

        if (Math.Abs(NextLineMarkerGroup.alpha - endAlpha) > 0.01f)
        {
            float elapsedTime = 0f;
            float totalDuration = duration;

            while (elapsedTime < totalDuration)
            {
                elapsedTime += Time.deltaTime;
                float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / totalDuration);

                NextLineMarkerGroup.alpha = currentAlpha;

                yield return null;
            }

            NextLineMarkerGroup.alpha = endAlpha;
        }

        // enable the button if alphaed in
        NextLineButton.interactable = endAlpha > 0.5f;
    }
}
