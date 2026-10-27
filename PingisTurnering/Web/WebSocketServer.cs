using System.Net;
using System.Text.Json;

namespace PingisTurnering.Web;

// Very small SignalR-less broadcaster using HTTPListener for a single-page spectator view.
public class WebSocketServerSingleton
{
    private static WebSocketServerSingleton? _instance;
    public static WebSocketServerSingleton? Instance => _instance;

    private readonly HttpListener _listener = new();
    private readonly List<System.Net.WebSockets.WebSocket> _sockets = new();

    public WebSocketServerSingleton(string prefix)
    {
        _instance = this;
        _listener.Prefixes.Add(prefix);
        _listener.Start();
        Task.Run(AcceptLoop);
    }

    private async Task AcceptLoop()
    {
        while (_listener.IsListening)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                if (ctx.Request.IsWebSocketRequest)
                {
                    var wsCtx = await ctx.AcceptWebSocketAsync(null);
                    var ws = wsCtx.WebSocket;
                    lock (_sockets) { _sockets.Add(ws); }
                    _ = Task.Run(() => ReceiveLoop(ws));
                }
                else
                {
                    // Serve static page
                    var resp = ctx.Response;
                    resp.ContentType = "text/html";
                    var html = System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "spectator.html"));
                    var bytes = System.Text.Encoding.UTF8.GetBytes(html);
                    resp.OutputStream.Write(bytes, 0, bytes.Length);
                    resp.Close();
                }
            }
            catch { }
        }
    }

    private async Task ReceiveLoop(System.Net.WebSockets.WebSocket ws)
    {
        var buf = new byte[1024];
        try
        {
            while (ws.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var res = await ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
                if (res.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
            }
        }
        catch { }
        finally
        {
            lock (_sockets) { _sockets.Remove(ws); }
        }
    }

    public async void NotifyMatchUpdated(object match)
    {
        var json = JsonSerializer.Serialize(match);
        var data = System.Text.Encoding.UTF8.GetBytes(json);
        List<System.Net.WebSockets.WebSocket> copy;
        lock (_sockets) { copy = _sockets.ToList(); }
        foreach (var s in copy)
        {
            if (s.State == System.Net.WebSockets.WebSocketState.Open)
            {
                try
                {
                    await s.SendAsync(new ArraySegment<byte>(data), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch { }
            }
        }
    }
}
