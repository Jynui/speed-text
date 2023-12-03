using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

[Serializable]
public class UserData
{
    public string Name { get; set; }
    public int CharactersPerMinute { get; set; }
    public double CharactersPerSecond { get; set; }
}

public static class Leaderboard
{
    private const string LeaderboardFilePath = "leaderboard.json";
    private static List<UserData> leaderboardData;

    static Leaderboard()
    {
        LoadLeaderboard();
    }

    private static void LoadLeaderboard()
    {
        if (File.Exists(LeaderboardFilePath))
        {
            string json = File.ReadAllText(LeaderboardFilePath);
            leaderboardData = JsonConvert.DeserializeObject<List<UserData>>(json);
        }
        else
        {
            leaderboardData = new List<UserData>();
        }
    }

    public static void AddUserToLeaderboard(UserData user)
    {
        leaderboardData.Add(user);
        SaveLeaderboard();
    }

    public static List<UserData> GetLeaderboard()
    {
        return leaderboardData.OrderByDescending(u => u.CharactersPerMinute).ToList();
    }

    private static void SaveLeaderboard()
    {
        string json = JsonConvert.SerializeObject(leaderboardData, Formatting.Indented);
        File.WriteAllText(LeaderboardFilePath, json);
    }
}

public class TypingTest
{
    private const string TextToType = "Чем уникальна Хакасия? Этот регион – часть огромной Евразийской степи, которая растянулась от Карпат до Северо-Западного Китая. Хакасия изолирована мощными горными системами: Кузнецким Алатау, Восточным и Западным Саянами. Это своего рода затерянный мир с райскими условиями, который при этом спрятан за горами.";
    private const int TestDurationSeconds = 60;

    public static void StartTest()
    {
        Console.Clear();

        Console.Write("Введите ваше имя: ");
        string userName = Console.ReadLine();

        Console.WriteLine("Наберите следующий текст:");
        Console.WriteLine(TextToType);
        Console.WriteLine("Нажмите Enter для начала набора.");

        Console.ReadLine();

        TypingTestResult result = RunTypingTest();
        DisplayResult(result);

        UserData user = new UserData
        {
            Name = userName,
            CharactersPerMinute = result.CharactersPerMinute,
            CharactersPerSecond = result.CharactersPerSecond
        };

        Leaderboard.AddUserToLeaderboard(user);
    }

    private static TypingTestResult RunTypingTest()
    {
        Stopwatch stopwatch = new Stopwatch();
        StringBuilder userTypedText = new StringBuilder();
        ManualResetEvent timerFinished = new ManualResetEvent(false);

        ConsoleTimer consoleTimer = new ConsoleTimer(TestDurationSeconds * 1000);
        consoleTimer.Elapsed += (sender, e) =>
        {
            timerFinished.Set();
        };

        Console.WriteLine("Текст для повторения:");
        Console.WriteLine(TextToType);

        Thread timerThread = new Thread(() =>
        {
            consoleTimer.Start();
            timerFinished.WaitOne();
            consoleTimer.Stop();
        });

        timerThread.Start();

        
        Thread displayThread = new Thread(() =>
        {
            DisplayText(consoleTimer, TextToType);
        });

        displayThread.Start();

        ConsoleKeyInfo keyInfo;
        do
        {
            keyInfo = Console.ReadKey(true);
            userTypedText.Append(keyInfo.KeyChar);
            Console.Write(keyInfo.KeyChar);
        } while (!consoleTimer.Finished);

        timerThread.Join();
        displayThread.Join();

        int charactersTyped = TextToType.Length;
        double charactersPerMinute = charactersTyped / (TestDurationSeconds / 60.0);
        double charactersPerSecond = charactersTyped / TestDurationSeconds;

        return new TypingTestResult
        {
            CharactersPerMinute = (int)charactersPerMinute,
            CharactersPerSecond = charactersPerSecond
        };
    }

    private static void DisplayText(ConsoleTimer timer, string text)
    {
        Stopwatch displayStopwatch = new Stopwatch();
        displayStopwatch.Start();


        while (!timer.Finished)
        {
            Console.Clear();
            Console.WriteLine("Текст для повторения:");
            Console.WriteLine(text);
            Console.WriteLine($"\nПрошло: {displayStopwatch.Elapsed.ToString(@"mm\:ss")}");
            Thread.Sleep(100);
        }

        displayStopwatch.Stop();
    }

    private static void DisplayResult(TypingTestResult result)
    {
        Console.WriteLine("\nТест завершён!");
        Console.WriteLine($"Символов в минуту: {result.CharactersPerMinute}");
        Console.WriteLine($"Символов в секунду: {result.CharactersPerSecond:F2}");
    }
}

public class TypingTestResult
{
    public int CharactersPerMinute { get; set; }
    public double CharactersPerSecond { get; set; }
}

public class ConsoleTimer : System.Timers.Timer
{
    private DateTime startTime;

    public bool Finished { get; private set; }

    public ConsoleTimer(double interval) : base(interval)
    {
        this.AutoReset = false;
        this.Finished = false;
        this.Elapsed += TimerElapsed;
    }

    public new void Start()
    {
        this.startTime = DateTime.Now;
        this.Finished = false;
        base.Start();
    }

    private void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        this.Finished = true;
    }
}

class Program
{
    static void Main()
    {
        while (true)
        {
            TypingTest.StartTest();

            Console.WriteLine("\nЛидерборд:");
            List<UserData> leaderboard = Leaderboard.GetLeaderboard();
            foreach (var user in leaderboard)
            {
                Console.WriteLine($"{user.Name} - {user.CharactersPerMinute} символов в минуту, {user.CharactersPerSecond:F2} символов в секунду");
            }

            Console.WriteLine("\nНажмите Enter для начала нового теста или Esc для выхода.");
            if (Console.ReadKey().Key == ConsoleKey.Escape)
            {
                break;
            }
        }
    }
}
