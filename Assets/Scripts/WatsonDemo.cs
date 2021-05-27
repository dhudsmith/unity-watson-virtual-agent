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
        Socket.SendText(Socket.msg_start_listening.ToJson()); //send the start message to spin up speach to text
        if (!Audio.IsListening)
        {
            if (!Socket.IsReady)
            {
                Socket.Connect();
            }

            Audio.StartTalking(); //begin processing input audio
            StartCoroutine("SendAudioChunks"); //begin sending available audio chunks up to the web API
            StartCoroutine("ListenTextMessages"); //begin receiving available audio chunks from the web API
            StartCoroutine("ListenAudioMessages");
            TalkButton.interactable = false; //disable button
        }
    }

    private void StopTalking()
    {
        TalkButton.interactable = true; //enable the talk button

        Socket.SendText(Socket.msg_stop_listening.ToJson()); //send the stop message to trigger graceful close of stt

        if (Audio.IsListening)
        {
            //currently never get here as AudioHandler calls StopTalking internally.
            //could be used down the road as part of more flexible speech starting/stoping
             Audio.StopTalking();
        }

        //stop sending and listening to results
        StopCoroutine("SendAudioChunks"); //TODO: wait until buffer queue is empty before shutting down!
    }

    IEnumerator SendAudioChunks()
    {
        while(true) {
            yield return null;

            if(Audio.IsChunkReady)
            {
                //ready to send a new audio chunk to the web api
                Socket.SendBytes(Audio.AudioChunk);
                Debug.Log("Sent a chunk with " + Audio.AudioChunk.Length + " bytes through the socket.");
                Audio.AudioChunk = null;
                Audio.IsChunkReady = false;
            }
        }
    }


    IEnumerator ListenTextMessages()
    {
        while (true)
        {
            yield return null;

            if (Socket.ReceivedMessageQueue.Count>0)
            {
                OnTextMessageReceived();
            }
        }
    }

    IEnumerator ListenAudioMessages()
    {
         while (true)
         {
             yield return null;

            
             while (Socket.ReceivedAudioQueue.Count > 0)
             {//A new audio chunk is available from the api.
                 byte[] temp = Socket.ReceivedAudioQueue.Dequeue();
                 Audio.OnReceiveAudio(temp);
                 Debug.Log("Received a chunk with " + temp.Length + " bytes.");

             }
             


         }
    }

    //Handle incoming text messages
    private void OnTextMessageReceived()
    {
        SocketMessage msg = Socket.ReceivedMessageQueue.Dequeue();
        Debug.Log(msg);

        if (ConversationText.text == null)
        {
            ConversationText.text = "";
        }

        if (msg.type == "AGENT_RESULT")
        {
            if (msg.note == "SPEAKER_TRANSCRIPT")
            {
                ConversationText.text += String.Format("\nYou: {0}", msg.meta["text"]);
            }
            else if(msg.note == "AGENT_RESPONSE")
            {
                ConversationText.text += String.Format("\nBob: {0}", msg.meta["text"]);
            }
        }
        else if (msg.type == "action")
        {
            if (msg.note == "DONE_SPEECH_SYNTHESIS")
            {
                               
                //check if all audio has been processed, if not, requeue the stop message for later
                if (Socket.ReceivedAudioQueue.Count > 0)
                {
                    Socket.ReceivedMessageQueue.Enqueue(msg);
                }
                else
                {
                    StopListening();
                }
                
            }
            
        }
        else
        {
            Debug.LogError(String.Format("Unknown message type: {0}", msg.type));
        }
    }
    
    private void StopListening()
    {
        //stop coroutines related to listening and play the audio message
        StopCoroutine("ListenTextMessages");
        StopCoroutine("ListenAudioMessages");
        Audio.DoneListening();
    }
    #endregion


    #region todo: unused methods
    //Currently unused, would
    private void SpeechToTextSpeechRecognized(string text, double confidence, bool final)
    {


        if (final)
        {
            // put final recognition results on the screen and send them to Conversation service.
            string finalText = text;
            finalText = finalText.Replace("%HESITATION", "");

            ConversationText.text = String.Format("You: {0}\n\n", finalText, confidence);
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
