using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The `WatsonDemo` class handles interactions with the virtual agent API exposed through a remote webapp
/// </summary>
[RequireComponent(typeof(WebSocket)), RequireComponent(typeof(AudioHandler))]
public class WatsonDemo : MonoBehaviour
{
    #region members
    // Settings
    public const float WaitForServicesTimeout = 10.0f;
    public bool ShowIntentConfidence = false;
    public InputField TextInputField;
    public Text ConversationText;
    public Button TalkButton;

    // Required Components
    public AudioHandler Audio;
    public WebSocket Socket;
    #endregion

    #region enable/disable
    void OnEnable ()
    {
        //Wait for audio and web api services to be ready. 
        StartCoroutine(WaitForServicesReady());
        
        //Actions to take when listening ends
        Audio.StoppedListening += StopTalking;
    }

    void OnDisable()
    {
        Audio.StoppedListening -= StopTalking;
        StopAllCoroutines();
    }

    IEnumerator WaitForServicesReady()
    {
        float startWaitTime = Time.time;
        bool allServicesReady = false;
        TalkButton.interactable = false;    

        while (Time.time - startWaitTime < WaitForServicesTimeout)
        {
            yield return null;
            
            if (Audio.IsReady && Socket.IsReady)
            {
                allServicesReady = true;
                break;
            }
        }

        if(allServicesReady)
        {
            TalkButton.interactable = true;
        }
        else
        {
            ConversationText.text = "Error. Some of the services failed to authenticate. Please verify ApiKey and Url fields are correct for each service.";
        }
    }

    #endregion

    #region start/stop talking
    public void StartTalking()
    {
        if (!Audio.IsListening)
        {
            TextInputField.text = ""; //empty the text field
            Audio.StartTalking(); //begin processing input audio
            StartCoroutine(SendAudioChunks()); //begin sending available audio chunks up to the web API
            StartCoroutine(ListenResults()); //begin receiving available audio chunks from the web API
            TalkButton.interactable = false; //disable button
        }
    }

    private void StopTalking()
    {
        TalkButton.interactable = true; //enable the talk button

        if (Audio.IsListening)
        {
            //currently never get here as AudioHandler calls StopTalking internally.
            //could be used down the road as part of more flexible speech starting/stoping
            Audio.StopTalking();
        }

        //stop sending and listening to results
        StopCoroutine(SendAudioChunks());
        StopCoroutine(ListenResults());
    }

    IEnumerator SendAudioChunks()
    {
        while(true) {
            yield return null;

            if(Audio.IsChunkReady && Socket.AsyncCount<=1)
            {
                //ready to send a new audio chunk to the web api
                Socket.SendBytes(Audio.AudioChunk);
                Debug.Log("Sent a chunk with " + Audio.AudioChunk.Length + " bytes through the socket.");
                Audio.AudioChunk = null;
                Audio.IsChunkReady = false;
            }
            else 
            {
                if (Socket.AsyncCount >= 1) Debug.Log("Waiting on Socket to catch up"); //Cannot have more than 1 outstanding async send so try again later
            }
        }
    }

    IEnumerator ListenResults()
    {
        while (true)
        {
            yield return null;

            if (Socket.IsNewResult)
            {
                //A new audio chunk is available from the api. 
                Audio.OnReceiveAudio(Socket.ReceivedResults);
                Debug.Log("Received a chunk with " + Socket.ReceivedResults.Length + " bytes.");
                Socket.ReceivedResults = new byte[0];
                Socket.IsNewResult = false;
            }
        }
    }
    #endregion

    #region todo: unused methods
    //Currently unused, would 
    private void SpeechToTextSpeechRecognized(string text, double confidence, bool final)
    {
        TextInputField.text = String.Format(" {0} ({1})\n\n", text, confidence);
        

        if (final)
        {
            // put final recognition results on the screen and send them to Conversation service.
            string finalText = text;
            finalText = finalText.Replace("%HESITATION", "");

            ConversationText.text = String.Format("You: {0}\n\n", finalText, confidence);
        }
    }

    public void EnterText()
    {
        ConversationText.text = String.Format("You: {0}\n\n", TextInputField.text);
        TextInputField.text = "";        
    }

    //Currently unused, this would take actions in the game when speech is returned
    private void OnConversationResponse(string text, string intent, float confidence)
    {
        if (text == null)
        {
            text = "";
        }

        if (ShowIntentConfidence)
        {
            ConversationText.text += String.Format("Watson: {0} (#{1} {2:0.000})", text, intent, confidence);
        }
        else
        {
            ConversationText.text += String.Format("Watson: {0}", text);
        }
    }

    //Currently unused, need to adapt to handle errors with web api calls
    private void SpeechToTextError(string error)
    {
        Debug.LogError("Speech to text error: " + error);
        StopTalking();
    }

    #endregion
    
}
