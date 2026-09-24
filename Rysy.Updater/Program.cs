using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;

if (args is not [var url]) {
    Console.WriteLine("Expected download URL as the first argument!");
    return;
}

if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
    !uri.AbsoluteUri.StartsWith("https://github.com/JaThePlayer/Rysy/releases/download",
        StringComparison.OrdinalIgnoreCase)) {
    Console.WriteLine("Expected a JaThePlayer/Rysy GitHub repository URL.");
    return;
}

var client = new HttpClient();

// GitHub requires a User-Agent header.
client.DefaultRequestHeaders.UserAgent.ParseAdd("Rysy/1.0");

client.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

// Current GitHub API version.
client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");

var stream = await client.GetStreamAsync(uri);
var zip = await ZipArchive.CreateAsync(stream, ZipArchiveMode.Read, false, null);

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

await zip.ExtractToDirectoryAsync(dir, overwriteFiles: true);

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
