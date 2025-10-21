using Hexa.NET.ImGui;
using Rysy.Mods;
using Rysy.Platforms;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Rysy.Gui;

public static class ImGuiThemer {
    static ImGuiThemer() {
        Settings.OnLoaded += (s) => {
            //SetFontSize(s.FontSize);
        };
    }

    private static readonly JsonSerializerOptions ThemerJsonOptions = new() {
        IncludeFields = true,
        WriteIndented = true,
        IgnoreReadOnlyProperties = true,
    };

    public static unsafe void LoadTheme(string filename) {
        var fs = RysyPlatform.Current.GetRysyFilesystem();
        if (!fs.FileExists(filename)) {
            var internalPath = $"themes/{filename}.json";
            if (fs.FileExists(internalPath)) {
                filename = internalPath;
            } else {
                Logger.Write("ImGuiThemer", LogLevel.Warning, $"Theme doesn't exist: {filename}.");
                return;
            }
        }

        if (fs.TryReadAllText(filename) is {} themeJson) {
            ImGuiStylePtr ptr = ImGui.GetStyle();
            var s = JsonSerializer.Deserialize<ImGuiStyleSerializable>(themeJson, ThemerJsonOptions);
            s.DockingSeparatorSize = 1;
            s.SeparatorTextAlign = new(.5f, .5f);
            s.SeparatorTextBorderSize = 1;
            s.WindowBorderHoverPadding = ptr.WindowBorderHoverPadding;
            s.TreeLinesFlags = ptr.TreeLinesFlags;
            s.FontSizeBase = ptr.FontSizeBase;
            s.FontScaleDpi = ptr.FontScaleDpi;
            s.FontScaleMain = ptr.FontScaleMain;
            s.DockingNodeHasCloseButton = 1;
            
            var nptr = ptr.Handle;
            *nptr = Unsafe.BitCast<ImGuiStyleSerializable, ImGuiStyle>(s);
        }
    }

