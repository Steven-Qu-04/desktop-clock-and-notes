using System;
using System.IO;

namespace DesktopClockExe;

internal static class AppLogger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "desktop-clock.log");

    public static void Log(string message, Exception? exception = null)
    {
        try
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            if (exception != null)
            {
                line += Environment.NewLine + exception + Environment.NewLine;
            }

            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch
        {
        }
    }
}
