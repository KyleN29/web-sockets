
using MonkeyTyper.Data; // <- for DbStrings
namespace MonkeyTyper.MonkeyStuff;
public class Monkey
{
    private String letterQueue = "";
    private DateTime lastSave = DateTime.UtcNow;
    HashSet<char> attemptedLetters = new HashSet<char>();

    private Dictionary<string, int> guessCounts = File.ReadAllLines("words.txt").ToDictionary(word => word, word => 0, StringComparer.OrdinalIgnoreCase);

        // Constructor tries to load initial queue from DB
    public Monkey()
    {
        try
        {
            // Run DB fetch synchronously at startup
            var dbValue = DbStrings.GetAsync().GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(dbValue))
            {
                letterQueue = dbValue;
                Console.WriteLine($"[Monkey] Loaded initial queue from DB: \"{letterQueue}\"");
            }
            else
            {
                Console.WriteLine("[Monkey] No queue found in DB; starting empty.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Monkey] Failed to load queue from DB: {ex.Message}");
        }
    }
    
    public (char, bool) makeRandomKeyPress()
    {
        char randomLetter = GetRandomLetter();
        if (attemptedLetters.Count >= 26 || letterQueue.Length == 0)
        {
            return ('!', false);
        }
        if (letterQueue[0] == ' ')
        {
            // Remove first letter
            string rest = letterQueue.Substring(1);
            letterQueue = rest;

            attemptedLetters = new HashSet<char>();

            SaveIfDue();
            return (' ', true);
        }
        while (attemptedLetters.Contains(randomLetter))
        {
            randomLetter = GetRandomLetter();
        }

        if (letterQueue[0] == randomLetter)
        {
            Console.WriteLine("Guessed Correct Letter " + randomLetter);

            // Remove first letter
            string rest = letterQueue.Substring(1);
            letterQueue = rest;

            attemptedLetters = new HashSet<char>();

            SaveIfDue();

            return (randomLetter, true);
        }
        else
        {
            attemptedLetters.Add(randomLetter);
            Console.WriteLine("Guessed wrong letter " + randomLetter + ", correct was " + letterQueue[0]);
            return (randomLetter, false);
        }


    }

    public String getLetterQueue()
    {
        return letterQueue;
    }

    public bool isValidWord(string word)
    {
        if (guessCounts.ContainsKey(word))
        {
            return true;
        }
        return false;
    }

    public void AddWordToQueue(string word)
    {
        letterQueue += " " + word;
        SaveIfDue();
    }

    public char GetRandomLetter()
    {
        return (char)('a' + Random.Shared.Next(0, 26));
    }

    private void SaveIfDue()
    {
        if ((DateTime.UtcNow - lastSave).TotalSeconds >= 10)
        {
            lastSave = DateTime.UtcNow;
            _ = DbStrings.SetAsync(letterQueue);
        }
    }
}