using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using static WindowUtils;

internal struct Settings
{
    public double Resolution { get; set; }
    public string[] TheButtons { get; set; }
    public bool Fullscreen { get; set; }
}

static class Program
{
    private static readonly ReadonlyImage _targetImage = new ReadonlyImage("img/target circle.png");
    private static readonly ReadonlyImage _okImage = new ReadonlyImage("img/ok.png");
    private static readonly ReadonlyImage _cImage = new ReadonlyImage("img/c.png");
    private static readonly Note[] _notes;

    private static readonly Settings _settings;

    private static readonly IntPtr _windowHandle;
    private static int _targetLeft = -1;
    private static int _targetTop = -1;

    static Program()
    {
        // Keep console open on crash
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
            ExceptionHandler
        );

        // Setup
        try
        {
            _settings = GetHolocureSettings();
            Console.WriteLine("Holocure settings found.");
            Console.WriteLine($"Buttons: [{string.Join(", ", _settings.TheButtons)}]");

            _windowHandle = GetHolocureWindow();
            Console.WriteLine("Holocure window found.");

            if (_settings.Fullscreen)
            {
                throw new Exception("Please turn off fullscreen.");
            }
            if (_settings.Resolution != 0.0)
            {
                throw new Exception("Please set resolution to 640x360.");
            }

            _notes = new Note[] {
                    new Note(new ReadonlyImage("img/circle.png"), _settings.TheButtons[0], 5, 15, 30, 32),
                    new Note(new ReadonlyImage("img/left.png"), _settings.TheButtons[2], 2, 14, 32, 33),
                    new Note(new ReadonlyImage("img/right.png"), _settings.TheButtons[3], 2, 14, 32, 33),
                    new Note(new ReadonlyImage("img/up.png"), _settings.TheButtons[4], 3, 13, 31, 34),
                    new Note(new ReadonlyImage("img/down.png"), _settings.TheButtons[5], 3, 13, 31, 34)
                };
        }
        catch (Exception e)
        {
            // Manually call exception handler as it has not been set yet
            ExceptionHandler(null, new UnhandledExceptionEventArgs(e, false));
        }
    }

    static void Main()
    {
        Console.WriteLine("Bot started.");
        Console.WriteLine(
            "Please ensure that the minigame is within view at all times (you can still have other windows on top of it).\n"
        );
        // Start bot loop
        bool playing = false;
        int captureCount = 0;
        int chain = 0;
        Stopwatch perfSw = Stopwatch.StartNew();
        Stopwatch timeoutSw = Stopwatch.StartNew();
        while (true)
        {
            if (!playing)
            {
                perfSw.Stop();

                StartFishingGame();
                timeoutSw.Restart();
                playing = true;

                perfSw.Start();
                continue;
            }

            if (_targetLeft >= 0 && _targetTop >= 0)
            {
                PlayFishingGame(ref playing, ref chain, in timeoutSw);
            }
            else
            {
                FindTargetArea();
            }

            // Aim for a little over 60 captures per second to match framerate
            while (captureCount / perfSw.Elapsed.TotalSeconds > 69)
            {
                Thread.Sleep(1);
            }

            // Print captures per second
            captureCount++;
            if (perfSw.ElapsedMilliseconds >= 1000)
            {
                Console.WriteLine(
                    $"Captures per second: {captureCount / perfSw.Elapsed.TotalSeconds:F2}"
                );
                captureCount = 0;
                perfSw.Restart();
            }

            // If no notes for too long, restart
            if (timeoutSw.ElapsedMilliseconds >= 30_000)
            {
                Console.WriteLine("No notes detected in 30 seconds. Attempting to restart minigame.");
                Console.WriteLine($"Chain broke at {chain}.");
                playing = false;
                chain = 0;
                timeoutSw.Restart();
            }
        }
    }

    private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
        Console.WriteLine($"\n{args.ExceptionObject}\n\nPress any key to exit.");
        Console.ReadKey();
        Environment.Exit(1);
    }

    private static Settings GetHolocureSettings()
    {
        string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string filePath = $"{userPath}\\AppData\\Local\\HoloCure\\settings.json";
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Settings file not found. HoloCure may not be installed."
            );
        }

        string jsonText = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<Settings>(jsonText);
    }

    public static IntPtr GetHolocureWindow()
    {
        Process[] processes = Process.GetProcessesByName("holocure");
        if (processes.Length <= 0)
        {
            throw new Exception("Please open HoloCure.");
        }

        return processes[0].MainWindowHandle;
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

    private static void PlayFishingGame(ref bool playing, ref int chain, in Stopwatch timeoutSw)
    {
        int right = _notes.Max(note => note.Right);
        int bottom = _notes.Max(note => note.Bottom);

        bool noteFound = false;
        ReadonlyImage targetArea = CaptureWindow(_windowHandle, _targetLeft, _targetTop, right, bottom);
        foreach (Note note in _notes)
        {
            if (targetArea.ContainsNote(note))
            {
                Console.WriteLine($"Pressing {note.Button}");
                InputUtils.SendKey(_windowHandle, note.Button);
                noteFound = true;
                break;
            }
        }

        if (noteFound)
        {
            timeoutSw.Restart();
        }
        else
        {
            CheckGameFisished(ref playing, ref chain);
        }
    }

    private static void CheckGameFisished(ref bool playing, ref int chain)
    {
        ReadonlyImage okArea = CaptureWindow(_windowHandle, _targetLeft - 63, _targetTop + 32, 11, 9);
        if (!okArea.CroppedEquals(_okImage))
        {
            return;
        }
        playing = false;

        // Check if caught sucessfully
        ReadonlyImage caughtArea = CaptureWindow(_windowHandle, _targetLeft - 100, _targetTop - 107, 12, 12);
        if (caughtArea.CroppedEquals(_cImage))
        {
            chain++;
        }
        else
        {
            Console.WriteLine($"Chain broke at {chain}.");
            chain = 0;
        }
    }

    private static void FindTargetArea()
    {
        ReadonlyImage screen = CaptureWindow(_windowHandle);
        (_targetLeft, _targetTop) = screen.Find(_targetImage);
        if (_targetLeft >= 0 && _targetTop >= 0)
        {
            Console.WriteLine($"Target area found: X={_targetLeft}, Y={_targetTop}");
        }
    }
}
