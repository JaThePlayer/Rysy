using System.Runtime.InteropServices;

namespace Rysy.Platforms;

public partial class Windows {
    private static partial class Imports {
        public const int StdInputHandle = -10;

        public const int StdOutputHandle = -11;

        public const uint EnableVirtualTerminalProcessing = 0x0004;

        public const uint EnableVirtualTerminalInput = 0x0200;

        [LibraryImport("user32.dll")]
        public static partial IntPtr GetActiveWindow();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(int hWnd, int nCmdShow);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial IntPtr GetStdHandle(int nStdHandle);

        [LibraryImport("kernel32.dll")]
        public static partial uint GetLastError();

        public static void SetImmersiveDarkTheme(nint window, bool toggle) {
            int value = toggle ? 1 : 0; // 1 = enable dark mode, 0 = disable

            DwmSetWindowAttribute(
                window,
                WindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref value,
                sizeof(int)
            );

            // Enable Mica
            DWM_SYSTEMBACKDROP_TYPE backdrop = DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(
                window,
                WindowAttribute.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdrop,
                sizeof(DWM_SYSTEMBACKDROP_TYPE)
            );

            MARGINS margins = new() //
            {
                cxLeftWidth = 0,
                cxRightWidth = 0,
                cyTopHeight = 1, // important: >= 1
                cyBottomHeight = 0
            };

            DwmExtendFrameIntoClientArea(window, ref margins);
        }

        [LibraryImport("dwmapi.dll")]
        internal static partial int DwmExtendFrameIntoClientArea(
            IntPtr hwnd,
            ref MARGINS margins
        );

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

        [StructLayout(LayoutKind.Sequential)]
        internal struct MARGINS {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        internal enum DWM_SYSTEMBACKDROP_TYPE {
            DWMSBT_AUTO = 0,
            DWMSBT_NONE = 1,
            DWMSBT_MAINWINDOW = 2, // Mica
            DWMSBT_TRANSIENTWINDOW = 3,
            DWMSBT_TABBEDWINDOW = 4 // Mica Alt
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
            
        [LibraryImport("kernel32")]
        public static partial int AttachConsole(long dwProcessId);
            
            
        [LibraryImport("kernel32")]
        public static partial int FreeConsole();
    
        public enum ALLOC_CONSOLE_MODE
        {
            DEFAULT = 0,
            NEW_WINDOW = 1,
            NO_WINDOW = 2
        }

        public enum ALLOC_CONSOLE_RESULT
        {
            NO_CONSOLE = 0,
            NEW_CONSOLE = 1,
            EXISTING_CONSOLE = 2
        }
    
        [StructLayout(LayoutKind.Sequential)]
        public struct ALLOC_CONSOLE_OPTIONS
        {
            public ALLOC_CONSOLE_MODE mode;

            public int useShowWindow;

            public ushort showWindow;
        }
    
        [LibraryImport("kernel32.dll", EntryPoint = "AllocConsoleWithOptions")]
        public static unsafe partial int AllocConsoleWithOptionsNative(
            ALLOC_CONSOLE_OPTIONS* allocOptions,
            ALLOC_CONSOLE_RESULT* result
        );

        public static int AllocConsoleWithOptions(
            ALLOC_CONSOLE_OPTIONS options,
            out ALLOC_CONSOLE_RESULT result)
        {
            unsafe {
                fixed (ALLOC_CONSOLE_RESULT* resPtr = &result) {
                    return AllocConsoleWithOptionsNative(&options, resPtr);
                }
            }
        }
    }
}