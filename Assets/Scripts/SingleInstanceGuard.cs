using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public static class SingleInstanceGuard
{
    const string MutexName = "AAR_DoomCarRacing_SingleInstance";

    const int SwRestore = 9;

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr window, int command);

    static Mutex instanceMutex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureSingleInstance()
    {
        bool isFirstInstance;
        try
        {
            instanceMutex = new Mutex(true, MutexName, out isFirstInstance);
        }
        catch (AbandonedMutexException)
        {
            isFirstInstance = true;
        }

        if (isFirstInstance)
            return;

        FocusRunningInstance();
        UnityEngine.Debug.Log("[SingleInstanceGuard] Another copy is already running, closing this one.");
        Application.Quit();
    }

    static void FocusRunningInstance()
    {
        var current = Process.GetCurrentProcess();
        var deadline = DateTime.UtcNow.AddSeconds(5f);

        while (DateTime.UtcNow < deadline)
        {
            var candidates = Process.GetProcessesByName(current.ProcessName);
            foreach (var candidate in candidates)
            {
                if (candidate.Id == current.Id)
                    continue;

                try
                {
                    var window = candidate.MainWindowHandle;
                    if (window != IntPtr.Zero)
                    {
                        ShowWindow(window, SwRestore);
                        SetForegroundWindow(window);
                        return;
                    }
                }
                catch (InvalidOperationException)
                {
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
            }

            Thread.Sleep(250);
        }
    }
}
