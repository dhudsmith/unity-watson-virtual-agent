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
    public Queue<SocketMessage> ReceivedMessageQueue = new Queue<SocketMessage>();
    public int AsyncCount { get; set; } = 0;
    public AudioHandler audio;

    // message strings
    public SocketMessage msg_initiate = new SocketMessage("action", "INITIATE",  new Dictionary<String, String> {
      { "freq", AudioHandler.Meta()[0] }, { "channel", AudioHandler.Meta()[1] }, { "format", AudioHandler.Meta()[2]}});
    public SocketMessage msg_start_listening = new SocketMessage("action", "START_LISTENING", null);
    public SocketMessage msg_stop_listening = new SocketMessage("action", "STOP_LISTENING",  null);

    private ClientWebSocket _cws = null;
    private readonly int _BufferSize = 1024;
    #endregion

    #region start up
    void Start() {
        Connect();
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
                Debug.Log("Connected to WebSocket.");
                Debug.Log("Sending initiate message to agent.");
                SendText(msg_initiate.ToJson());
                Receive();
                IsReady = true;
            }
        }
        catch (Exception e) {
            Debug.Log("Failed to connect to WebSocket. Error: " + e.Message);
        }
    }
    #endregion

    #region send/receive from websocket

    public async void SendText(String text)
    {
        ArraySegment<byte> b = new ArraySegment<byte>(Encoding.UTF8.GetBytes(text));
        await _cws.SendAsync(b, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async void SendBytes(byte[] b)
    {
        ArraySegment<byte> buf = new ArraySegment<byte>(b);
        await _cws.SendAsync(buf, WebSocketMessageType.Binary, true, CancellationToken.None);
    }

    /// <summary>
    /// Consume audio chunks from the web api.
    /// This method consumes a message in units of _BufferSize until a message is complete.
    /// Results are stored appended in byte array for use of other services
    /// </summary>
    async void Receive()
    {
        while (true)
        {
            WebSocketReceiveResult result;
            ReceivedResults = new byte[0];
            ArraySegment<byte> Buffer = new ArraySegment<byte>(new byte[_BufferSize]);

            do
            {
                result = await _cws.ReceiveAsync(Buffer, CancellationToken.None);
                ReceivedResults = WatsonDemoUtils.ConcatenateByteArrays(ReceivedResults, Buffer.Array);
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string text = System.Text.Encoding.UTF8.GetString(ReceivedResults, 0, ReceivedResults.Length);
                Debug.Log(text);
                ReceivedMessageQueue.Enqueue(Newtonsoft.Json.JsonConvert.DeserializeObject<SocketMessage>(text));
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }
        }
    }
    #endregion
}
