using System;
using System.Text;
using System.Threading;
using System.Net.WebSockets;
using UnityEngine;

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

    private Uri _u;
    private ClientWebSocket _cws = null;
    private readonly int _BufferSize = 1024;
    #endregion

    #region start up
    void Start() {
        _u = new Uri("ws://" + WebSocketUrl);
        Connect();
    }

    /// <summary>
    /// Connect to the websocket
    /// </summary>
    async void Connect()
    {
        _cws = new ClientWebSocket();
        try
        {
            await _cws.ConnectAsync(_u, CancellationToken.None);
            if (_cws.State == WebSocketState.Open)
            {
                Debug.Log("Connected to WebSocket");
            }
            SendText("Hello");
            Receive();

            IsReady = true;
        }
        catch (Exception e) {
            Debug.Log("Failed to connect to WebSocket. Error: " + e.Message);           
        }
    }
    #endregion

    #region send/receive from websocket

    async void SendText(String text)
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
    /// Consume messages from the web api. 
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