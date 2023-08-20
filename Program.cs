using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

static class Images
{
    public static Bitmap circle_mod = new Bitmap("img/as2.png");

    public static Bitmap circle = new Bitmap("img/circle.png");
    public static Bitmap left = new Bitmap("img/left.png");
    public static Bitmap right = new Bitmap("img/right.png");
    public static Bitmap up = new Bitmap("img/up.png");
    public static Bitmap down = new Bitmap("img/down.png");

    public static Bitmap ok = new Bitmap("img/ok.png");
}

static class Program
{
    [DllImport("user32.dll")]
    static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    static extern uint BitBlt(
        IntPtr hdcDest,
        int xDest,
        int yDest,
        int wDest,
        int hDest,
        IntPtr hdcSource,
        int xSrc,
        int ySrc,
        uint rop
    );

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    static void Main()
    {
        IntPtr hWnd = GetHolocureWindow();
        var (left, top, width, height) = Ensure640x360(hWnd);

        // Align to target circle thingy
        left += 377;
        top += 239;
        Stopwatch sw = Stopwatch.StartNew();
        while (true)
        {
            Bitmap target_area = CaptureWindow(hWnd, left, top, 32, 27);
            if (target_area.MaskedContains(Images.circle, Images.circle))
            {
                Console.WriteLine($"Found circle after {sw.Elapsed.TotalSeconds} seconds.");
                break;
            }
            else
            {
                Console.WriteLine(
                    $"Circle not found after {sw.Elapsed.TotalSeconds} seconds. Retrying."
                );
            }
            Thread.Sleep(33);
        }
    }

    static IntPtr GetHolocureWindow()
    {
        Stopwatch sw = Stopwatch.StartNew();

        while (true)
        {
            if (sw.Elapsed.Minutes >= 5)
            {
                throw new TimeoutException("Holocure window not found after 5 minutes.");
            }

            Process[] processes = Process.GetProcessesByName("holocure");
            if (processes.Length > 0)
            {
                return processes[0].MainWindowHandle;
            }
            Console.WriteLine("Please open HoloCure. Retrying in 1 second.");

            // Try once per second
            Thread.Sleep(1000);
        }
    }

    static (int left, int top, int width, int height) Ensure640x360(IntPtr hWnd)
    {
        while (true)
        {
            Rect rect = new Rect();
            GetWindowRect(hWnd, ref rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (
                // Check if window is minimised
                rect.Right < 0
                || rect.Bottom < 0
                // Check if resolution is wrong
                || width < 640
                || width >= 1280
                || height < 360
                || height >= 720
            )
            {
                Console.WriteLine(
                    "Please change HoloCure resolution to 640x360 and ensure it is not misimised. Retrying in 1 second."
                );
            }
            else
            {
                return ((width - 640) / 2, 31, 640, 360);
            }
            Thread.Sleep(1000);
        }
    }

    static Bitmap CaptureWindow(
        IntPtr hWnd,
        int left = 0,
        int top = 0,
        int width = -1,
        int height = -1
    )
    {
        Rect rect = new Rect();
        GetWindowRect(hWnd, ref rect);
        rect.Left += left;
        rect.Top += top;

        if (width < 0)
        {
            width = rect.Right - rect.Left;
        }
        if (height < 0)
        {
            height = rect.Bottom - rect.Top;
        }

        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        Graphics graphics = Graphics.FromImage(bmp);
        IntPtr hdcBitmap = graphics.GetHdc();

        IntPtr hdcWindow = GetWindowDC(hWnd);

        BitBlt(hdcBitmap, 0, 0, width, height, hdcWindow, left, top, 0x00CC0020); // SRCCOPY

        graphics.ReleaseHdc(hdcBitmap);
        ReleaseDC(hWnd, hdcWindow);

        return bmp;
    }

    // Check that colors are close enough instead of exactly the same
    static bool MaskedEquals(this Bitmap self, Bitmap other, Bitmap mask, int left, int top)
    {
        for (int i = 0; i < other.Width; i++)
        {
            for (int j = 0; j < other.Height; j++)
            {
                if (mask.GetPixel(i, j).A == 0)
                {
                    continue;
                }

                if (other.GetPixel(i, j) != self.GetPixel(left + i, top + j))
                {
                    return false;
                }
            }
        }
        return true;
    }

    static bool MaskedContains(this Bitmap self, Bitmap other, Bitmap mask)
    {
        for (int x = 0; x <= self.Width - other.Width; x++)
        {
            for (int y = 0; y <= self.Height - other.Height; y++)
            {
                if (self.MaskedEquals(other, mask, x, y))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
