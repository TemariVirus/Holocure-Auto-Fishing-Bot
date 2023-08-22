using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static WindowUtils;

internal static class Images
{
    public static readonly Image2D Target = new Image2D("img/target circle.png");

    public static readonly Image2D Circle = new Image2D("img/circle.png");
    public static readonly Image2D Left = new Image2D("img/left.png");
    public static readonly Image2D Right = new Image2D("img/right.png");
    public static readonly Image2D Up = new Image2D("img/up.png");
    public static readonly Image2D Down = new Image2D("img/down.png");
}

internal struct Settings
{
    public double Resolution { get; set; }
    public string[] TheButtons { get; set; }
    public bool Fullscreen { get; set; }
}

static class Program
{
    public static Settings Settings { get; }

    private static readonly IntPtr _windowHandle;
    private static int _targetLeft = -1;
    private static int _targetTop = -1;
    private static readonly Stopwatch _noteTimer = new Stopwatch();

    static Program()
    {
        // Keep console open on crash
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
            CrashHandler
        );

        // Setup
        try
        {
            Settings = GetHolocureSettings();
            Console.WriteLine("Holocure settings found.");
            Console.WriteLine($"Buttons: [{string.Join(", ", Settings.TheButtons)}]");

            _windowHandle = GetHolocureWindow();
            Console.WriteLine("Holocure window found.");

            if (Settings.Fullscreen)
            {
                throw new Exception("Please turn off fullscreen.");
            }
            if (Settings.Resolution != 0.0)
            {
                throw new Exception("Please set resolution to 640x360.");
            }
        }
        catch (Exception e)
        {
            CrashHandler(null, new UnhandledExceptionEventArgs(e, false));
        }
    }

    static void Main()
    {
        Console.WriteLine("Bot started.");
        Console.WriteLine(
            "Please ensure that the minigame is within view at all times (you can still have other windows on top of it).\n"
        );
        // Start bot loop
        int capture_count = 0;
        bool playing = false;
        Stopwatch perf_sw = Stopwatch.StartNew();
        _noteTimer.Restart();
        while (true)
        {
            if (!playing)
            {
                perf_sw.Stop();

                StartFishingGame();
                _noteTimer.Restart();
                playing = true;

                perf_sw.Start();
                continue;
            }

            if (_targetLeft >= 0 && _targetTop >= 0)
            {
                PlayFishingGame();
            }
            else
            {
                FindTargetArea();
            }

            // Aim for a little over 60 captures per second to match framerate
            while (capture_count / perf_sw.Elapsed.TotalSeconds > 69)
            {
                Thread.Sleep(1);
            }

            // Print captures per second
            capture_count++;
            if (perf_sw.ElapsedMilliseconds >= 1000)
            {
                Console.WriteLine(
                    $"Captures per second: {capture_count / perf_sw.Elapsed.TotalSeconds:F2}"
                );
                capture_count = 0;
                perf_sw.Restart();
            }

            // If no notes for too long, restart
            if (_noteTimer.ElapsedMilliseconds >= 1222)
            {
                playing = false;
                _noteTimer.Restart();
            }
        }
    }

    private static void CrashHandler(object sender, UnhandledExceptionEventArgs args)
    {
        Console.WriteLine($"\n{args.ExceptionObject}\n\nPress any key to exit.");
        Console.ReadKey();
        Environment.Exit(1);
    }

    private static Settings GetHolocureSettings()
    {
        string user_path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string file_path = $"{user_path}\\AppData\\Local\\HoloCure\\settings.json";
        if (!File.Exists(file_path))
        {
            throw new FileNotFoundException(
                "Settings file not found. HoloCure may not be installed."
            );
        }

        string json_text = File.ReadAllText(file_path);
        return JsonConvert.DeserializeObject<Settings>(json_text);
    }

    private static void StartFishingGame()
    {
        for (int i = 0; i < 2; i++)
        {
            Console.WriteLine($"Pressing ENTER");
            InputUtils.SendKey(_windowHandle, "ENTER");
            Thread.Sleep(150);
        }
    }

    private static void PlayFishingGame()
    {
        int right = Note.Notes.Max(note => note.Right);
        int bottom = Note.Notes.Max(note => note.Bottom);

        Image2D target_area = CaptureWindow(_windowHandle, _targetLeft, _targetTop, right, bottom);
        foreach (Note note in Note.Notes)
        {
            if (target_area.ContainsNote(note))
            {
                Console.WriteLine($"Pressing {note.Button}");
                InputUtils.SendKey(_windowHandle, note.Button);
                _noteTimer.Restart();
                break;
            }
        }
    }

    private static void FindTargetArea()
    {
        Image2D screen = CaptureWindow(_windowHandle);
        (_targetLeft, _targetTop) = screen.Find(Images.Target);
        if (_targetLeft >= 0 && _targetTop >= 0)
        {
            Console.WriteLine($"Target area found: X={_targetLeft}, Y={_targetTop}");
        }
    }
}
