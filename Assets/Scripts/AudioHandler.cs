using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using IBM.Cloud.SDK;

public delegate void SpeechRecognizedDelegate(string text, double confidence, bool final);
public delegate void SpeechErrorDelegate(string error);
public delegate void StopListeningDelegate();

/// <summary>
/// AudioHandler handles pulling of audio chunks from the microphone and publishing them
/// for consumption by other services.
/// </summary>
public class AudioHandler : MonoBehaviour
{
    #region members
    //Event delegates
    public event SpeechRecognizedDelegate SpeechRecognized = delegate { };
    public event SpeechErrorDelegate SpeechError = delegate { };
    public event StopListeningDelegate StoppedListening = delegate { };

    //public members
    public AudioSource AudioInput;
    public AudioSource AudioOutput;
    public const int MicDeviceId = 0;
    public float InactivityTimeoutSec = 6.0f;
    public bool IsReady { get; set; } = false;
    public bool IsListening { get; set; } = false;
    public bool IsChunkReady { get; set; } = false;
    public byte[] AudioChunk { get; set; } = null;
    
    //private members
    private const int MIC_REC_BUFFER_LEN_SEC = 30;
    private const int MIC_FREQUENCY = 16000;
    private const float PUSH_AUDIO_CHUNK_INTERVAL = 1f;
    private int _audioChunkStartPosition = -1;
    private AudioClip _rollingAudioClip;
    private Coroutine _checkJobStatusCoroutine;
    private Coroutine _pushAudioChunkCroutine;
    private Coroutine _stopListeningTimeoutCoroutine;
    private List<float> _playBackAudioData;
    private byte[] _outputAudioStream { get; set; } = null;
    #endregion

    #region start, OnEnable, and OnDisable
    void Start()
    {
        IsReady = true;
    }

    void OnEnable()
    {
        // Starting recording in here. We'll record continuously and loop the audio clip.
        // We'll only send data that's captured between StartTalking and StopTalking calls

        Debug.Log("Starting recording on " + Microphone.devices[MicDeviceId]);

        _rollingAudioClip = Microphone.Start(
            Microphone.devices[MicDeviceId],
            true,
            MIC_REC_BUFFER_LEN_SEC,
            MIC_FREQUENCY);
    }

    void OnDisable()
    {
        Microphone.End(Microphone.devices[MicDeviceId]);
    }

    //Currently unused
    private void OnSpeachToTextError(string error)
    {
        Debug.Log(string.Format("Speech to text error! {0}", error));
        SpeechError(error);
    }
    #endregion

    #region Starting and Stopping Talking
    /// <summary>
    /// StartTalking turns on the microphone, and begins routines for processing audio chunks
    /// </summary>
    public void StartTalking()
    {
        if (!IsListening)
        {
            IsListening = true;

            _playBackAudioData = new List<float>();

            _audioChunkStartPosition = Microphone.GetPosition(Microphone.devices[0]);

            // cancel the timeout if user starts speaking
            if (_stopListeningTimeoutCoroutine != null)
            {
                StopCoroutine(_stopListeningTimeoutCoroutine);
            }
            
            _pushAudioChunkCroutine = StartCoroutine(PrepareAudioChunkCoroutine());
            _stopListeningTimeoutCoroutine = StartCoroutine(StopTalkingTimeout(InactivityTimeoutSec));
        }
    }

    /// <summary>
    /// StopTalking shuts down processes for listening to the mic and publishing audio chunks, plays the
    /// final audio clip, and invokes any other actions tied to end of talking. 
    /// </summary>
    public void StopTalking()
    {
        if (IsListening)
        {
            IsListening = false;

            //Stop pushing audio chunks and stop listening
            StopCoroutine(_pushAudioChunkCroutine);
            _pushAudioChunkCroutine = null;
            StopCoroutine(_stopListeningTimeoutCoroutine);
            _stopListeningTimeoutCoroutine = null;

            //Play the final audio clip
            AudioOutput.clip = GetFinalAudioClip();
            AudioOutput.Play();

            //Reset the output audio data
            _outputAudioStream = null;

            //Invoke any subscribed routines
            StoppedListening?.Invoke();
        }
    }

