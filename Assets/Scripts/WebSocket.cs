using System;
using System.Text;
using System.Threading;
using System.Net.WebSockets;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The `WebSocket` class handles sending and receiving of messages with the Web API through a web socket connection.
/// </summary>
public class WebSocket : MonoBehaviour
{
    #region members

    /// <summary>
    /// The url path for the web socket server.
    /// </summary>
    public string WebSocketUrl;
    public bool IsReady { get; set; } = false;
    public byte[] ReceivedResults { get; set; } = new byte[0];
    public bool IsNewResult { get; set; } = false;
    public int AsyncCount { get; set; } = 0;
    public AudioHandler audio;

    // message strings
    public SocketMessage msg_initiate = new SocketMessage("action", "INITIATE",  new Dictionary<String, String> {
      { "freq", AudioHandler.Meta()[0] }, { "channel", AudioHandler.Meta()[1] }, { "format", AudioHandler.Meta()[2]}});
    public SocketMessage msg_start_listening = new SocketMessage("action", "START_LISTENING", new Dictionary<String, String> {
      { "freq", AudioHandler.Meta()[0] }, { "channel", AudioHandler.Meta()[1] }, { "format", AudioHandler.Meta()[2]}});
    public SocketMessage msg_stop_listening = new SocketMessage("action", "STOP_LISTENING",  new Dictionary<String, String> {
      { "freq", AudioHandler.Meta()[0] }, { "channel", AudioHandler.Meta()[1] }, { "format", AudioHandler.Meta()[2]}});

    private ClientWebSocket _cws = null;
    private readonly int _BufferSize = 1024;
    #endregion

    #region start up
    void Start() {
        Connect();
        SendText(msg_initiate.ToJson());  // Send the initiate message to trigger setup of the stt service
    }

    /// <summary>
    /// Connect to the websocket
    /// </summary>
    public async void Connect()
    {
        _cws = new ClientWebSocket();
        try
        {
            Uri uri = new Uri("ws://" + WebSocketUrl);
            await _cws.ConnectAsync(uri, CancellationToken.None);
            if (_cws.State == WebSocketState.Open)
            {
                Debug.Log("Connected to WebSocket");
            }

            Receive();

            IsReady = true;
        }
        catch (Exception e) {
            Debug.Log("Failed to connect to WebSocket. Error: " + e.Message);
        }
    }
    #endregion

    #region send/receive from websocket

    public async void SendText(String text)
    {
        AsyncCount++;
        ArraySegment<byte> b = new ArraySegment<byte>(Encoding.UTF8.GetBytes(text));
        await _cws.SendAsync(b, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async void SendBytes(byte[] b)
    {
        AsyncCount++;
        ArraySegment<byte> buf = new ArraySegment<byte>(b);
        await _cws.SendAsync(buf, WebSocketMessageType.Binary, true, CancellationToken.None);
    }

    /// <summary>
    /// Consume text messages from the web api.
    /// This method consumes a message in units of _BufferSize until a message is complete.
    /// Results are stored appended in byte array for use of other services
    /// </summary>
    async void ReceiveText()
    {
        ArraySegment<byte> Buffer = new ArraySegment<byte>(new byte[_BufferSize]);

        while (true)
        {
            WebSocketReceiveResult result;
            do
            {
                result = await _cws.ReceiveAsync(Buffer, CancellationToken.None);
                ReceivedResults = WatsonDemoUtils.ConcatenateByteArrays(ReceivedResults, Buffer.Array);
            } while (!result.EndOfMessage);

            AsyncCount--;
            IsNewResult = true;

            if (result.MessageType == WebSocketMessageType.Close)
                break;

        }
    }

    /// <summary>
    /// Consume audio chunks from the web api.
    /// This method consumes a message in units of _BufferSize until a message is complete.
    /// Results are stored appended in byte array for use of other services
    /// </summary>
    async void Receive()
    {
        ArraySegment<byte> Buffer = new ArraySegment<byte>(new byte[_BufferSize]);

        while (true)
        {
            WebSocketReceiveResult result;

            do
            {
                result = await _cws.ReceiveAsync(Buffer, CancellationToken.None);

                Debug.Log(result.MessageType);

                ReceivedResults = WatsonDemoUtils.ConcatenateByteArrays(ReceivedResults, Buffer.Array);
            } while (!result.EndOfMessage);

            AsyncCount--;
            IsNewResult = true;

            if (result.MessageType == WebSocketMessageType.Close)
                break;

        }
    }
    #endregion
}
