using ImGuiNET;
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
            ImGuiStyle s = JsonSerializer.Deserialize<ImGuiStyle>(themeJson, ThemerJsonOptions);
            var nptr = ptr.NativePtr;
            *nptr = s;

            nptr->DockingSeparatorSize = 1;
            nptr->SeparatorTextAlign = new(.5f, .5f);
            nptr->SeparatorTextBorderSize = 1;
        }
    }

    public static unsafe void SetFontSize(float fontSize) {
        var io = ImGui.GetIO();
        io.Fonts.Clear();
        
        ImVector ranges = new();
        var builder = ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder();
        ImGuiNative.ImFontGlyphRangesBuilder_AddRanges(builder, (ushort*)io.Fonts.GetGlyphRangesDefault());
        
        ReadOnlySpan<ushort> latinExtendedRanges =
        [
            0x0100, 0x024F,
            0,
        ];
        
        ImGuiNative.ImFontGlyphRangesBuilder_AddRanges(builder, (ushort*)Unsafe.AsPointer(ref Unsafe.AsRef(in latinExtendedRanges[0])));
        ImGuiNative.ImFontGlyphRangesBuilder_BuildRanges(builder, &ranges);
        
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

        bool fontBuildSuccess;
        try {
            fontBuildSuccess = io.Fonts.Build();
        } catch (Exception ex) {
            Logger.Error(ex, "Failed to build ImGui font atlas");
            fontBuildSuccess = false;
        }
        
        if (!fontBuildSuccess) {
            Settings.Instance.Font = "RobotoMono";
            SetFontSize(fontSize);
            return;
        }
        
        if (!ImGuiManager.GuiResourceManager.BuildFontAtlas()) {
            Settings.Instance.Font = "RobotoMono";
            SetFontSize(fontSize);
            return;
        }
        
        ImFontPtr AddFont(string name, float size, ImVector* ranges) {
            var fs = RysyPlatform.Current.GetRysyFilesystem();

            ImFontConfigPtr cfg = ImGuiNative.ImFontConfig_ImFontConfig();
            cfg.RasterizerDensity = 1f;
            cfg.FontDataOwnedByAtlas = true;
            cfg.OversampleH = 2;
            cfg.OversampleV = 2;
            cfg.GlyphMaxAdvanceX = float.MaxValue;
            cfg.RasterizerMultiply = 1.0f;
            cfg.EllipsisChar = unchecked((ushort) -1);
            cfg.GlyphRanges = ranges->Data;
            
            if (File.Exists(name)) {
                return io.Fonts.AddFontFromFileTTF(name, size, cfg);
            }

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

            if (fontPtr.NativePtr == null && name != defaultFontPath) {
                return AddFont(defaultFontPath, size, ranges);
            }

            if (fontPtr.NativePtr != null) {
                var newCfgData = *cfg.NativePtr;
                var newCfg = new ImFontConfigPtr(&newCfgData) { MergeMode = true };
                const int ICON_MIN_FA = 0xe000;
                const int  ICON_MAX_FA = 0xf8ff;
                ReadOnlySpan<ushort> icon_ranges = [ICON_MIN_FA, ICON_MAX_FA, 0];
                var mem = ImGui.MemAlloc((uint)icon_ranges.Length * sizeof(ushort));
                icon_ranges.CopyTo(new Span<ushort>((void*)mem, icon_ranges.Length));
                newCfg.GlyphRanges = mem;
                fontPtr = io.Fonts.AddFontFromFileTTF("Assets/fa-solid-900.ttf", 13.0f, newCfg);
            }
            
            return fontPtr;
        }
    }

    public static ImFontPtr DefaultFont { get; private set; }
    public static ImFontPtr BoldFont { get; private set; }
    public static ImFontPtr ItalicFont { get; private set; }
    public static ImFontPtr ItalicBoldFont { get; private set; }
    public static ImFontPtr HeaderFont { get; private set; }
    public static ImFontPtr Header2Font { get; private set; }
}
