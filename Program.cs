using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

internal readonly struct Settings
{
    public string[] Buttons { get; }
    public int Resolution { get; }
    public bool IsFullscreen { get; }

    public Settings(string[] buttons, int resolution, bool fullscreen)
    {
        Buttons = buttons;
        Resolution = resolution;
        IsFullscreen = fullscreen;
    }
}

static class Program
{
    private static readonly ReadonlyImage _targetImage = new ReadonlyImage("img/target circle.png");
    private static readonly ReadonlyImage _okImage = new ReadonlyImage("img/ok.png");

    private static readonly Note[] _notes;

    private static readonly Settings _settings;

    private static readonly IntPtr _windowHandle;
    private static int _targetLeft = -1;
    private static int _targetTop = -1;

    private static readonly bool _jpMode =
        CultureInfo.CurrentCulture.ThreeLetterISOLanguageName.ToLower() == "jpn";

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
            Console.WriteLine(
                _jpMode ? "HoloCure settei ga mitukarimasita." : "Holocure settings found."
            );
            Console.WriteLine(
                (_jpMode ? "ki-baindo: " : "Buttons: ")
                    + $"[{string.Join(", ", _settings.Buttons)}]"
            );

            _windowHandle = GetHolocureWindow();
            Console.WriteLine(
                _jpMode ? "HoloCure no mado ga mitukarimasita." : "Holocure window found."
            );

            if (_settings.IsFullscreen)
            {
                throw new Exception(
                    _jpMode ? "furusukuri-n wo ofu ni site kudasai." : "Please turn off fullscreen."
                );
            }

            if (_settings.Resolution < 1 || _settings.Resolution > 4)
            {
                Console.WriteLine(
                    _jpMode
                        ? "HoloCure no kaizoudo settei ni ayamari ga arimasita. 1280 x 720 to soutei simasu."
                        : "Found invalid HoloCure resolution setting. Assuming 1280 x 720."
                );
                _settings = new Settings(_settings.Buttons, 2, _settings.IsFullscreen);
            }
            else
            {
                string res = new string[]
                {
                    "640 x 360",
                    "1280 x 720",
                    "1920 x 1080",
                    "2560 x 1440"
                }[_settings.Resolution - 1];
                Console.WriteLine(
                    _jpMode
                        ? $"HoloCure no kaizoudo ga mitukarimasita: {res}"
                        : $"Detected HoloCure resolution: {res}"
                );
            }
            if (_settings.Resolution == 4)
            {
                Console.WriteLine(
                    _jpMode
                        ? "botto ha 2560 x 1440 no kaizoudo deha hataraku koto ga dekimasen kanousei ga arimasu."
                        : "Bot may not completely work on 2560 x 1440 resolution."
                );
            }

            _notes = new Note[]
            {
                new Note(new ReadonlyImage("img/circle.png"), _settings.Buttons[0], 5, 15, 30, 32),
                new Note(new ReadonlyImage("img/left.png"), _settings.Buttons[2], 2, 14, 32, 33),
                new Note(new ReadonlyImage("img/right.png"), _settings.Buttons[3], 2, 14, 32, 33),
                new Note(new ReadonlyImage("img/up.png"), _settings.Buttons[4], 3, 13, 31, 34),
                new Note(new ReadonlyImage("img/down.png"), _settings.Buttons[5], 3, 13, 31, 34)
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
        Console.WriteLine(_jpMode ? "botto wo kidou simasita" : "Bot started.");
        Console.WriteLine(
            _jpMode
                ? "minige-mu ga gamengai ni denai you ni site kudasai (hoka no mado ni kaburarete mo daijoubu desu).\n"
                : "Please ensure that the minigame is within view at all times (you can still have other windows on top of it).\n"
        );
        // Start bot loop
        bool playing = false;
        int cycleCount = 0;
        Stopwatch perfSw = Stopwatch.StartNew();
        Stopwatch timeoutSw = Stopwatch.StartNew();
        Stopwatch waitSw = Stopwatch.StartNew();
        while (true)
        {
            if (!playing)
            {
                perfSw.Stop();

                StartFishingGame();
                timeoutSw.Restart();
                waitSw.Restart();
                playing = true;

                perfSw.Start();
                continue;
            }

            if (_targetLeft >= 0 && _targetTop >= 0)
            {
                PlayFishingGame(ref playing, in timeoutSw);
            }
            else
            {
                FindTargetArea();
            }

            // Aim for a little over 60 cycles per second to match framerate
            // while (cycleCount / perfSw.Elapsed.TotalSeconds > 69)
            // {
            //     Thread.Sleep(1);
            // }

            // Print cycles per second
            cycleCount++;
            if (perfSw.ElapsedMilliseconds >= 4545)
            {
                Console.WriteLine(
                    $"Cycles per second: {cycleCount / perfSw.Elapsed.TotalSeconds:F2}"
                );
                cycleCount = 0;
                perfSw.Restart();
            }

            // If no notes for too long, restart
            if (timeoutSw.ElapsedMilliseconds >= 20_000)
            {
                Console.WriteLine(
                    _jpMode
                        ? "20byou inai ni no-tu ga mitukarimasen desita. minige-mu wo saikidou siyou to simasu."
                        : "No notes detected in 20 seconds. Attempting to restart minigame."
                );
                playing = false;
                timeoutSw.Restart();
            }
        }
    }

