namespace MonkeyTyper.MonkeyStuff;

public class Monkey
{
    private String letterQueue = "";
    HashSet<char> attemptedLetters = new HashSet<char>();

    private Dictionary<string, int> guessCounts = File.ReadAllLines("words.txt").ToDictionary(word => word, word => 0, StringComparer.OrdinalIgnoreCase);

    public (char, bool) makeRandomKeyPress()
    {
        char randomLetter = GetRandomLetter();
        if (attemptedLetters.Count >= 26 || letterQueue.Length == 0)
        {
            return ('!', false);
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
        letterQueue += word;
    }

    public char GetRandomLetter()
    {
        return (char)('a' + Random.Shared.Next(0, 26));
    }
}