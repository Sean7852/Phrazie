using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Phrazie.Core.Interfaces;

namespace Phrazie.Desktop.Services;

/// <summary>
/// Minimal embedded HTTP + WebSocket server.
/// Serves the mobile remote HTML page and maintains real-time state sync
/// with connected browsers via WebSocket frames (RFC 6455).
/// Uses raw TcpListener — no admin / urlacl required.
/// </summary>
public sealed class RemoteServer : IRemoteServer
{
    // ── dependencies ──────────────────────────────────────────────────────────

    private readonly ISessionService  _session;
    private readonly IPlaybackService _playback;

    // ── state ─────────────────────────────────────────────────────────────────

    private TcpListener?             _listener;
    private CancellationTokenSource? _cts;

    private readonly List<WsConn> _connections    = [];
    private readonly object       _connLock       = new();

    public bool    IsRunning { get; private set; }
    public string? LocalUrl  { get; private set; }

    public event Action<Guid>? NextStateRequested;

    // ── ctor ──────────────────────────────────────────────────────────────────

    public RemoteServer(ISessionService session, IPlaybackService playback)
    {
        _session  = session;
        _playback = playback;

        _session.SessionChanged += s  => _ = BroadcastAsync();
        _playback.ClipChanged   += cl => _ = BroadcastAsync();
    }

    // ── lifecycle ─────────────────────────────────────────────────────────────

    public Task StartAsync(int port = 8765)
    {
        if (IsRunning) return Task.CompletedTask;

        LocalUrl  = $"http://{GetLocalIp()}:{port}";
        _cts      = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        IsRunning = true;

        _ = AcceptLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        IsRunning = false;
    }

    public void Dispose() => Stop();

