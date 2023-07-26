using System.Net.WebSockets;
using System.Text;

namespace WebHookClient;

public class WebSocketClient : IDisposable
{
    private readonly ClientWebSocket _client;
    private bool _islistening;
    private List<Action<string>> _events;
    private Thread _thread;
    private Uri _baseUrl;
    public WebSocketClient(Uri baseUrl)
    {
        _baseUrl = baseUrl;
        _client = new ClientWebSocket();
        _events = new List<Action<string>>();
        _thread = new Thread( Start);
    }

    private async void Start()
    {
        await Receiver();
    }

    ~WebSocketClient()
    {
        Dispose();
    }

    public async Task SandMassage(string msg)
    {
        var data = Encoding.UTF8.GetBytes(msg);
        await _client.ConnectAsync(_baseUrl, CancellationToken.None);
        await _client.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
        await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
    }

    public async Task RegisterEvent(Action<string> act)
    {
        _events.Add(act);
    }

    public void StartListebing()
    {
        _islistening = true;
        _thread.Start();
    }
    
    public void StoptListebing()
    {
        _islistening = false;
        _thread.Interrupt();
    }

    private async Task Receiver()
    {
        await _client.ConnectAsync(_baseUrl, CancellationToken.None);
        while (_islistening)
        {
            var buffer = new byte[8192];
            
            var result = await _client.ReceiveAsync(buffer, CancellationToken.None);
            var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
            foreach (var act in _events)
                act(msg);
            
        }
        await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
    }

    public void Dispose()
    {
        _islistening = false;
        _client.Dispose();
        _thread.Interrupt();
    }
}