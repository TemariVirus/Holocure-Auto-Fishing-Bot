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

    private static Rect GetWindowRectUnscaled(IntPtr hWnd)
    {
        Rect rect = new Rect();
        GetWindowRect(hWnd, ref rect);

        rect.Left = (int)Math.Round(rect.Left * _scaleFactor);
        rect.Top = (int)Math.Round(rect.Top * _scaleFactor);
        rect.Right = (int)Math.Round(rect.Right * _scaleFactor);
        rect.Bottom = (int)Math.Round(rect.Bottom * _scaleFactor);

        return rect;
    }

    public static ReadonlyImage CaptureWindow(
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

        return new ReadonlyImage(bmp);
    }
}
