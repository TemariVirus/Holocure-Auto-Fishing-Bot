using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Holocure_Auto_Fishing_Bot
{
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

        #region DLL Imports
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWind);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

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
        #endregion

        private const int DEBUG_MAX_IMAGES = 20;
        private static int _debugImgCounter = 0;
        private static Stopwatch _debugSaveSw = Stopwatch.StartNew();

        private static Rect GetWindowRectUnscaled()
        {
            Rect rect = new Rect();
            GetWindowRect(_windowHandle, ref rect);

            double dpi = GetDpiForWindow(_windowHandle);
            double scale = dpi / 96;

            rect.Left = (int)Math.Round((rect.Left + 8) * scale);
            rect.Top = (int)Math.Round((rect.Top + 31) * scale);
            rect.Right = (int)Math.Round((rect.Right - 8) * scale);
            rect.Bottom = (int)Math.Round((rect.Bottom - 8) * scale);

            return rect;
        }

        private static int GetHolocureResolution(Rect rect)
        {
            const int GRACE = 64;

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (_windowWidth != width || _windowHeight != height)
            {
                _windowWidth = width;
                _windowHeight = height;
                PrintLine(
                    _isLocaleJp
                        ? $"HoloCure no mado ha {width} x {height} ni saremasita"
                        : $"HoloCure window was resized to {width} x {height}"
                );

                InvalidateTargetPos();
            }

            width -= GRACE;
            height -= GRACE;
            if (width <= 0 || height <= 0)
            {
                return 0;
            }
            if (width <= 640 && height <= 360)
            {
                return 1;
            }
            if (width <= 1280 && height <= 720)
            {
                return 2;
            }
            if (width <= 1920 && height <= 1080)
            {
                return 3;
            }
            if (width <= 2560 && height <= 1440)
            {
                return 4;
            }
            return 0;
        }

        private static void SaveDebugImg(ReadonlyImage img)
        {
            // Save at most 2 images per second to minimise performance impact
            if (_debugSaveSw.ElapsedMilliseconds < 500)
            {
                return;
            }
            _debugSaveSw.Restart();

            if (!Directory.Exists("debug"))
            {
                Directory.CreateDirectory("debug");
            }

            _debugImgCounter %= DEBUG_MAX_IMAGES;
            string path = $"debug/{_debugImgCounter++}.png";
            img.Save(path);
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
            int old_resolution = _resolution;
            _resolution = GetHolocureResolution(rect);

            left *= _resolution;
            top *= _resolution;
            width *= _resolution;
            height *= _resolution;
            if (width < 0)
            {
                width = _windowWidth - left;
            }
            if (height < 0)
            {
                height = _windowHeight - top;
            }

            // Return black image if can't capture window
            if (_resolution <= 0)
            {
                if (_resolution != old_resolution)
                {
                    PrintLine(
                        _isLocaleJp
                            ? "kaizoudo ga fumei desita. mosi FURUSUKURI-N wo tsukatteiru baai ha, HoloCure no mado ni CHENZI site kudasai."
                            : "Could not determine resolution. If you are using fullscreen, make sure to have the HoloCure window in focus."
                    );
                }
                return new ReadonlyImage(640, 360);
            }

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = graphics.GetHdc();
            if (_hardwareAccelerated)
            {
                IntPtr hdcWindow = GetDC(IntPtr.Zero);
                BitBlt(hdcBitmap, 0, 0, width, height, hdcWindow, rect.Left + left, rect.Top + top, 0x00CC0020);
                ReleaseDC(_windowHandle, hdcWindow);
            }
            else
            {
                IntPtr hdcWindow = GetDC(_windowHandle);
                BitBlt(hdcBitmap, 0, 0, width, height, hdcWindow, left, top, 0x00CC0020);
                ReleaseDC(_windowHandle, hdcWindow);
            }
            graphics.ReleaseHdc(hdcBitmap);

            var ret = new ReadonlyImage(bmp).ShrinkBy(_resolution);
            SaveDebugImg(ret);
            return ret;
        }

        private static void InvalidateLastSS()
        {
            _lastSS = null;
        }
    }
}