    /// <summary>
    /// Turn audio off after a fixed amount of time.
    /// </summary>
    /// <param name="delay"></param>
    /// <returns></returns>
    IEnumerator StopTalkingTimeout(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopTalking();
    }
    #endregion

    #region Receiving Audio
    public void OnReceiveAudio(byte[] result)
    {
        _outputAudioStream = WatsonDemoUtils.ConcatenateByteArrays(_outputAudioStream, result);
    }

    public AudioClip GetFinalAudioClip()
    {
        // convert to float
        float[] outputAudioStreamData = WatsonDemoUtils.PCM2Floats(_outputAudioStream);
        AudioClip clip = AudioClip.Create("clip", _outputAudioStream.Length, _rollingAudioClip.channels, MIC_FREQUENCY, false);
        clip.SetData(outputAudioStreamData, 0);

        return clip;
    }
    #endregion

    #region Publishing audio chunks
    /// <summary>
    /// Coroutine that pushes audio chunks at a fixed time interval
    /// </summary>
    /// <returns></returns>
    private IEnumerator PrepareAudioChunkCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(PUSH_AUDIO_CHUNK_INTERVAL);
            PrepareAudioChunk();
        }
    }

    /// <summary>
    /// Logic for pulling audio from the microphone and formatting it appropriately
    /// </summary>
    private void PrepareAudioChunk()
    {
        //last position recorded by microphone
        int endPosition = Microphone.GetPosition(Microphone.devices[0]);

        if (endPosition == _audioChunkStartPosition)
        {
            //no data to send
            return;
        }

        float[] speechAudioData;
        int newClipLength;

        if (endPosition > _audioChunkStartPosition)
        {
            // there is some new data to send
            newClipLength = endPosition - _audioChunkStartPosition + 1;
            speechAudioData = new float[newClipLength * _rollingAudioClip.channels];
            _rollingAudioClip.GetData(speechAudioData, _audioChunkStartPosition);

        }
        else
        {   
            // We've wrapped around the rolling audio clip. We have to take the audio from start position till the end of the rolling clip. Then, add clip from 0 to endPosition;
            int newClipLengthLeft = _rollingAudioClip.samples - _audioChunkStartPosition + 1;
            int newClipLengthRight = endPosition + 1;

            float[] speechAudioDataLeft = new float[newClipLengthLeft * _rollingAudioClip.channels];
            float[] speechAudioDataRight = new float[newClipLengthRight * _rollingAudioClip.channels];

            _rollingAudioClip.GetData(speechAudioDataLeft, _audioChunkStartPosition);
            _rollingAudioClip.GetData(speechAudioDataRight, 0);

            newClipLength = speechAudioDataLeft.Length + speechAudioDataRight.Length;
            speechAudioData = new float[newClipLength];

            Array.Copy(speechAudioDataLeft, speechAudioData, newClipLengthLeft);
            Array.Copy(speechAudioDataRight, 0, speechAudioData, newClipLengthLeft, newClipLengthRight);
        }

        AudioClip Clip = AudioClip.Create("clip", newClipLength, _rollingAudioClip.channels, MIC_FREQUENCY, false);
        Clip.SetData(speechAudioData, 0);

        _audioChunkStartPosition = endPosition;

        PublishClip(Clip);
    }

    /// <summary>
    /// Logic for publishing the audio clip for use by other services
    /// </summary>
    /// <param name="clip"></param>
    private void PublishClip(AudioClip clip)
    {
        AudioChunk = WatsonDemoUtils.ConcatenateByteArrays(AudioChunk, AudioClipUtil.GetL16(clip));
        IsChunkReady = true;
        Debug.Log("Published " + AudioChunk.Length + " bytes of audio data.");
    }
    #endregion
}

  
