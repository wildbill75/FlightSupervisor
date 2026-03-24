using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;

namespace FlightSupervisor.UI.Services
{
    public class PanelServerService
    {
        private HttpListener _listener;
        private Thread _serverThread;
        private bool _isRunning = false;
        private ConcurrentDictionary<WebSocket, bool> _clients = new();

        public void StartServer()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:5050/");
            _listener.Prefixes.Add("http://127.0.0.1:5050/");
            _listener.Start();
            _isRunning = true;
            _serverThread = new Thread(Listen);
            _serverThread.IsBackground = true;
            _serverThread.Start();
        }

        public void StopServer()
        {
            _isRunning = false;
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        private async void Listen()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        var wsContext = await context.AcceptWebSocketAsync(null);
                        _clients.TryAdd(wsContext.WebSocket, true);
                        _ = HandleWebSocket(wsContext.WebSocket);
                    }
                    else if (context.Request.Url?.AbsolutePath == "/")
                    {
                        ServeHtml(context.Response);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                    }
                }
                catch { }
            }
        }

        private void ServeHtml(HttpListenerResponse response)
        {
            response.ContentType = "text/html; charset=utf-8";
            string html = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Flight Supervisor VR</title>
    <style>
        body { background-color: #121212; color: #ffffff; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 15px; overflow: hidden; }
        .card { background: rgba(30, 30, 30, 0.8); border-radius: 12px; padding: 20px; box-shadow: 0 4px 15px rgba(0,0,0,0.5); backdrop-filter: blur(10px); border: 1px solid rgba(255,255,255,0.1); margin-bottom: 15px; }
        h1 { font-size: 20px; margin: 0 0 10px 0; color: #64B5F6; text-transform: uppercase; letter-spacing: 1px; }
        .score { font-size: 42px; font-weight: bold; margin: 10px 0; background: -webkit-linear-gradient(45deg, #81C784, #4CAF50); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
        .phase { font-size: 16px; color: #B0BEC5; padding: 5px 10px; background: rgba(255,255,255,0.05); border-radius: 6px; display: inline-block; font-weight:bold; }
        .data-row { display: flex; justify-content: space-between; margin-top: 10px; border-top: 1px solid rgba(255,255,255,0.1); padding-top: 10px;}
        .data-label { color: #9E9E9E; font-size: 12px; }
        .data-value { font-weight: bold; font-size: 14px; }
        #connection { position: absolute; top: 15px; right: 15px; width: 12px; height: 12px; border-radius: 50%; background: #F44336; box-shadow: 0 0 8px #F44336; }
        .connected { background: #4CAF50 !important; box-shadow: 0 0 8px #4CAF50 !important; }
    </style>
</head>
<body>
    <div id='connection'></div>
    <div class='card'>
        <h1>SuperScore</h1>
        <div class='score' id='scoreValue'>---</div>
        <div class='phase' id='phaseValue'>STANDBY</div>
    </div>
    <div class='card'>
        <h1>Ground Ops</h1>
        <div class='data-row'><span class='data-label'>Status</span><span class='data-value' id='goStatus'>Waiting for Flight Plan</span></div>
    </div>
    
    <script>
        let ws;
        function connect() {
            ws = new WebSocket('ws://127.0.0.1:5050/');
            ws.onopen = () => document.getElementById('connection').className = 'connected';
            ws.onclose = () => { document.getElementById('connection').className = ''; setTimeout(connect, 2000); };
            ws.onmessage = (e) => {
                try {
                    const data = JSON.parse(e.data);
                    if(data.score !== undefined) document.getElementById('scoreValue').innerText = data.score;
                    if(data.phase !== undefined) document.getElementById('phaseValue').innerText = data.phase.toUpperCase();
                    if(data.groundOps !== undefined) {
                        let formattedLines = data.groundOps.replace(/\n/g, '<br/>');
                        document.getElementById('goStatus').innerHTML = formattedLines;
                    }
                } catch(err) {}
            };
        }
        connect();
    </script>
</body>
</html>";
            var buffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private async Task HandleWebSocket(WebSocket ws)
        {
            var buffer = new byte[1024];
            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                }
                catch { break; }
            }
            _clients.TryRemove(ws, out _);
            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }

        public void BroadcastData(object obj)
        {
            string json = JsonSerializer.Serialize(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(bytes);
            foreach (var client in _clients.Keys.ToList())
            {
                if (client.State == WebSocketState.Open)
                {
                    _ = client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
