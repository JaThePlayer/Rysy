using Microsoft.Win32;
using Rysy.Components;
using Rysy.Gui;
using Rysy.Mods;
using Rysy.Signals;
using System.Runtime.Versioning;

namespace Rysy.Platforms;

[SupportedOSPlatform("windows")]
public partial class Windows : RysyPlatform, ISignalListener<ThemeChanged>, ISignalListener<ComponentAdded<Settings>>, ISignalListener<SettingsChanged<bool>> {
    private ReadonlyModFilesystem? _systemFontsFs;
    private IReadOnlyDictionary<string, string>? _fontFilenameToDisplayName;

    public override IModFilesystem GetSystemFontsFilesystem()
        => _systemFontsFs ??= new ReadonlyModFilesystem(new FolderModFilesystem("C:/Windows/Fonts"));

    public override IReadOnlyDictionary<string, string> GetFontFilenameToDisplayName() {
        if (_fontFilenameToDisplayName is { })
            return _fontFilenameToDisplayName;

        var fontNameKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts");
        if (fontNameKey is null)
            return _fontFilenameToDisplayName = new Dictionary<string, string>();
        
        var fontNames = fontNameKey.GetValueNames();
        var fontPathToName = fontNames.ToDictionary(x => fontNameKey.GetValue(x)?.ToString() ?? "", x => x, StringComparer.OrdinalIgnoreCase);

        return _fontFilenameToDisplayName = fontPathToName;
    }

    public override bool IsSystemFontValid(string fontPath) => GetFontFilenameToDisplayName().ContainsKey(fontPath);

    public override void Init() {
        base.Init();

        EnableAnsi();
    }

    private void OnThemeChanged(Theme theme) {
        // Enable dark theme on the window depending on the menubar color
        var window = Imports.GetActiveWindow().ToInt32();
        if (window != 0) {
            var menubarColor = theme.ImGuiStyle.Colors.TryGetValue("MenuBarBg", out var menubarBg)
                ? menubarBg
                : new Color(0x23, 0x23, 0x23); // imgui default for dark theme (which is the default Rysy theme)
            Imports.SetImmersiveDarkTheme(window, !IsColorLight(menubarColor));
        }

        return;

        // https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-windows-themes#know-when-dark-mode-is-enabled
        bool IsColorLight(Color clr)
        {
            return 5 * clr.G + 2 * clr.R + clr.B > 8 * 128;
        }
    }

    public override void ResizeWindow(int x, int y, int w, int h) {
        var gdm = RysyState.GraphicsDeviceManager;
        var monitorSize = gdm.GraphicsDevice.DisplayMode;

        if (w == monitorSize.Width && Math.Abs(h - monitorSize.Height) <= 80) {
            // most likely, the window was maximized previously
            // let's do this property
            gdm.PreferredBackBufferWidth = w;
            gdm.PreferredBackBufferHeight = h;
            gdm.ApplyChanges();
            var window = Imports.GetActiveWindow().ToInt32();
            // subsequent calls to ShowWindow with the same argument seem to do nothing,
            // so let's give some other flag first.
            // Otherwise, disabling borderless fullscreen wouldn't maximize the window again.
            Imports.ShowWindow(window, 4);
            Imports.ShowWindow(window, 3);
        } else {
            base.ResizeWindow(x, y, w, h);
        }
    }

    #region ANSI codes
    // Based on:
    // https://gist.github.com/tomzorz/6142d69852f831fb5393654c90a1f22e
    private void EnableAnsi() {
        // Running the .exe directly doesn't enable ANSI codes on Windows, even though cmd supports them.
        // We can enable it by calling into the Windows API though!

        var iStdIn = Imports.GetStdHandle(Imports.StdInputHandle);
        var iStdOut = Imports.GetStdHandle(Imports.StdOutputHandle);

        if (!Imports.GetConsoleMode(iStdIn, out uint inConsoleMode)) {
            //Console.WriteLine("Failed to get input console mode. Not enabling ANSI codes!");
            return;
        }
        if (!Imports.GetConsoleMode(iStdOut, out uint outConsoleMode)) {
            //Console.WriteLine("failed to get output console mode. Not enabling ANSI codes!");
            return;
        }

        inConsoleMode |= Imports.EnableVirtualTerminalInput;
        outConsoleMode |= Imports.EnableVirtualTerminalProcessing;

        if (!Imports.SetConsoleMode(iStdIn, inConsoleMode)) {
            //Console.WriteLine($"failed to set input console mode, error code: {GetLastError()}. Not enabling ANSI codes!");
            return;
        }
        if (!Imports.SetConsoleMode(iStdOut, outConsoleMode)) {
            //Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}. Not enabling ANSI codes!");
            return;
        }

        Logger.UseColorsInConsole = true;
    }
    #endregion

    void ISignalListener<ThemeChanged>.OnSignal(ThemeChanged signal) {
        OnThemeChanged(signal.NewTheme);
    }

    private bool _wasConsoleAllocated;

    private void ToggleConsole(bool enabled) {
        if (enabled) {
            if (_wasConsoleAllocated)
                return;
            if (Imports.AttachConsole(-1) != 0)
                return;
            if (Imports.AttachConsole(Environment.ProcessId) != 0)
                return;
        
            Imports.AllocConsoleWithOptions(new Imports.ALLOC_CONSOLE_OPTIONS {
                mode = Imports.ALLOC_CONSOLE_MODE.DEFAULT,
                useShowWindow = 1,
                showWindow = 6 // SW_MINIMIZE
            }, out Imports.ALLOC_CONSOLE_RESULT res);
            _wasConsoleAllocated = res == Imports.ALLOC_CONSOLE_RESULT.NEW_CONSOLE;
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
            Console.SetError(new StreamWriter(Console.OpenStandardError()));
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
            EnableAnsi();
            return;
        }

        if (!_wasConsoleAllocated)
            return;
        _wasConsoleAllocated = false;
            
        Console.SetOut(StreamWriter.Null);
        Console.SetError(StreamWriter.Null);
        Console.SetIn(StreamReader.Null);
        Imports.FreeConsole();
    }

    void ISignalListener<ComponentAdded<Settings>>.OnSignal(ComponentAdded<Settings> signal) {
        ToggleConsole(signal.Component.AllocateConsole);
    }
    
    void ISignalListener<SettingsChanged<bool>>.OnSignal(SettingsChanged<bool> signal) {
        if (signal.SettingName == nameof(Settings.AllocateConsole)) {
            ToggleConsole(signal.Value);
        }
    }
}
