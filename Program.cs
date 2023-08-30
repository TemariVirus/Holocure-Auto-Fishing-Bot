using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

// TODO: Check out OBS windows 10 capture method

namespace Holocure_Auto_Fishing_Bot
{
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
        #region DLL Imports
        [DllImport("user32")]
        private static extern bool SetForegroundWindow(IntPtr hwnd);
        #endregion

        #region Fields
        private static readonly ReadonlyImage _targetImage = new ReadonlyImage(
            "img/target circle.png"
        );
        private static readonly ReadonlyImage _okImage = new ReadonlyImage("img/ok.png");

        private static Note[] _notes;

        private static readonly Settings _settings;
        private static int _resolution;

        private static readonly IntPtr _windowHandle;
        private static ReadonlyImage _lastSS = null;
        private static int _windowWidth = -1;
        private static int _windowHeight = -1;
        private static int _targetLeft = -1;
        private static int _targetTop = -1;

        private static readonly bool _isLocaleJp =
            CultureInfo.CurrentCulture.ThreeLetterISOLanguageName.ToLower() == "jpn";
        private static bool _hardwareAccelerated = false;

        private static readonly string _optionsMsg = _isLocaleJp
            ? "\"O\" KI- wo oshite settei wo hiraku"
            : "Press 'o' key for options";
        #endregion

        static Program()
        {
            // Keep console open on crash
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
                ExceptionHandler
            );

            // Setup
            try
            {
                _settings = GetSettings();
                Console.WriteLine(
                    _isLocaleJp ? "HoloCure settei ga mitukarimasita." : "Holocure settings found."
                );
                PrintLine(
                    (_isLocaleJp ? "KI-BAINDO: " : "Buttons: ")
                        + $"[{string.Join(", ", _settings.Buttons)}]"
                );

                _windowHandle = GetWindow();
                Console.WriteLine(
                    _isLocaleJp ? "HoloCure no mado ga mitukarimasita." : "Holocure window found."
                );

                _resolution = GetHolocureResolution(GetWindowRectUnscaled());

                _notes = new Note[]
                {
                    new Note(
                        new ReadonlyImage("img/circle.png"),
                        _settings.Buttons[0],
                        5,
                        15,
                        30,
                        32
                    ),
                    new Note(
                        new ReadonlyImage("img/left.png"),
                        _settings.Buttons[2],
                        2,
                        14,
                        32,
                        33
                    ),
                    new Note(
                        new ReadonlyImage("img/right.png"),
                        _settings.Buttons[3],
                        2,
                        14,
                        32,
                        33
                    ),
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

        private static void Main(string[] args)
        {
            PrintLine(_isLocaleJp ? "BOTTO wo kidou simasita." : "Bot started.");
            PrintLine(_isLocaleJp ? "CTRL + C wo osite teisi." : "Press ctrl + C to stop.");
            if (!_hardwareAccelerated)
            {
                PrintLine(
                    _isLocaleJp
                        ? "MINIGE-MU ga gamengai ni denai you ni site kudasai (hoka no mado ni kaburarete mo daizyoubu desu)."
                        : "Please ensure that the minigame is within view at all times (you can still have other windows on top of it)."
                );
            }
            PrintLine();

            // Start bot loop
            bool playing = false;
            int cycleCount = 0;
            Stopwatch perfSw = Stopwatch.StartNew();
            Stopwatch timeoutSw = Stopwatch.StartNew();
            while (true)
            {
                HandleKeys();

                InvalidateLastSS();

                // Restart minigame if not playing
                if (!playing)
                {
                    perfSw.Stop();

                    StartGame();
                    timeoutSw.Restart();
                    playing = true;

                    perfSw.Start();
                    continue;
                }

                // Find target area if not found yet
                if (_targetLeft < 0 || _targetTop < 0)
                {
                    if (FindTarget())
                    {
                        timeoutSw.Restart();
                    }
                }

                // Play game
                bool noteFound = PlayGame();
                if (noteFound)
                {
                    timeoutSw.Restart();
                }
                else
                {
                    playing = !IsGameFisished();
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
                    PrintLine($"Cycles per second: {cycleCount / perfSw.Elapsed.TotalSeconds:F2}");
                    cycleCount = 0;
                    perfSw.Restart();
                }

                // If no notes for too long, restart
                if (timeoutSw.ElapsedMilliseconds >= 20_000)
                {
                    PrintLine(
                        _isLocaleJp
                            ? "20byou inai ni NO-TU ga mitukarimasen desita. kore ga tudukeru baai ha, HoloCure no kaizoudo wo tiisaku site kudasai. MINIGE-MU wo saikidou simasu."
                            : "No notes detected in 20 seconds. If this continues, try setting HoloCure to a smaller resolution. Restarting minigame."
                    );
                    ActivateDirectXWorkaround();

                    playing = false;
                    timeoutSw.Restart();
                }
            }
        }

        private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Console.WriteLine($"\n{args.ExceptionObject}\n");
            Console.WriteLine(_isLocaleJp ? "KI- wo osite syuuryou." : "Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
        }

        private static void PrintLine()
        {
            PrintLine("");
        }

        private static void PrintLine(object obj)
        {
            Console.WriteLine(obj);
            return;
            Console.CursorTop = Math.Max(0, Console.CursorTop - 2);
            Console.WriteLine(obj.ToString().PadRight(_optionsMsg.Length));
            Console.WriteLine("".PadRight(_optionsMsg.Length));
            Console.WriteLine(_optionsMsg);
        }

        private static void HandleKeys()
        {
            return;
            while (Console.KeyAvailable)
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                if (info.KeyChar == 'O' || info.KeyChar == 'o')
                {
                    SettingsScreen();
                }
            }
        }

        private static void SettingsScreen()
        {
            Console.CursorTop = Math.Max(0, Console.CursorTop - 1);
            Console.Write("".PadRight(_optionsMsg.Length));
            Console.CursorLeft = 0;
        }

        private static Settings GetSettings()
        {
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string filePath = $"{userPath}\\AppData\\Local\\HoloCure\\settings.json";
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    _isLocaleJp
                        ? "settei FAIRU ga mitukarimasen desita."
                        : "Settings file not found."
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
                    _isLocaleJp
                        ? "settei FAIRU ni KI-BAINDO ga mitukarimasen desita."
                        : "settings.json was not formatted correctly. Could not find key bindings."
                );
            }
            string buttonsStr = buttonsMatch.Groups[1].Value.Trim();
            string[] buttons = buttonsStr.Split(',').Select(s => s.Trim(' ', '"')).ToArray();

