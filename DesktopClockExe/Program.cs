using System;
using System.Windows.Forms;

namespace DesktopClockExe;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => AppLogger.Log("UI exception", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            AppLogger.Log("Unhandled exception", args.ExceptionObject as Exception);

        ApplicationConfiguration.Initialize();
        AppLogger.Log("Application starting");
        Application.Run(new WallpaperForm());
    }
}
