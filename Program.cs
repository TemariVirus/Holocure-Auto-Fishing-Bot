using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using static WindowUtils;

// TODO:
// - Use different target areas for different buttons
// - Find target circle to align the target area

static class Images
{
    // public static readonly Image2D Target = new Image2D("img/target circle.png");

    public static readonly Image2D Circle = new Image2D("img/circle.png");
    public static readonly Image2D Left = new Image2D("img/left.png");
    public static readonly Image2D Right = new Image2D("img/right.png");
    public static readonly Image2D Up = new Image2D("img/up.png");
    public static readonly Image2D Down = new Image2D("img/down.png");
}

static class Program
{
    private struct Settings
    {
        public double Resolution { get; set; }
        public string[] TheButtons { get; set; }
        public bool Fullscreen { get; set; }
    }

    static void Main()
    {
        // Keep console open on crash
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
            CrashHandler
        );

        Settings settings = GetHolocureSettings();
        Console.WriteLine("Holocure settings found.");

        var note_map = new Dictionary<Image2D, string>
        {
            { Images.Circle, settings.TheButtons[0] },
            { Images.Left, settings.TheButtons[2] },
            { Images.Right, settings.TheButtons[3] },
            { Images.Up, settings.TheButtons[4] },
            { Images.Down, settings.TheButtons[5] },
        };

        IntPtr hWnd = GetHolocureWindow();
        Console.WriteLine("Holocure window found.");

        if (settings.Fullscreen)
        {
            throw new Exception("Please turn off fullscreen.");
        }
        if (settings.Resolution != 0.0)
        {
            throw new Exception("Please set resolution to 640x360.");
        }

        Console.WriteLine("Bot started.");
        int capture_count = 0;
        Stopwatch perf_sw = Stopwatch.StartNew();
        Stopwatch note_timer = Stopwatch.StartNew();
        bool playing = false;
        while (true)
        {
            // Press confirm button twice to start fishing game
            if (!playing)
            {
                perf_sw.Stop();
                string key = settings.TheButtons[0];

                Console.WriteLine($"Pressing {key}");
                InputUtils.PressKey(hWnd, key);
                Thread.Sleep(33);
                InputUtils.ReleaseKey(hWnd, key);

                Thread.Sleep(200);

                Console.WriteLine($"Pressing {key}");
                InputUtils.PressKey(hWnd, key);
                Thread.Sleep(33);
                InputUtils.ReleaseKey(hWnd, key);

                note_timer.Restart();
                playing = true;
                perf_sw.Start();
                continue;
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

            // Find note
            Image2D target_area = CaptureTargetArea();
            foreach (var pair in note_map)
            {
                Image2D img = pair.Key;
                string key = pair.Value;

                if (target_area.Contains(img))
                {
                    Console.WriteLine($"Pressing {key}");
                    InputUtils.PressKey(hWnd, key);
                    // Wait until game updates to release key
                    do
                    {
                        Thread.Sleep(1);
                        target_area = CaptureTargetArea();
                    } while (target_area.Contains(img));
                    InputUtils.ReleaseKey(hWnd, key);

                    break;
                }
            }

            // If no notes for too long, restart
            if (note_timer.ElapsedMilliseconds >= 1200)
            {
                playing = false;
                note_timer.Restart();
            }

            // Aim for a little over 60 captures per second to match framerate
            while (capture_count / perf_sw.Elapsed.TotalSeconds > 69)
            {
                Thread.Sleep(1);
            }
        }

        // Laptop on 125% scale
        Image2D CaptureTargetArea() => CaptureWindow(hWnd, 387, 280, 42, 21);
        // Desktop on 100% scale
        // Image2D CaptureTargetArea() => CaptureWindow(hWnd, 388, 273, 40, 21);
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
}
