using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using MonkeyTyper.MonkeyStuff;

namespace MonkeyTyper.WebSockets;

public static class WebsocketHandler
{

    public static async Task HandleMessages(string ip, WebSocket socket, Monkey monkey)
    {
        Console.WriteLine("Listening for messages...");
        var buffer = new byte[1024 * 4];



        while (socket.State == WebSocketState.Open)
        {
            var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(
                    receiveResult.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                    receiveResult.CloseStatusDescription ?? "Closing",
                    CancellationToken.None);
                break;
            }

            var message = WebSocketHelper.GetMessageFromBuffer(buffer, receiveResult);

            if (message == "ping")
            {
                await WebSocketHelper.SendTextAsync(socket, "pong-" + string.Concat(monkey.getLetterQueue()));
            }
            else
            {
                Console.WriteLine(message);
                var submission = JsonSerializer.Deserialize<WordSubmission>(message);
                if (string.IsNullOrWhiteSpace(submission.word))
                {
                    Console.WriteLine("JSON Serialize ERROR");
                    continue;
                }
                if (!monkey.isValidWord(submission.word))
                {
                    Console.WriteLine("Invalid Word");
                    continue;
                }
                monkey.AddWordToQueue(submission.word.ToLower());


            }
        }


    }
}

public class WordSubmission
{
    public string word { get; set; }
}