    private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
        Console.WriteLine(
            $"\n{args.ExceptionObject}\n\n"
                + (_jpMode ? "ki- wo osite syuuryou." : "Press any key to exit.")
        );
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
                _jpMode ? "settei fairu ga mitukarimasen desita." : "Settings file not found."
            );
        }

        string jsonText = File.ReadAllText(filePath);

        Match buttonsMatch = Regex.Match(jsonText, @"""theButtons"":\[(""\w+""(,""\w+""){5})\]");
        if (!buttonsMatch.Success)
        {
            throw new Exception(
                _jpMode
                    ? "settei fairu ni ki-baindo ga mitukarimasen desita."
                    : "settings.json was not formatted correctly. Could not find key bindings."
            );
        }
        string[] buttons = buttonsMatch.Groups[1].Value
            .Split(',')
            .Select(s => s.Trim(' ', '"'))
            .ToArray();

        Match resMatch = Regex.Match(jsonText, @"""Resolution"":(\d+\.\d+|\d+)");
        if (!resMatch.Success)
        {
            throw new Exception(
                _jpMode
                    ? "settei fairu ni kaizoudo ga mitukarimasen desita."
                    : "settings.json was not formatted correctly. Could not find resolution setting."
            );
        }
        double resolution = double.Parse(resMatch.Groups[1].Value);

        Match fullscreenMatch = Regex.Match(jsonText, @"""fullscreen"":(\d+\.\d+|\d+|true|false)");
        if (!fullscreenMatch.Success)
        {
            throw new Exception(
                _jpMode
                    ? "settei fairu ni furusukuri-n settei ga mitukarimasen desita."
                    : "settings.json was not formatted correctly. Could not find fullscreen setting."
            );
        }
        bool isFullscreenNum = double.TryParse(
            fullscreenMatch.Groups[1].Value,
            out double fullscreenNum
        );
        bool fullscreen = isFullscreenNum
            ? fullscreenNum != 0.0
            : bool.Parse(fullscreenMatch.Groups[1].Value);

        return new Settings(buttons, (int)resolution + 1, fullscreen);
    }

    private static IntPtr GetHolocureWindow()
    {
        Process[] processes = Process.GetProcessesByName("holocure");
        if (processes.Length <= 0)
        {
            throw new Exception(
                _jpMode ? "HoloCure wo kidou site kudasai." : "Please open HoloCure."
            );
        }

        return processes[0].MainWindowHandle;
    }

    private static ReadonlyImage CaptureHolocureWindow(
        int left = 0,
        int top = 0,
        int width = -1,
        int height = -1
    )
    {
        left *= _settings.Resolution;
        top *= _settings.Resolution;
        width *= _settings.Resolution;
        height *= _settings.Resolution;

        return WindowUtils
            .CaptureWindow(_windowHandle, left, top, width, height)
            .Shrink(_settings.Resolution);
    }

    private static void StartFishingGame()
    {
        const string KEY = "ENTER";

        for (int i = 0; i < 2; i++)
        {
            Console.WriteLine($"Pressing {KEY}");
            InputUtils.SendKey(_windowHandle, KEY);
            Thread.Sleep(150);
        }
    }

    private static void PlayFishingGame(ref bool playing, in Stopwatch timeoutSw)
    {
        int right = _notes.Max(note => note.Right);
        int bottom = _notes.Max(note => note.Bottom);

        bool noteFound = false;
        ReadonlyImage targetArea = CaptureHolocureWindow(_targetLeft, _targetTop, right, bottom);
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
            CheckGameFisished(ref playing);
        }
    }

    private static void CheckGameFisished(ref bool playing)
    {
        ReadonlyImage okArea = CaptureHolocureWindow(_targetLeft - 63, _targetTop + 32, 11, 9);
        if (!okArea.CroppedEquals(_okImage))
        {
            return;
        }
        playing = false;
    }

    private static void FindTargetArea()
    {
        ReadonlyImage screen = CaptureHolocureWindow();
        (_targetLeft, _targetTop) = screen.Find(_targetImage);
        if (_targetLeft >= 0 && _targetTop >= 0)
        {
            Console.WriteLine(
                _jpMode
                    ? $"ta-getto ga mitukarimasita: X={_targetLeft * _settings.Resolution}, Y={_targetTop * _settings.Resolution}"
                    : $"Target area found: X={_targetLeft * _settings.Resolution}, Y={_targetTop * _settings.Resolution}"
            );
        }
    }
}
