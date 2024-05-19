using Rysy.Extensions;
using System.Runtime.InteropServices;

namespace Rysy.Platforms;

public partial class Windows : RysyPlatform {
    private static string SaveLocation = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rysy"
    ).Unbackslash();

    public override string GetSaveLocation() => SaveLocation;

    public override void Init() {
        base.Init();

        EnableANSI();
    }

    public override void ResizeWindow(int x, int y, int w, int h) {
        var gdm = RysyState.GraphicsDeviceManager;
        var monitorSize = gdm.GraphicsDevice.DisplayMode;

        if (w == monitorSize.Width && Math.Abs(h - monitorSize.Height) <= 80) {
            // most likely, the window was maximized previously
            // let's do this property
            gdm.PreferredBackBufferWidth = w;
            gdm.PreferredBackBufferHeight = h;
            var window = GetActiveWindow().ToInt32();
            // subsequent calls to ShowWindow with the same argument seem to do nothing,
            // so let's give some other flag first.
            // Otherwise, disabling borderless fullscreen wouldn't maximize the window again.
            ShowWindow(window, 4);
            ShowWindow(window, 3);
        } else {
            base.ResizeWindow(x, y, w, h);
        }
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetActiveWindow();
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(int hWnd, int nCmdShow);

    #region ANSI codes
    // Based on:
    // https://gist.github.com/tomzorz/6142d69852f831fb5393654c90a1f22e
    private void EnableANSI() {
        // Running the .exe directly doesn't enable ANSI codes on Windows, even though cmd supports them.
        // We can enable it by calling into the Windows API though!

        var iStdIn = GetStdHandle(STD_INPUT_HANDLE);
        var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

        if (!GetConsoleMode(iStdIn, out uint inConsoleMode)) {
            //Console.WriteLine("Failed to get input console mode. Not enabling ANSI codes!");
            return;
        }
        if (!GetConsoleMode(iStdOut, out uint outConsoleMode)) {
            //Console.WriteLine("failed to get output console mode. Not enabling ANSI codes!");
            return;
        }

        inConsoleMode |= ENABLE_VIRTUAL_TERMINAL_INPUT;
        outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;

        if (!SetConsoleMode(iStdIn, inConsoleMode)) {
            //Console.WriteLine($"failed to set input console mode, error code: {GetLastError()}. Not enabling ANSI codes!");
            return;
        }
        if (!SetConsoleMode(iStdOut, outConsoleMode)) {
            //Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}. Not enabling ANSI codes!");
            return;
        }

        Logger.UseColorsInConsole = true;
    }

    private const int STD_INPUT_HANDLE = -10;

    private const int STD_OUTPUT_HANDLE = -11;

    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll")]
    private static partial uint GetLastError();
    #endregion
}