    // ── accept loop ───────────────────────────────────────────────────────────

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(client, ct);
            }
            catch (OperationCanceledException) { break; }
            catch { /* ignore transient accept errors */ }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            client.NoDelay = true;
            var stream = client.GetStream();
            try
            {
                var (_, _, headers) = await ReadHttpHeadersAsync(stream, ct);

                if (headers.TryGetValue("upgrade", out var up) &&
                    string.Equals(up, "websocket", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleWebSocketAsync(stream, headers, ct);
                }
                else
                {
                    await ServeHtmlAsync(stream);
                }
            }
            catch { /* connection closed */ }
        }
    }

    // ── HTTP ──────────────────────────────────────────────────────────────────

    private static async Task ServeHtmlAsync(Stream stream)
    {
        var body    = Encoding.UTF8.GetBytes(RemoteHtml);
        var headers = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n" +
            $"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n");

        await stream.WriteAsync(headers);
        await stream.WriteAsync(body);
        await stream.FlushAsync();
    }

    // ── WebSocket handshake ───────────────────────────────────────────────────

    private async Task HandleWebSocketAsync(
        Stream stream, Dictionary<string, string> headers, CancellationToken ct)
    {
        if (!headers.TryGetValue("sec-websocket-key", out var key))
            return;

        var accept = ComputeWsAccept(key);
        var response = Encoding.ASCII.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\nConnection: Upgrade\r\n" +
            $"Sec-WebSocket-Accept: {accept}\r\n\r\n");

        await stream.WriteAsync(response, ct);
        await stream.FlushAsync(ct);

        var conn = new WsConn(stream);
        lock (_connLock) _connections.Add(conn);

        try
        {
            // Send current state immediately
            await conn.SendTextAsync(BuildStateJson());

            // Read commands
            while (!ct.IsCancellationRequested)
            {
                var text = await conn.ReceiveTextAsync(ct);
                if (text is null) break;
                await HandleCommandAsync(text);
            }
        }
        finally
        {
            lock (_connLock) _connections.Remove(conn);
        }
    }

    // ── command handling ──────────────────────────────────────────────────────

    private async Task HandleCommandAsync(string json)
    {
        try
        {
            using var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeProp)) return;

            switch (typeProp.GetString())
            {
                case "selectNext":
                    if (root.TryGetProperty("stateId", out var idProp) &&
                        Guid.TryParse(idProp.GetString(), out var stateId))
                    {
                        NextStateRequested?.Invoke(stateId);
                    }
                    break;

                case "togglePlay":
                    if (_playback.IsPlaying)
                        await _playback.PauseAsync();
                    else
                        await _playback.ResumeAsync();
                    await BroadcastAsync();
                    break;
            }
        }
        catch { /* malformed JSON */ }
    }

    // ── broadcast ─────────────────────────────────────────────────────────────

    private async Task BroadcastAsync()
    {
        List<WsConn> snapshot;
        lock (_connLock) snapshot = [.._connections];
        if (snapshot.Count == 0) return;

        var json = BuildStateJson();
        var dead = new List<WsConn>();

        foreach (var conn in snapshot)
        {
            try   { await conn.SendTextAsync(json); }
            catch { dead.Add(conn); }
        }

        if (dead.Count > 0)
            lock (_connLock)
                foreach (var c in dead) _connections.Remove(c);
    }

    private string BuildStateJson()
    {
        var s      = _session.Current;
        var states = s.ActiveCollection?.States ?? [];

        return JsonSerializer.Serialize(new
        {
            collection   = s.ActiveCollection?.Name,
            currentState = s.CurrentState is null ? null : new
            {
                id    = s.CurrentState.Id.ToString(),
                name  = s.CurrentState.Name,
                color = s.CurrentState.Color,
            },
            nextState    = s.NextState is null ? null : new
            {
                id    = s.NextState.Id.ToString(),
                name  = s.NextState.Name,
                color = s.NextState.Color,
            },
            states = states.Select(st => new
            {
                id    = st.Id.ToString(),
                name  = st.Name,
                color = st.Color,
            }),
            bpm       = s.Bpm,
            isPlaying = _playback.IsPlaying,
            clip      = _playback.CurrentClip?.DisplayName,
        });
    }

    // ── HTTP header reader ────────────────────────────────────────────────────

    private static async Task<(string method, string path, Dictionary<string, string> headers)>
        ReadHttpHeadersAsync(Stream stream, CancellationToken ct)
    {
        var buf = new byte[8192];
        int pos = 0;

        while (pos < buf.Length)
        {
            await stream.ReadExactlyAsync(buf.AsMemory(pos, 1), ct);
            pos++;
            if (pos >= 4 &&
                buf[pos-4] == '\r' && buf[pos-3] == '\n' &&
                buf[pos-2] == '\r' && buf[pos-1] == '\n')
                break;
        }

        var text  = Encoding.Latin1.GetString(buf, 0, pos);
        var lines = text.Split("\r\n");
        var req   = lines[0].Split(' ');

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            var ci = lines[i].IndexOf(':');
            if (ci > 0)
                headers[lines[i][..ci].Trim()] = lines[i][(ci+1)..].Trim();
        }

        return (req.Length > 0 ? req[0] : "GET",
                req.Length > 1 ? req[1] : "/",
                headers);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static string ComputeWsAccept(string key)
    {
        const string magic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        using var sha1 = SHA1.Create();
        var bytes = sha1.ComputeHash(Encoding.ASCII.GetBytes(key + magic));
        return Convert.ToBase64String(bytes);
    }

    private static string GetLocalIp()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            s.Connect("8.8.8.8", 65530);
            return (s.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "localhost";
        }
        catch { return "localhost"; }
    }

    // ── WebSocket connection helper ───────────────────────────────────────────

    private sealed class WsConn
    {
        private readonly Stream        _stream;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public WsConn(Stream stream) => _stream = stream;

        public async Task SendTextAsync(string text)
        {
            var payload = Encoding.UTF8.GetBytes(text);
            await _sendLock.WaitAsync();
            try   { await WriteFrameAsync(payload); }
            finally { _sendLock.Release(); }
        }

        public async Task<string?> ReceiveTextAsync(CancellationToken ct)
        {
            var (payload, _, isClosed) = await ReadFrameAsync(ct);
            return isClosed ? null : Encoding.UTF8.GetString(payload);
        }

        private async Task WriteFrameAsync(byte[] payload)
        {
            using var ms = new MemoryStream(10 + payload.Length);
            ms.WriteByte(0x81); // FIN + text opcode
            if (payload.Length < 126)
            {
                ms.WriteByte((byte)payload.Length);
            }
            else if (payload.Length < 65536)
            {
                ms.WriteByte(126);
                ms.WriteByte((byte)(payload.Length >> 8));
                ms.WriteByte((byte)(payload.Length & 0xFF));
            }
            else
            {
                ms.WriteByte(127);
                var lenBytes = BitConverter.GetBytes((long)payload.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
                ms.Write(lenBytes);
            }
            ms.Write(payload);
            var frame = ms.ToArray();
            await _stream.WriteAsync(frame);
            await _stream.FlushAsync();
        }

        private async Task<(byte[] payload, bool isBinary, bool isClosed)> ReadFrameAsync(
            CancellationToken ct)
        {
            var header = new byte[2];
            await _stream.ReadExactlyAsync(header, ct);

            int  opcode     = header[0] & 0x0F;
            bool masked     = (header[1] & 0x80) != 0;
            long payloadLen = header[1] & 0x7F;

            if (payloadLen == 126)
            {
                var ext = new byte[2];
                await _stream.ReadExactlyAsync(ext, ct);
                payloadLen = (ext[0] << 8) | ext[1];
            }
            else if (payloadLen == 127)
            {
                var ext = new byte[8];
                await _stream.ReadExactlyAsync(ext, ct);
                if (BitConverter.IsLittleEndian) Array.Reverse(ext);
                payloadLen = BitConverter.ToInt64(ext, 0);
            }

            byte[]? mask = null;
            if (masked)
            {
                mask = new byte[4];
                await _stream.ReadExactlyAsync(mask, ct);
            }

            var payload = new byte[payloadLen];
            if (payloadLen > 0)
                await _stream.ReadExactlyAsync(payload, ct);

            if (masked && mask is not null)
                for (int i = 0; i < payload.Length; i++)
                    payload[i] ^= mask[i % 4];

            return (payload, opcode == 2, opcode == 8);
        }
    }

    // ── embedded remote HTML ─────────────────────────────────────────────────

    private const string RemoteHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1">
          <title>Phrazie Remote</title>
          <style>
            *{box-sizing:border-box;margin:0;padding:0}
            body{background:#0A0A14;color:#CCCCDD;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;min-height:100dvh;padding:20px 16px 72px}
            h1{font-size:15px;font-weight:300;letter-spacing:5px;color:#44CC88;margin-bottom:2px}
            .sub{font-size:9px;color:#333355;letter-spacing:3px;margin-bottom:28px}
            .section{margin-bottom:24px}
            .lbl{font-size:9px;letter-spacing:3px;color:#333355;margin-bottom:8px}
            .val{font-size:24px;font-weight:200}
            .clip{font-size:11px;color:#666688;margin-top:4px}
            .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(88px,1fr));gap:8px}
            .sbtn{background:#12121E;border:1px solid #1E1E30;border-radius:6px;color:#444466;padding:12px 8px;font-size:13px;cursor:pointer;transition:all .1s;-webkit-tap-highlight-color:transparent;outline:none;width:100%}
            .sbtn.active{border-color:#2A5A2A;background:#0E1E0E;color:#66CC66}
            .sbtn.next{border-color:#3A6A9F;background:#1E3A5F;color:#fff}
            .pbtn{background:#1E3A5F;border:1px solid transparent;border-radius:8px;color:#fff;padding:16px;font-size:13px;letter-spacing:2px;font-weight:500;cursor:pointer;width:100%;transition:all .12s;-webkit-tap-highlight-color:transparent;outline:none}
            .pbtn.paused{background:#12121E;border-color:#1E1E30;color:#555577}
            hr{border:none;border-top:1px solid #111122;margin:20px 0}
            .conn{position:fixed;bottom:0;left:0;right:0;background:#0A0A14;border-top:1px solid #111122;padding:9px 16px;font-size:9px;letter-spacing:2px;color:#333355;display:flex;align-items:center;gap:6px}
            .dot{width:5px;height:5px;border-radius:50%;background:#1E1E30;flex-shrink:0}
            .conn.ok .dot{background:#44CC88}.conn.ok{color:#556677}
          </style>
        </head>
        <body>
          <h1>PHRAZIE</h1><p class="sub">REMOTE CONTROL</p>
          <div class="section"><div class="lbl">COLLECTION</div><div class="val" id="col">—</div></div>
          <div class="section">
            <div class="lbl">CURRENT STATE</div>
            <div class="val" id="cur">—</div>
            <div class="clip" id="clip"></div>
          </div>
          <hr>
          <div class="section"><div class="lbl">SELECT NEXT STATE</div><div class="grid" id="grid"></div></div>
          <hr>
          <div class="section"><button class="pbtn paused" id="pbtn" onclick="togglePlay()">&#9654;&#xA0; PLAY</button></div>
          <div class="conn" id="conn"><span class="dot"></span>CONNECTING</div>
          <script>
            var ws,s={};
            function connect(){
              ws=new WebSocket('ws://'+location.host+'/ws');
              ws.onopen=function(){var c=document.getElementById('conn');c.className='conn ok';c.innerHTML='<span class="dot"></span>CONNECTED'};
              ws.onclose=function(){var c=document.getElementById('conn');c.className='conn';c.innerHTML='<span class="dot"></span>DISCONNECTED';setTimeout(connect,2000)};
              ws.onmessage=function(e){s=JSON.parse(e.data);render()};
            }
            function render(){
              document.getElementById('col').textContent=s.collection||'—';
              document.getElementById('cur').textContent=(s.currentState&&s.currentState.name)||'—';
              document.getElementById('clip').textContent=s.clip||'';
              var b=document.getElementById('pbtn');
              if(s.isPlaying){b.textContent='\u23F8\u00A0 PAUSE';b.className='pbtn';}
              else{b.textContent='\u25B6\u00A0 PLAY';b.className='pbtn paused';}
              var g=document.getElementById('grid');g.innerHTML='';
              (s.states||[]).forEach(function(st){
                var btn=document.createElement('button');
                var cls='sbtn';
                if(s.currentState&&st.id===s.currentState.id)cls+=' active';
                if(s.nextState&&st.id===s.nextState.id)cls+=' next';
                btn.className=cls;btn.textContent=st.name;
                btn.onclick=function(){send({type:'selectNext',stateId:st.id})};
                g.appendChild(btn);
              });
            }
            function togglePlay(){send({type:'togglePlay'})}
            function send(m){if(ws&&ws.readyState===1)ws.send(JSON.stringify(m))}
            connect();
          </script>
        </body>
        </html>
        """;
}
