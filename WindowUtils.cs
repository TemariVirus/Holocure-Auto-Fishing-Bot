using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class WindowUtils
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

    public static IntPtr GetHolocureWindow()
    {
        Process[] processes = Process.GetProcessesByName("holocure");
        if (processes.Length <= 0)
        {
            throw new Exception("Please open HoloCure.");
        }

        return processes[0].MainWindowHandle;
    }

    // https://stackoverflow.com/a/21450169
    private static float GetScalingFactor()
    {
        const int VERTRES = 10;
        const int DESKTOPVERTRES = 117;

        Graphics g = Graphics.FromHwnd(IntPtr.Zero);
        IntPtr desktop = g.GetHdc();
        int logical_height = GetDeviceCaps(desktop, VERTRES);
        int physical_height = GetDeviceCaps(desktop, DESKTOPVERTRES);

        return (float)physical_height / (float)logical_height;
    }

    private static Rect GetWindowRectUnscaled(IntPtr hWnd)
    {
        Rect rect = new Rect();
        GetWindowRect(hWnd, ref rect);

        float scale = GetScalingFactor();
        rect.Left = (int)(rect.Left * scale);
        rect.Top = (int)(rect.Top * scale);
        rect.Right = (int)(rect.Right * scale);
        rect.Bottom = (int)(rect.Bottom * scale);

        return rect;
    }

    public static Image2D CaptureWindow(
        IntPtr hWnd,
        int left = 0,
        int top = 0,
        int width = -1,
        int height = -1
    )
    {
        Rect rect = GetWindowRectUnscaled(hWnd);
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

        BitBlt(hdcBitmap, 0, 0, width, height, hdcWindow, left, top, 0x00CC0020);

        graphics.ReleaseHdc(hdcBitmap);
        ReleaseDC(hWnd, hdcWindow);

        return new Image2D(bmp);
    }
}
