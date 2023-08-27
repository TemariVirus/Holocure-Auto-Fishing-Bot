using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

// TODO: Check out OBS windows 10 capture method

internal readonly struct Settings
{
    public string[] Buttons { get; }
    public bool IsFullscreen { get; }

    public Settings(string[] buttons, bool fullscreen)
    {
        Buttons = buttons;
        IsFullscreen = fullscreen;
    }
}

static partial class Program
{
    [DllImport("user32")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    private static readonly ReadonlyImage _targetImage = new ReadonlyImage("img/target circle.png");
    private static readonly ReadonlyImage _okImage = new ReadonlyImage("img/ok.png");

    private static Note[] _notes;

    private static readonly Settings _settings;
    private static int _resolution;

    private static readonly IntPtr _windowHandle;
    private static ReadonlyImage _lastCapture = null;
    private static int _windowWidth = -1;
    private static int _windowHeight = -1;
    private static int _targetLeft = -1;
    private static int _targetTop = -1;

    private static readonly bool _jpMode =
        CultureInfo.CurrentCulture.ThreeLetterISOLanguageName.ToLower() == "jpn";
    private static bool _hardwareAccelerated = false;

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
                (_jpMode ? "KI-BAINDO: " : "Buttons: ")
                    + $"[{string.Join(", ", _settings.Buttons)}]"
            );

            _windowHandle = GetHolocureWindow();
            Console.WriteLine(
                _jpMode ? "HoloCure no mado ga mitukarimasita." : "Holocure window found."
            );

            if (_settings.IsFullscreen)
            {
                throw new Exception(
                    _jpMode
                        ? "FURUSUKURI-N wo OFU ni site kudasai."
                        : "Please turn off full screen."
                );
            }

            _resolution = GetHolocureResolution(GetWindowRectUnscaled());
            string res = new string[] { "640 x 360", "1280 x 720", "1920 x 1080", "2560 x 1440" }[
                _resolution - 1
            ];
            Console.WriteLine(
                _jpMode
                    ? $"HoloCure no kaizoudo ga mitukarimasita: {res}"
                    : $"Detected HoloCure resolution: {res}"
            );

            _notes = new Note[]
            {
                new Note(new ReadonlyImage("img/circle.png"), _settings.Buttons[0], 5, 15, 30, 32),
                new Note(new ReadonlyImage("img/left.png"), _settings.Buttons[2], 2, 14, 32, 33),
                new Note(new ReadonlyImage("img/right.png"), _settings.Buttons[3], 2, 14, 32, 33),
                new Note(new ReadonlyImage("img/up.png"), _settings.Buttons[4], 3, 13, 31, 34),
                new Note(new ReadonlyImage("img/down.png"), _settings.Buttons[5], 3, 13, 31, 34)
            };

