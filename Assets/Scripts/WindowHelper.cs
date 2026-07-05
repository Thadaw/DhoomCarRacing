using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public static class WindowHelper
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    public static void BringToFront()
    {
        try
        {
            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, SW_RESTORE);
                SetForegroundWindow(handle);
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("WindowHelper.BringToFront failed: " + e.Message);
        }
    }
}