    public static unsafe void SetFontSize(float fontSize) {
        var io = ImGui.GetIO();
        io.Fonts.Clear();
        
        ImVector<uint> ranges = new();
        /*
        var builder = ImGui.ImFontGlyphRangesBuilder();
        builder.AddRanges(io.Fonts.GetGlyphRangesDefault());
        
        ReadOnlySpan<ushort> latinExtendedRanges =
        [
            0x0100, 0x024F,
            0,
        ];
        
        builder.AddRanges((uint*)Unsafe.AsPointer(ref Unsafe.AsRef(in latinExtendedRanges[0])));
        builder.BuildRanges(&ranges);
        */
        
        var font = Settings.Instance.Font ?? "RobotoMono";// "C:/Windows/Fonts/consola";
        var defaultFontPath = $"{font}.ttf";
        var boldFontPath = $"{font}b.ttf";
        var italicFontPath = $"{font}i.ttf";
        var boldItalicFontPath = $"{font}z.ttf";

        if (Settings.Instance.UseBoldFontByDefault) {
            BoldFont = AddFont(boldFontPath, fontSize, &ranges);
            DefaultFont = AddFont(defaultFontPath, fontSize, &ranges);
        } else {
            DefaultFont = AddFont(defaultFontPath, fontSize, &ranges);
            BoldFont = AddFont(boldFontPath, fontSize, &ranges);
        }

        ItalicFont = AddFont(italicFontPath, fontSize, &ranges);
        ItalicBoldFont = AddFont(boldItalicFontPath, fontSize, &ranges);
        
        HeaderFont = AddFont(boldFontPath, fontSize * 2f, &ranges);
        Header2Font = AddFont(boldFontPath, fontSize * 1.5f, &ranges);
        
        ImGui.GetStyle().FontSizeBase = fontSize;
        
        ImFontPtr AddFont(string name, float size, ImVector<uint>* ranges) {
            var fs = RysyPlatform.Current.GetRysyFilesystem();

            ImFontConfigPtr cfg = ImGui.ImFontConfig();
            //cfg.RasterizerDensity = 1f;
            //cfg.FontDataOwnedByAtlas = true;
            //cfg.OversampleH = 2;
            //cfg.OversampleV = 2;
            cfg.GlyphMaxAdvanceX = float.MaxValue;
            cfg.RasterizerMultiply = 1.0f;
            cfg.EllipsisChar = unchecked((ushort) -1);
            //cfg.GlyphRanges = ranges->Data;
            
            if (File.Exists(name)) {
                return io.Fonts.AddFontFromFileTTF(name, size, cfg);
            }

            ImFontPtr fontPtr = AddFontFromVirtPath(name, size, cfg);

            if (fontPtr.Handle == null && name != defaultFontPath) {
                return AddFont(defaultFontPath, size, ranges);
            }

            /*
            if (fontPtr.Handle != null) {
                var newCfgData = *cfg.Handle;
                var newCfg = new ImFontConfigPtr(&newCfgData) { MergeMode = true };
                const int ICON_MIN_FA = 0xe000;
                const int  ICON_MAX_FA = 0xf8ff;
                ReadOnlySpan<ushort> icon_ranges = [ICON_MIN_FA, ICON_MAX_FA, 0];
                var mem = ImGui.MemAlloc((uint)icon_ranges.Length * sizeof(ushort));
                icon_ranges.CopyTo(new Span<ushort>((void*)mem, icon_ranges.Length));
                newCfg.GlyphRanges = (uint*)mem;
                var newFontWithIcons = AddFontFromVirtPath("fa-solid-900.ttf", 13f, newCfg);
                if (newFontWithIcons.Handle != null)
                    fontPtr = newFontWithIcons;
            }
            */
            
            return fontPtr;

            // Loads a font from a virtual path, either from the mod filesystem or the OS's fonts folder.
            static ImFontPtr AddFontFromVirtPath(string name, float size, ImFontConfigPtr cfg) {
                var io = ImGui.GetIO();
                
                ImFontPtr fontPtr = default;
                if (RysyPlatform.Current.GetRysyFilesystem().TryReadAllBytes(name) is { } bytes) {
                    // Imgui will take ownership of this memory, we need to native-alloc it.
                    var mem = ImGui.MemAlloc((uint)bytes.Length);
                    bytes.CopyTo(new Span<byte>((void*)mem, bytes.Length));
                    fontPtr = io.Fonts.AddFontFromMemoryTTF(mem, bytes.Length, size, cfg);
                }
                else if (RysyPlatform.Current.GetSystemFontsFilesystem()?.TryReadAllBytes(name) is { } bytes2) {
                    // Imgui will take ownership of this memory, we need to native-alloc it.
                    var mem = ImGui.MemAlloc((uint)bytes2.Length);
                    bytes2.CopyTo(new Span<byte>((void*)mem, bytes2.Length));
                
                    fontPtr = io.Fonts.AddFontFromMemoryTTF(mem, bytes2.Length, size, cfg);
                }

                return fontPtr;
            }
        }
    }

    public static ImFontPtr DefaultFont { get; private set; }
    public static ImFontPtr BoldFont { get; private set; }
    public static ImFontPtr ItalicFont { get; private set; }
    public static ImFontPtr ItalicBoldFont { get; private set; }
    public static ImFontPtr HeaderFont { get; private set; }
    public static ImFontPtr Header2Font { get; private set; }
    
    public struct ImGuiStyleSerializable
    {
        
        public float FontSizeBase;
        
        public float FontScaleMain;
        
        public float FontScaleDpi;
        
        public float Alpha;
        
        public float DisabledAlpha;
        
        public Vector2 WindowPadding;
        
        public float WindowRounding;
        
        public float WindowBorderSize;
        
        public float WindowBorderHoverPadding;
        
        public Vector2 WindowMinSize;
        
        public Vector2 WindowTitleAlign;
        
        public ImGuiDir WindowMenuButtonPosition;
        
        public float ChildRounding;
        
        public float ChildBorderSize;
        
        public float PopupRounding;
        
        public float PopupBorderSize;
        
        public Vector2 FramePadding;
        
        public float FrameRounding;
        
        public float FrameBorderSize;
        
        public Vector2 ItemSpacing;
        
        public Vector2 ItemInnerSpacing;
        
        public Vector2 CellPadding;
        
        public Vector2 TouchExtraPadding;
        
        public float IndentSpacing;
        
