using Microsoft.Win32;
using Rysy.Gui;
using Rysy.Mods;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Rysy.Platforms;

[SupportedOSPlatform("windows")]
public partial class Windows : RysyPlatform {
    private ReadonlyModFilesystem? _systemFontsFs;
    private IReadOnlyDictionary<string, string>? _fontFilenameToDisplayName;
    
    
    private static string SaveLocation = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rysy"
    ).Unbackslash();

    public override string GetSaveLocation() => RysyState.CmdArguments.Portable ? "portableData" : SaveLocation;

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
        
        Themes.ThemeChanged += ThemesOnThemeChanged;
    }

    private void ThemesOnThemeChanged(Theme theme) {
        // Enable dark theme on the window depending on the menubar color
        var window = GetActiveWindow().ToInt32();
        if (window != 0) {
            var menubarColor = theme.ImGuiStyle.Colors.TryGetValue("MenuBarBg", out var menubarBg)
                ? menubarBg
                : new Color(0x23, 0x23, 0x23); // imgui default for dark theme (which is the default Rysy theme)
            DwmApi.SetImmersiveDarkTheme(window, !IsColorLight(menubarColor));
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
    private void EnableAnsi() {
        // Running the .exe directly doesn't enable ANSI codes on Windows, even though cmd supports them.
        // We can enable it by calling into the Windows API though!

        var iStdIn = GetStdHandle(StdInputHandle);
        var iStdOut = GetStdHandle(StdOutputHandle);

        if (!GetConsoleMode(iStdIn, out uint inConsoleMode)) {
            //Console.WriteLine("Failed to get input console mode. Not enabling ANSI codes!");
            return;
        }
        if (!GetConsoleMode(iStdOut, out uint outConsoleMode)) {
            //Console.WriteLine("failed to get output console mode. Not enabling ANSI codes!");
            return;
        }

        inConsoleMode |= EnableVirtualTerminalInput;
        outConsoleMode |= EnableVirtualTerminalProcessing;

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

    private const int StdInputHandle = -10;

    private const int StdOutputHandle = -11;

    private const uint EnableVirtualTerminalProcessing = 0x0004;

    private const uint EnableVirtualTerminalInput = 0x0200;

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
    
    internal static partial class DwmApi
    {
        public static void SetImmersiveDarkTheme(nint window, bool toggle) {
            int value = toggle ? 1 : 0; // 1 = enable dark mode, 0 = disable

            DwmSetWindowAttribute(
                window,
                WindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref value,
                sizeof(int)
            );
            
            // Enable Mica
            var backdrop = DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(
                window,
                WindowAttribute.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdrop,
                sizeof(DWM_SYSTEMBACKDROP_TYPE)
            );
        }
        
        [LibraryImport("dwmapi.dll")]
        internal static partial int DwmSetWindowAttribute(
            IntPtr hwnd,
            WindowAttribute dwAttribute,
            ref int pvAttribute,
            int cbAttribute
        );
        
        [LibraryImport("dwmapi.dll")]
        internal static partial int DwmSetWindowAttribute(
            IntPtr hwnd,
            WindowAttribute attribute,
            ref DWM_SYSTEMBACKDROP_TYPE pvAttribute,
            int cbAttribute
        );
        
        internal enum DWM_SYSTEMBACKDROP_TYPE
        {
            DWMSBT_AUTO = 0,
            DWMSBT_NONE = 1,
            DWMSBT_MAINWINDOW = 2,   // Mica
            DWMSBT_TRANSIENTWINDOW = 3,
            DWMSBT_TABBEDWINDOW = 4  // Mica Alt
        }
        
        internal enum WindowAttribute // DWMWINDOWATTRIBUTE
        {
            DWMWA_NCRENDERING_ENABLED = 0,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_PASSIVE_UPDATE_MODE,
            DWMWA_USE_HOSTBACKDROPBRUSH,
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            DWMWA_BORDER_COLOR,
            DWMWA_CAPTION_COLOR,
            DWMWA_TEXT_COLOR,
            DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
            DWMWA_SYSTEMBACKDROP_TYPE,
            DWMWA_LAST
        }
    }
}
