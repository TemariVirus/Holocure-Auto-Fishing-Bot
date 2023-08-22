using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using static WindowUtils;

// TODO: Use different target areas for different buttons

static class Images
{
    public static readonly Image2D circle = new Image2D("img/circle.png");
    public static readonly Image2D left = new Image2D("img/left.png");
    public static readonly Image2D right = new Image2D("img/right.png");
    public static readonly Image2D up = new Image2D("img/up.png");
    public static readonly Image2D down = new Image2D("img/down.png");
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
            { Images.circle, settings.TheButtons[0] },
            { Images.left, settings.TheButtons[2] },
            { Images.right, settings.TheButtons[3] },
            { Images.up, settings.TheButtons[4] },
            { Images.down, settings.TheButtons[5] },
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
        Stopwatch sw = Stopwatch.StartNew();
        Stopwatch note_timer = Stopwatch.StartNew();
        bool playing = false;
        while (true)
        {
            // Press enter twice to Start fishing game
            if (!playing)
            {
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
                continue;
            }

            capture_count++;
            if (sw.ElapsedMilliseconds >= 1000)
            {
                Console.WriteLine(
                    $"Captures per second: {capture_count / sw.Elapsed.TotalSeconds:F2}"
                );
                capture_count = 0;
                sw.Restart();
            }

            // Find note
            Image2D target_area = CaptureTargetArea();
            foreach (var pair in note_map)
            {
                Image2D img = pair.Key;
                string key = pair.Value;

                if (target_area.MaskedContains(img))
                {
                    Console.WriteLine($"Pressing {key}");
                    InputUtils.PressKey(hWnd, key);
                    // Wait until game updates to release key
                    do
                    {
                        target_area = CaptureTargetArea();
                    } while (target_area.MaskedContains(img));
                    InputUtils.ReleaseKey(hWnd, key);

                    break;
                }
            }

            if (note_timer.ElapsedMilliseconds >= 2000)
            {
                playing = false;
                note_timer.Restart();
            }
        }

        // Laptop on 125% scale
        Image2D CaptureTargetArea() => CaptureWindow(hWnd, 384, 280, 38, 21);
        // Desktop on 100% scale
        // Image2D CaptureTargetArea() => CaptureWindow(hWnd, 383, 273, 36, 21);
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
