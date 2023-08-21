using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class InputUtils
{
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private static readonly Dictionary<string, uint> KeyMapping = new Dictionary<string, uint>
    {
        { "SHIFT", 0x10 },
        { "CTRL", 0x11 },
        { "ALT", 0x12 },
        { "SPACE", 0x20 },
        { "0", 0x30 },
        { "1", 0x31 },
        { "2", 0x32 },
        { "3", 0x33 },
        { "4", 0x34 },
        { "5", 0x35 },
        { "6", 0x36 },
        { "7", 0x37 },
        { "8", 0x38 },
        { "9", 0x39 },
        { "A", 0x41 },
        { "B", 0x42 },
        { "C", 0x43 },
        { "D", 0x44 },
        { "E", 0x45 },
        { "F", 0x46 },
        { "G", 0x47 },
        { "H", 0x48 },
        { "I", 0x49 },
        { "J", 0x4A },
        { "K", 0x4B },
        { "L", 0x4C },
        { "M", 0x4D },
        { "N", 0x4E },
        { "O", 0x4F },
        { "P", 0x50 },
        { "Q", 0x51 },
        { "R", 0x52 },
        { "S", 0x53 },
        { "T", 0x54 },
        { "U", 0x55 },
        { "V", 0x56 },
        { "W", 0x57 },
        { "X", 0x58 },
        { "Y", 0x59 },
        { "Z", 0x5A },
    };

    public static void PressKey(IntPtr hWnd, string key)
    {
        const uint WM_KEYDOWN = 0x0100;

        IntPtr key_code = (IntPtr)KeyMapping[key];
        PostMessage(hWnd, WM_KEYDOWN, key_code, IntPtr.Zero);
    }

    public static void ReleaseKey(IntPtr hWnd, string key)
    {
        const uint WM_KEYUP = 0x0101;

        IntPtr key_code = (IntPtr)KeyMapping[key];
        PostMessage(hWnd, WM_KEYUP, key_code, (IntPtr)0xC000_0000);
    }
}