            Match fullscreenMatch = Regex.Match(
                jsonText,
                @"""fullscreen"":(\d+\.\d+|\d+|true|false)",
                RegexOptions.IgnoreCase
            );
            if (!fullscreenMatch.Success)
            {
                throw new Exception(
                    _isLocaleJp
                        ? "settei FAIRU ni FURUSUKURI-N settei ga mitukarimasen desita."
                        : "settings.json was not formatted correctly. Could not find fullscreen setting."
                );
            }
            string fullscreenStr = fullscreenMatch.Groups[1].Value.Trim();
            PrintLine(
                _isLocaleJp
                    ? $"furusukuri-n settei: {fullscreenStr}"
                    : $"Full screen setting: {fullscreenStr}"
            );
            bool fullscreen;
            bool isFullscreenNum = double.TryParse(
                fullscreenStr,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double fullscreenNum
            );
            fullscreen = isFullscreenNum ? fullscreenNum != 0.0 : bool.Parse(fullscreenStr);

            return new Settings(buttons, fullscreen);
        }

        private static IntPtr GetWindow()
        {
            Process[] processes = Process.GetProcessesByName("holocure");
            if (processes.Length <= 0)
            {
                throw new Exception(
                    _isLocaleJp ? "HoloCure wo kidou site kudasai." : "Please open HoloCure."
                );
            }

            return processes[0].MainWindowHandle;
        }

        private static void StartGame()
        {
            const string KEY = "ENTER";

            for (int i = 0; i < 2; i++)
            {
                PrintLine($"Pressing {KEY}");
                InputUtils.SendKeyPress(_windowHandle, KEY);
                Thread.Sleep(150);
            }
        }

        private static bool PlayGame()
        {
            int right = _notes.Max(note => note.Right);
            int bottom = _notes.Max(note => note.Bottom);

            ReadonlyImage targetArea = CaptureHolocureWindow(
                _targetLeft,
                _targetTop,
                right,
                bottom
            );
            foreach (Note note in _notes)
            {
                if (targetArea.ContainsNote(note))
                {
                    PrintLine($"Pressing {note.Button}");
                    InputUtils.SendKeyPress(_windowHandle, note.Button);
                    return true;
                }
            }

            return false;
        }

        private static bool IsGameFisished()
        {
            ReadonlyImage okArea = CaptureHolocureWindow(_targetLeft - 73, _targetTop + 17, 31, 39);
            return okArea.Find(_okImage) != (-1, -1);
        }

        private static bool FindTarget()
        {
            ReadonlyImage screen = CaptureHolocureWindow();
            (_targetLeft, _targetTop) = screen.Find(_targetImage);
            if (_targetLeft >= 0 && _targetTop >= 0)
            {
                PrintLine(
                    _isLocaleJp
                        ? $"TA-GETTO ga mitukarimasita: X={_targetLeft * _resolution}, Y={_targetTop * _resolution}"
                        : $"Target area found: X={_targetLeft * _resolution}, Y={_targetTop * _resolution}"
                );
            }

            return _targetLeft == -1 && _targetTop == -1;
        }

        private static void InvalidateTargetPos()
        {
            _targetLeft = -1;
            _targetTop = -1;
        }

        private static void ActivateDirectXWorkaround()
        {
            if (_hardwareAccelerated)
            {
                return;
            }

            _hardwareAccelerated = true;

            _notes = new Note[]
            {
                new Note(new ReadonlyImage("img/circle.png"), _settings.Buttons[0], 3, 15, 32, 32),
                new Note(new ReadonlyImage("img/left.png"), _settings.Buttons[2], 1, 14, 32, 33),
                new Note(new ReadonlyImage("img/right.png"), _settings.Buttons[3], 1, 14, 32, 33),
                new Note(new ReadonlyImage("img/up.png"), _settings.Buttons[4], 2, 13, 32, 34),
                new Note(new ReadonlyImage("img/down.png"), _settings.Buttons[5], 2, 13, 32, 34)
            };

            PrintLine(
                _isLocaleJp
                    ? "cyuui: HoloCure no mado ha HA-DOUXEA AKUSERARE-SYON ni sareteiru kanousei ga arimasu. ippan no SUKURI-NSYOTTO houhou ni tayorimasu. BOTTO ha osoku narimasu. HoloCure no mado ga kaburarenai you ni site kudasai."
                    : "Note: The HoloCure window may be hardware accelerated. Resorting to taking normal screenshots. The bot will be slower, and please make sure that the HoloCure window always stays on top."
            );
            PrintLine(
                _isLocaleJp
                    ? "syousai zyouhou: https://support.microsoft.com/ja-jp/windows/3f006843-2c7e-4ed0-9a5e-f9389e535952"
                    : "More info: https://support.microsoft.com/windows/3f006843-2c7e-4ed0-9a5e-f9389e535952"
            );
            PrintLine();
            PrintLine(
                _isLocaleJp
                    ? "mosimo haikei de hatarakitai baai ha, ika no sizi wo tamesite kudasai:"
                    : "If you want to run HoloCure in the background instead, you can try these steps:"
            );
            PrintLine(
                _isLocaleJp
                    ? "1. BOTTO wo kanrisya tosite zikkou site mite kudasai. mukou no baai ha, SUTEPPU 2 ni susunde kudasai."
                    : "1. Re-run the bot as administrator. If that doesn't work, proceed to step 2."
            );
            PrintLine(
                _isLocaleJp
                    ? "2. settei de, \"SISUTEMU\" > \"hyouzi\" > \"GURAFIKKUSU\" > \"kitei no GURAFIKKUSU settei wo henkou suru\" wo sentaku site kudasai."
                    : "2. Open settings, and navigate to System > Display > Graphics > Default graphics settings."
            );
            PrintLine(
                _isLocaleJp
                    ? "3. ika no settei wo dekiru kagiri OFU ni site kudasai:"
                        + "\n    a. UXINDOU GE-MU no saitekika"
                        + "\n    b. kahen RIFURESSYU RE-TO"
                        + "\n    c. zidou HDR"
                    : "3. Disable the following (if the setting(s) can be found):"
                        + "\n    a. Optimizations for windowed games"
                        + "\n    b. Variable refresh rate"
                        + "\n    c. Auto HDR"
            );
            PrintLine(
                _isLocaleJp
                    ? "4. HoloCure wo saikidou site, mou itido tamesite kudasai. mukou no baai ha, mousi wake gozaimasen desita :("
                    : "4. Restart Holocure and try again. If that doesn't work, then I'm out of tricks :("
            );

            // Bring HoloCure window to front
            SetForegroundWindow(_windowHandle);
        }
    }
}
