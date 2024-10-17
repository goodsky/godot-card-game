using System;
using Godot;

public static class Log
{
    public static bool SkipAllLogging { get; set; } = false; // used in tests to avoid GD.* calls
    public static bool EnableDebugLogs { get; set; } = true;

    static Log()
    {
        AppDomain.CurrentDomain.UnhandledException += (obj, ex) => Error("Unhandled Exception in AppDomain: ", ex);
    }

    public static void Debug(params object[] what)
    {
        if (SkipAllLogging) return;
        if (EnableDebugLogs)
        {
            GD.Print(what);
        }
    }

    public static void Info(params object[] what)
    {
        if (SkipAllLogging) return;
        GD.Print(what);
    }

    public static void Warning(params object[] what)
    {
        if (SkipAllLogging) return;
        GD.Print(what);
        GD.PushWarning(what);
    }

    public static void Error(params object[] what)
    {
        if (SkipAllLogging) return;
        GD.PrintErr(what);
        GD.PushError(what);
    }
}