            Console.WriteLine();
        }
        catch (Exception e)
        {
            // Manually call exception handler as it has not been set yet
            ExceptionHandler(null, new UnhandledExceptionEventArgs(e, false));
        }
    }

    private static void Main()
    {
        Console.WriteLine(_jpMode ? "BOTTO wo kidou simasita." : "Bot started.");
        Console.WriteLine(_jpMode ? "CTRL + C wo osite teisi." : "Press ctrl + C to stop.");
        if (_hardwareAccelerated) { }
        else
        {
            Console.WriteLine(
                _jpMode
                    ? "MINIGE-MU ga gamengai ni denai you ni site kudasai (hoka no mado ni kaburarete mo daizyoubu desu)."
                    : "Please ensure that the minigame is within view at all times (you can still have other windows on top of it)."
            );
        }
        Console.WriteLine();

        // Start bot loop
        bool playing = false;
        int cycleCount = 0;
        Stopwatch perfSw = Stopwatch.StartNew();
        Stopwatch timeoutSw = Stopwatch.StartNew();
        Stopwatch waitSw = Stopwatch.StartNew();
        while (true)
        {
            // Invalidate capture
            _lastCapture = null;

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
            while (cycleCount / perfSw.Elapsed.TotalSeconds > 69)
            {
                Thread.Sleep(1);
            }

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
                        ? "20byou inai ni NO-TU ga mitukarimasen desita. kore ga tudukeru baai ha, HoloCure no kaizoudo wo tiisaku site kudasai. MINIGE-MU wo saikidou simasu."
                        : "No notes detected in 20 seconds. If this continues, try setting HoloCure to a smaller resolution. Restarting minigame."
                );
                if (!_hardwareAccelerated)
                {
                    ActivateDirectXWorkaround();
                }

                playing = false;
                timeoutSw.Restart();
            }
        }
    }

    private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
        Console.WriteLine($"\n{args.ExceptionObject}\n");
        Console.WriteLine(_jpMode ? "KI- wo osite syuuryou." : "Press any key to exit.");
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
                _jpMode ? "settei FAIRU ga mitukarimasen desita." : "Settings file not found."
            );
        }

        string jsonText = File.ReadAllText(filePath);

        Match buttonsMatch = Regex.Match(
            jsonText,
            @"""theButtons"":\[(""\w+""(,""\w+""){5})\]",
            RegexOptions.IgnoreCase
        );
        if (!buttonsMatch.Success)
        {
            throw new Exception(
                _jpMode
                    ? "settei FAIRU ni KI-BAINDO ga mitukarimasen desita."
                    : "settings.json was not formatted correctly. Could not find key bindings."
            );
        }
        string[] buttons = buttonsMatch.Groups[1].Value
            .Split(',')
            .Select(s => s.Trim(' ', '"'))
            .ToArray();

        Match fullscreenMatch = Regex.Match(
            jsonText,
            @"""fullscreen"":(\d+\.\d+|\d+|true|false)",
            RegexOptions.IgnoreCase
        );
        if (!fullscreenMatch.Success)
        {
            throw new Exception(
                _jpMode
                    ? "settei FAIRU ni FURUSUKURI-N settei ga mitukarimasen desita."
                    : "settings.json was not formatted correctly. Could not find fullscreen setting."
            );
        }
        Console.WriteLine(
            _jpMode
                ? $"furusukuri-n settei: {fullscreenMatch.Groups[1].Value}"
                : $"Full screen setting: {fullscreenMatch.Groups[1].Value}"
        );
        bool fullscreen;
        bool isFullscreenNum = double.TryParse(
            fullscreenMatch.Groups[1].Value,
            out double fullscreenNum
        );
        if (isFullscreenNum)
        {
            fullscreen = fullscreenNum != 0.0;
        }
        else
        {
            fullscreen = bool.Parse(fullscreenMatch.Groups[1].Value);
        }

        return new Settings(buttons, fullscreen);
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
                    ? $"TA-GETTO ga mitukarimasita: X={_targetLeft * _resolution}, Y={_targetTop * _resolution}"
                    : $"Target area found: X={_targetLeft * _resolution}, Y={_targetTop * _resolution}"
            );
        }
    }

    private static void ActivateDirectXWorkaround()
    {
        _hardwareAccelerated = true;

        _notes = new Note[]
        {
            new Note(new ReadonlyImage("img/circle.png"), _settings.Buttons[0], 3, 15, 32, 32),
            new Note(new ReadonlyImage("img/left.png"), _settings.Buttons[2], 1, 14, 32, 33),
            new Note(new ReadonlyImage("img/right.png"), _settings.Buttons[3], 1, 14, 32, 33),
            new Note(new ReadonlyImage("img/up.png"), _settings.Buttons[4], 2, 13, 32, 34),
            new Note(new ReadonlyImage("img/down.png"), _settings.Buttons[5], 2, 13, 32, 34)
        };

        Console.WriteLine(
            _jpMode
                ? "cyuui: HoloCure no mado ha HA-DOUXEA AKUSERARE-SYON ni sareteiru kanousei ga arimasu. ippan no SUKURI-NSYOTTO houhou ni tayorimasu. BOTTO ha osoku narimasu. HoloCure no mado ga kaburarenai you ni site kudasai."
                : "Note: The HoloCure window may be hardware accelerated. Resorting to taking normal screenshots. The bot will be slower, and please make sure that the HoloCure window always stays on top."
        );
        Console.WriteLine(
            _jpMode
                ? "syousai zyouhou: https://support.microsoft.com/ja-jp/windows/3f006843-2c7e-4ed0-9a5e-f9389e535952"
                : "More info: https://support.microsoft.com/windows/3f006843-2c7e-4ed0-9a5e-f9389e535952"
        );
        Console.WriteLine();
        Console.WriteLine(
            _jpMode
                ? "mosimo haikei de hatarakitai baai ha, ika no sizi wo tamesite kudasai:"
                : "If you want to run HoloCure in the background instead, you can try these steps:"
        );
        Console.WriteLine(
            _jpMode
                ? "1. BOTTO wo kanrisya tosite zikkou site mite kudasai. mukou no baai ha, SUTEPPU 2 ni susunde kudasai."
                : "1. Re-run the bot as administrator. If that doesn't work, proceed to step 2."
        );
        Console.WriteLine(
            _jpMode
                ? "2. settei de, \"SISUTEMU\" > \"hyouzi\" > \"GURAFIKKUSU\" > \"kitei no GURAFIKKUSU settei wo henkou suru\" wo sentaku site kudasai."
                : "2. Open settings, and navigate to System > Display > Graphics > Default graphics settings."
        );
        Console.WriteLine(
            _jpMode
                ? "3. ika no settei wo dekiru kagiri OFU ni site kudasai:"
                    + "\n\ta. UXINDOU GE-MU no saitekika"
                    + "\n\tb. kahen RIFURESSYU RE-TO"
                    + "\n\tc. zidou HDR"
                : "3. Disable the following (if the setting(s) can be found):"
                    + "\n\ta. Optimizations for windowed games"
                    + "\n\tb. Variable refresh rate"
                    + "\n\tc. Auto HDR"
        );
        Console.WriteLine(
            _jpMode
                ? "4. HoloCure wo saikidou site, mou itido tamesite kudasai. mukou no baai ha, mousi wake gozaimasen desita :("
                : "4. Restart Holocure and try again. If that doesn't work, then I'm out of tricks :("
        );

        // Bring HoloCure window to front
        SetForegroundWindow(_windowHandle);
    }
}
