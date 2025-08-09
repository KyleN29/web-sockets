using System.Net.WebSockets;
using MonkeyTyper.WebSockets;
using MonkeyTyper.MonkeyStuff;
using System.Text.Json;

var monkey = new Monkey();
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var connectionsByIp = new Dictionary<string, List<WebSocket>>();
var letterCooldownsByIp = new Dictionary<string, UserCooldowns>();
app.UseWebSockets();
app.UseStaticFiles();
app.UseDefaultFiles();


app.MapGet("/ws", async (context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!connectionsByIp.ContainsKey(ipAddress))
        {
            connectionsByIp[ipAddress] = new List<WebSocket>();
        }
        if (!letterCooldownsByIp.ContainsKey(ipAddress))
        {
            letterCooldownsByIp[ipAddress] = new UserCooldowns();
        }
        connectionsByIp[ipAddress].Add(webSocket);
        await WebSocketHelper.SendTextAsync(webSocket, "Connected to Server");
        await WebsocketHandler.HandleMessages(ipAddress, webSocket, monkey);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

var monkeyTask = Task.Run(async () =>
{
    while (true)
    {
        var characterTyped = monkey.makeRandomKeyPress();

        var keysToRemove = connectionsByIp.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();

        foreach (var key in keysToRemove)
        {
            connectionsByIp.Remove(key);
        }

        foreach (var ip in connectionsByIp.Keys)
        {
            connectionsByIp[ip].RemoveAll((socket) => socket.State != WebSocketState.Open);
            foreach (var socket in connectionsByIp[ip])
            {
                var payload = new {characterTyped = characterTyped.Item1,  letterQueue = monkey.getLetterQueue(), correct = characterTyped.Item2};
                var jsonString = JsonSerializer.Serialize(payload);
                await WebSocketHelper.SendTextAsync(socket, jsonString);
            }
        }


        await Task.Delay(1000);
    }
});

app.MapFallbackToFile("index.html");

app.Run();

class UserCooldowns
{
    public Dictionary<char, float> cooldowns = new Dictionary<char, float> {
        {'a', 0 },
        {'b', 0 },
        {'c', 0 },
        {'d', 0 },
        {'e', 0 },
        {'f', 0 },
        {'g', 0 },
        {'h', 0 },
        {'i', 0 },
        {'j', 0 },
        {'k', 0 },
        {'l', 0 },
        {'m', 0 },
        {'n', 0 },
        {'o', 0 },
        {'p', 0 },
        {'q', 0 },
        {'r', 0 },
        {'s', 0 },
        {'t', 0 },
        {'u', 0 },
        {'v', 0 },
        {'w', 0 },
        {'x', 0 },
        {'y', 0 },
        {'z', 0 },
    };
}