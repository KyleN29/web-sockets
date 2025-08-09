namespace MonkeyTyper.WebSockets;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public static class WebSocketHelper
{
    public static async Task SendTextAsync(WebSocket socket, string message, CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var buffer = new ArraySegment<byte>(bytes);
        await socket.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
    }

    public static async Task SendJsonAsync<T>(WebSocket socket, T data, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(data);
        await SendTextAsync(socket, json, cancellationToken);
    }

    public static async Task<string> ReceiveTextAsync(WebSocket socket, int bufferSize = 1024, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[bufferSize];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    public static async Task CloseSocketAsync(WebSocket socket, string reason = "Closed", CancellationToken cancellationToken = default)
    {
        if (socket.State == WebSocketState.Open)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, cancellationToken);
        }
    }

    public static string GetMessageFromBuffer(byte[] buffer, WebSocketReceiveResult result)
    {
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }
}
