using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

static partial class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern uint BitBlt(
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
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    private static readonly double _scaleFactor = GetScalingFactor();

    // https://stackoverflow.com/a/21450169
    private static double GetScalingFactor()
    {
        const int VERT_RES = 10;
        const int DESKTOP_VERT_RES = 117;

        Graphics g = Graphics.FromHwnd(IntPtr.Zero);
        IntPtr desktop = g.GetHdc();
        double logicalHeight = GetDeviceCaps(desktop, VERT_RES);
        double physicalHeight = GetDeviceCaps(desktop, DESKTOP_VERT_RES);

        return physicalHeight / logicalHeight;
    }

    private static Rect GetWindowRectUnscaled()
    {
        Rect rect = new Rect();
        GetWindowRect(_windowHandle, ref rect);

        rect.Left = (int)Math.Round(rect.Left * _scaleFactor) + 8;
        rect.Top = (int)Math.Round(rect.Top * _scaleFactor) + 31;
        rect.Right = (int)Math.Round(rect.Right * _scaleFactor) - 8;
        rect.Bottom = (int)Math.Round(rect.Bottom * _scaleFactor) - 8;

        return rect;
    }

    private static int GetHolocureResolution(Rect rect)
    {
        const int GRACE = 32;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (_windowWidth != width || _windowHeight != height)
        {
            _windowWidth = width;
            _windowHeight = height;
            Console.WriteLine(
                _jpMode
                    ? $"HoloCure no mado ha {width} x {height} ni saremasita"
                    : $"HoloCure window was resized to {width} x {height}"
            );

            // Invalidate target position
            _targetLeft = -1;
            _targetTop = -1;
        }

        width -= GRACE;
        height -= GRACE;
        if (width <= 640 && height <= 360)
        {
            return 1;
        }
        else if (width <= 1280 && height <= 720)
        {
            return 2;
        }
        else if (width <= 1920 && height <= 1080)
        {
            return 3;
        }
        else if (width <= 2560 && height <= 1440)
        {
            return 4;
        }

        throw new Exception(
            _jpMode
                ? "kaizoudo ga fumei desita. HoloCure no kaizoudo wo tiisaku shite mite kudasai."
                : "Could not determine resolution. Try making HoloCure's resolution smaller."
        );
    }

    // Always behaves as if the window is 640 x 360
    private static ReadonlyImage CaptureHolocureWindow(
        int left = 0,
        int top = 0,
        int width = -1,
        int height = -1
    )
    {
        Rect rect = GetWindowRectUnscaled();
        _resolution = GetHolocureResolution(rect);

        left *= _resolution;
        top *= _resolution;
        width *= _resolution;
        height *= _resolution;

        if (width < 0)
        {
            width = _windowWidth;
        }
        if (height < 0)
        {
            height = _windowHeight;
        }

        if (_hardwareAccelerated)
        {
            if (_lastSS == null)
            {
                Bitmap bmp = new Bitmap(_windowWidth, _windowHeight, PixelFormat.Format32bppArgb);
                Graphics graphics = Graphics.FromImage(bmp);
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                _lastSS = new ReadonlyImage(bmp);
            }

            return _lastSS.Crop(left, top, width, height).ShrinkBy(_resolution);
        }
        else
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = graphics.GetHdc();
            IntPtr hdcWindow = GetWindowDC(_windowHandle);
            BitBlt(hdcBitmap, 0, 0, width, height, hdcWindow, left, top, 0x00CC0020);

            graphics.ReleaseHdc(hdcBitmap);
            ReleaseDC(_windowHandle, hdcWindow);
            return new ReadonlyImage(bmp).ShrinkBy(_resolution);
        }
    }

    private static void InvalidateLastSS()
    {
        _lastSS = null;
    }
}
