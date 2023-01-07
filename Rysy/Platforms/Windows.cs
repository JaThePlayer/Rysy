using System.Runtime.InteropServices;

namespace Rysy.Platforms;

public class Windows : RysyPlatform {
    private static string SaveLocation = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rysy"
    ).Unbackslash();

    public override string GetSaveLocation() => SaveLocation;

    public override void Init() {
        base.Init();

        EnableANSI();
    }

    #region ANSI codes
    // Based on:
    // https://gist.github.com/tomzorz/6142d69852f831fb5393654c90a1f22e
    private void EnableANSI() {
        // Running the .exe directly doesn't enable ANSI codes on Windows, even though cmd supports them.
        // We can enable it by calling into the Windows API though!

        var iStdIn = GetStdHandle(STD_INPUT_HANDLE);
        var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

        if (!GetConsoleMode(iStdIn, out uint inConsoleMode)) {
            Console.WriteLine("Failed to get input console mode. Not enabling ANSI codes!");
            return;
        }
        if (!GetConsoleMode(iStdOut, out uint outConsoleMode)) {
            Console.WriteLine("failed to get output console mode. Not enabling ANSI codes!");
            return;
        }

        inConsoleMode |= ENABLE_VIRTUAL_TERMINAL_INPUT;
        outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;

        if (!SetConsoleMode(iStdIn, inConsoleMode)) {
            Console.WriteLine($"failed to set input console mode, error code: {GetLastError()}. Not enabling ANSI codes!");
            return;
        }
        if (!SetConsoleMode(iStdOut, outConsoleMode)) {
            Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}. Not enabling ANSI codes!");
            return;
        }
    }

    private const int STD_INPUT_HANDLE = -10;

    private const int STD_OUTPUT_HANDLE = -11;

    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();
    #endregion
}
