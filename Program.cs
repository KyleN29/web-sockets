using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Net;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var sim = new WeatherSimulator();
var clientsByIp = new Dictionary<string, List<WebSocket>>();
var playerStatesByIp = new Dictionary<string, PlayerState>();

app.UseWebSockets();
app.UseStaticFiles();
app.UseDefaultFiles();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Track clients per IP
        if (!clientsByIp.ContainsKey(ipAddress))
        {
            clientsByIp[ipAddress] = new List<WebSocket>();
            playerStatesByIp[ipAddress] = new PlayerState();
        }

        clientsByIp[ipAddress].Add(webSocket);
        Console.WriteLine($"Client connected from IP: {ipAddress}");

        var idBytes = Encoding.UTF8.GetBytes($"connected_from:{ipAddress}");
        await webSocket.SendAsync(new ArraySegment<byte>(idBytes), WebSocketMessageType.Text, true, CancellationToken.None);

        await HandleMessages(ipAddress, webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

async Task HandleMessages(string ip, WebSocket socket)
{
    var buffer = new byte[1024];

    while (socket.State == WebSocketState.Open)
    {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            clientsByIp[ip].Remove(socket);

            if (clientsByIp[ip].Count == 0)
            {
                clientsByIp.Remove(ip);
                playerStatesByIp.Remove(ip);
            }

            Console.WriteLine($"Client disconnected from IP: {ip}");
        }
        else if (result.MessageType == WebSocketMessageType.Text)
        {
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received from {ip}: {receivedMessage}");

            try
            {
                var parsed = JsonSerializer.Deserialize<ClientMessage>(receivedMessage);

                if (parsed?.action == "increase")
                {
                    if (playerStatesByIp[ip].moneyPerStep < 100)
                    {
                        playerStatesByIp[ip].moneyPerStep += 1;
                    }

                    Console.WriteLine($"[{ip}] moneyPerStep: {playerStatesByIp[ip].moneyPerStep}");
                }
                else if (parsed?.action == "getState")
                {
                    var state = playerStatesByIp[ip];
                    var payload = new { money = sim.GetMoney(), moneyPerStep = state.moneyPerStep };
                    var json = JsonSerializer.Serialize(payload);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Invalid JSON: " + ex.Message);
            }
        }
    }
}

// Background simulation
var simTask = Task.Run(async () =>
{
    while (true)
    {
        double totalMoneyPerStep = 0;
        foreach (var kvp in playerStatesByIp)
        {
            totalMoneyPerStep += kvp.Value.moneyPerStep;
            kvp.Value.moneyPerStep = Math.Max(0, kvp.Value.moneyPerStep - 1); // decay
        }

        sim.setMoneyPerStep(totalMoneyPerStep);
        sim.Step();

        foreach (var ip in clientsByIp.Keys)
        {
            var state = playerStatesByIp[ip];
            var payload = new { money = sim.GetMoney(), moneyPerStep = state.moneyPerStep };
            var json = JsonSerializer.Serialize(payload);
            var message = Encoding.UTF8.GetBytes(json);

            foreach (var socket in clientsByIp[ip])
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
        Console.WriteLine("Clients Connected: " + clientsByIp.Count);
        await Task.Delay(1000);
    }
});

app.Run();


// === Classes ===
class WeatherSimulator
{
    private double money = 20.0;
    private double moneyPerStep = 0;

    public void Step() => money += moneyPerStep;
    public double GetMoney() => money;
    public void setMoneyPerStep(double amount) => moneyPerStep = amount;
}

class PlayerState
{
    public double moneyPerStep { get; set; }
}

public class ClientMessage
{
    public string action { get; set; }
}
