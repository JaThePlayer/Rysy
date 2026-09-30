using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

if (OperatingSystem.IsWindows())
    WindowsImport.AttachConsole(-1);

if (args is not [var zipPath]) {
    Console.WriteLine("Expected zip path as the first argument!");
    return;
}

try {
    await ExtractZipAndRunRysy(zipPath);
} catch (Exception ex) {
    Console.WriteLine($"""
                       Failed to update Rysy.
                       Press any key to close the updater.
                       Please report this issue, and, if Rysy cannot launch anymore, reinstall the program manually.
                       Exception: {ex}
                       """);
    _ = Console.ReadLine();
}

return;

async Task ExtractZipAndRunRysy(string s)
{
    var dir = AppContext.BaseDirectory;
    var rysyExecutableFile = Path.Combine(dir, OperatingSystem.IsWindows() ? "Rysy.exe" : "Rysy");

    while (true) {
        if (!File.Exists(rysyExecutableFile)) {
            Console.WriteLine("Rysy executable file does not exist, continuing..");
            break;
        }
    
        try {
            await using var file = File.OpenWrite(rysyExecutableFile);
            break;
        } catch (IOException) {
            Console.WriteLine("Rysy executable is not writeable, waiting...");
            await Task.Delay(500);
        }
    }

    Console.WriteLine("Rysy executable file writeable, extracting zip...");
    {
        await using var zip = await ZipArchive.CreateAsync(File.Open(s, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update, false, null);
        // We can't extract the updater since its executable is locked now, so it has already been extracted before Rysy closed.
        var updaterFileName = OperatingSystem.IsWindows() ? "Rysy.Updater.exe" : "Rysy.Updater";
        zip.GetEntry(updaterFileName)?.Delete();

        await zip.ExtractToDirectoryAsync(dir, overwriteFiles: true);
    }

    File.Delete(zipPath);

    Console.WriteLine("Extracted zip file, running Rysy...");

    if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
        try {
            File.SetUnixFileMode(rysyExecutableFile,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        } catch (Exception ex) {
            Console.WriteLine($"Failed add execute permission to rysy executable file: {ex}");
        }
    }

    Process.Start(new ProcessStartInfo {
        FileName = rysyExecutableFile,
        UseShellExecute = true,
    });
}

[SupportedOSPlatform("windows")]
partial class WindowsImport {
    [LibraryImport("kernel32")]
    public static partial int AttachConsole(long dwProcessId);
}