        public float ColumnsMinSpacing;
        
        public float ScrollbarSize;
        
        public float ScrollbarRounding;
        
        public float ScrollbarPadding;
        
        public float GrabMinSize;
        
        public float GrabRounding;
        
        public float LogSliderDeadzone;
        
        public float ImageBorderSize;
        
        public float TabRounding;
        
        public float TabBorderSize;
        
        public float TabMinWidthBase;
        
        public float TabMinWidthShrink;
        
        public float TabCloseButtonMinWidthSelected;
        
        public float TabCloseButtonMinWidthUnselected;
        
        public float TabBarBorderSize;
        
        public float TabBarOverlineSize;
        
        public float TableAngledHeadersAngle;
        
        public Vector2 TableAngledHeadersTextAlign;
        
        public ImGuiTreeNodeFlags TreeLinesFlags;
        
        public float TreeLinesSize;
        
        public float TreeLinesRounding;
        
        public ImGuiDir ColorButtonPosition;
        
        public Vector2 ButtonTextAlign;
        
        public Vector2 SelectableTextAlign;
        
        public float SeparatorTextBorderSize;
        
        public Vector2 SeparatorTextAlign;
        
        public Vector2 SeparatorTextPadding;
        
        public Vector2 DisplayWindowPadding;
        
        public Vector2 DisplaySafeAreaPadding;
        
        public byte DockingNodeHasCloseButton;
        
        public float DockingSeparatorSize;
        
        public float MouseCursorScale;
        
        public byte AntiAliasedLines;
        
        public byte AntiAliasedLinesUseTex;
        
        public byte AntiAliasedFill;
        
        public float CurveTessellationTol;
        
        public float CircleTessellationMaxError;
        
        public Vector4 Colors_0;
        public Vector4 Colors_1;
        public Vector4 Colors_2;
        public Vector4 Colors_3;
        public Vector4 Colors_4;
        public Vector4 Colors_5;
        public Vector4 Colors_6;
        public Vector4 Colors_7;
        public Vector4 Colors_8;
        public Vector4 Colors_9;
        public Vector4 Colors_10;
        public Vector4 Colors_11;
        public Vector4 Colors_12;
        public Vector4 Colors_13;
        public Vector4 Colors_14;
        public Vector4 Colors_15;
        public Vector4 Colors_16;
        public Vector4 Colors_17;
        public Vector4 Colors_18;
        public Vector4 Colors_19;
        public Vector4 Colors_20;
        public Vector4 Colors_21;
        public Vector4 Colors_22;
        public Vector4 Colors_23;
        public Vector4 Colors_24;
        public Vector4 Colors_25;
        public Vector4 Colors_26;
        public Vector4 Colors_27;
        public Vector4 Colors_28;
        public Vector4 Colors_29;
        public Vector4 Colors_30;
        public Vector4 Colors_31;
        public Vector4 Colors_32;
        public Vector4 Colors_33;
        public Vector4 Colors_34;
        public Vector4 Colors_35;
        public Vector4 Colors_36;
        public Vector4 Colors_37;
        public Vector4 Colors_38;
        public Vector4 Colors_39;
        public Vector4 Colors_40;
        public Vector4 Colors_41;
        public Vector4 Colors_42;
        public Vector4 Colors_43;
        public Vector4 Colors_44;
        public Vector4 Colors_45;
        public Vector4 Colors_46;
        public Vector4 Colors_47;
        public Vector4 Colors_48;
        public Vector4 Colors_49;
        public Vector4 Colors_50;
        public Vector4 Colors_51;
        public Vector4 Colors_52;
        public Vector4 Colors_53;
        public Vector4 Colors_54;
        public Vector4 Colors_55;
        public Vector4 Colors_56;
        public Vector4 Colors_57;
        public Vector4 Colors_58;
        public Vector4 Colors_59;
        
        public float HoverStationaryDelay;
        
        public float HoverDelayShort;
        
        public float HoverDelayNormal;
        
        public ImGuiHoveredFlags HoverFlagsForTooltipMouse;
        
        public ImGuiHoveredFlags HoverFlagsForTooltipNav;
        
        public float MainScale;
        
        public float NextFrameFontSizeBase;
    }
}

