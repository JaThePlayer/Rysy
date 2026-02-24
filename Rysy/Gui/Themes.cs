using Hexa.NET.ImGui;
using Rysy.Components;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Signals;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rysy.Gui;

public sealed class Themes : ISignalEmitter, ISignalListener<SettingsChanged<string>>, ISignalListener<SettingsChanged<int>>, ISignalListener<SettingsChanged<bool>> {
    public Theme Current {
        get;
        private set {
            if (field == value) return;
            field = value;
            ((ISignalEmitter)this).SignalTarget.Send(new ThemeChanged(value));
        }
    } = new();

    private sealed class ColorJsonConverter : JsonConverter<Color> {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var str = reader.GetString() ?? "ffffff";

            return ColorHelper.TryGet(str, ColorFormat.Rgba, out Color c) ? c : default;
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
            writer.WriteStringValue(ColorHelper.ToRgbaString(value));
        }
    }

    internal static readonly JsonSerializerOptions JsonOptions = new() {
        IncludeFields = true,
        WriteIndented = true,
        IgnoreReadOnlyProperties = true,
        Converters = { new ColorJsonConverter(), new JsonStringEnumConverter(), }
    };

    public void LoadThemeFromJson(string themeJson) {
        if (JsonExtensions.TryDeserialize<Theme>(themeJson, out var theme, JsonOptions) &&
            theme.ImGuiStyle.Colors.Count > 0) {
            theme.Apply();
            Current = theme;
        }
    }

    public record FoundTheme(string Filename, Searchable Searchable);
    
    public static IReadOnlyList<FoundTheme> FindThemes() {
        return ModRegistry.Filesystem.FindFilesInDirectoryRecursiveWithMod("Rysy/themes", "json")
            .Select(x => new FoundTheme(x.Item1, new Searchable(x.Item1.TrimStart("Rysy/themes/").TrimEnd(".json").Humanize(), x.Item2)))
            .ToList();
    }
    
    public void LoadThemeFromFile(string filename) {
        if (!ModRegistry.IsLoaded)
            return;
        
        var fs = ModRegistry.Filesystem;
        filename = filename
            .AddPrefixIfNeeded("Rysy/themes/")
            .AddPostfixIfNeeded(".json");
        
        if (!fs.FileExists(filename)) {
            Logger.Write("Themes", LogLevel.Warning, $"Theme doesn't exist: {filename}.");
            return;
        }

        if (fs.TryReadAllText(filename) is { } themeJson) {
            LoadThemeFromJson(themeJson);
        }
    }

    public static unsafe void SetFontSize(float fontSize) {
        var io = ImGui.GetIO();
        io.Fonts.Clear();

        ImVector<uint> ranges = new();

        var builder = ImGui.ImFontGlyphRangesBuilder();
        builder.AddRanges(io.Fonts.GetGlyphRangesDefault());

        {
            var latinExtendedRanges = (ushort*)ImGui.MemAlloc(3 * sizeof(ushort));
            latinExtendedRanges[0] = 0x0100;
            latinExtendedRanges[1] = 0x024F;
            latinExtendedRanges[2] = 0x0000;
            builder.AddRanges((uint*)latinExtendedRanges);
            builder.BuildRanges(&ranges);
            ImGui.MemFree(latinExtendedRanges);
        }

        var font = Settings.Instance.Font ?? "RobotoMono"; // "C:/Windows/Fonts/consola";
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
            ImFontConfigPtr cfg = ImGui.ImFontConfig();
            //cfg.RasterizerDensity = 1f;
            //cfg.FontDataOwnedByAtlas = true;
            //cfg.OversampleH = 2;
            //cfg.OversampleV = 2;
            cfg.GlyphMaxAdvanceX = float.MaxValue;
            cfg.RasterizerMultiply = 1.0f;
            cfg.EllipsisChar = unchecked((ushort) -1);
            //cfg.GlyphRanges = ranges->Data;

            ImFontPtr fontPtr = AddFontFromVirtPath(name, size, cfg);

            if (fontPtr.Handle == null && name != defaultFontPath) {
                return AddFont(defaultFontPath, size, ranges);
            }


            if (fontPtr.Handle != null) {
                var newCfgData = *cfg.Handle;
                var newCfg = new ImFontConfigPtr(&newCfgData) { MergeMode = true };
                
                var mem = (ushort*)ImGui.MemAlloc(3 * sizeof(ushort));
                mem[0] = 0xe000;
                mem[1] = 0xf8ff;
                mem[2] = 0;
                newCfg.GlyphRanges = (uint*) mem;
                var newFontWithIcons = AddFontFromVirtPath("fa-solid-900.ttf", 13f, newCfg);
                if (newFontWithIcons.Handle != null)
                    fontPtr = newFontWithIcons;
            }


            return fontPtr;

            // Loads a font from a virtual path, either from the mod filesystem or the OS's fonts folder.
            static ImFontPtr AddFontFromVirtPath(string name, float size, ImFontConfigPtr cfg) {
                var io = ImGui.GetIO();

                ImFontPtr fontPtr = default;
                if (ModRegistry.Filesystem.TryReadAllBytes(name.AddPrefixIfNeeded("Rysy/fonts/")) is { } bytes) {
                    // Imgui will take ownership of this memory, we need to native-alloc it.
                    var mem = ImGui.MemAlloc((uint) bytes.Length);
                    bytes.CopyTo(new Span<byte>(mem, bytes.Length));
                    fontPtr = io.Fonts.AddFontFromMemoryTTF(mem, bytes.Length, size, cfg);
                } else if (RysyPlatform.Current.GetSystemFontsFilesystem()?.TryReadAllBytes(name) is { } bytes2) {
                    // Imgui will take ownership of this memory, we need to native-alloc it.
                    var mem = ImGui.MemAlloc((uint) bytes2.Length);
                    bytes2.CopyTo(new Span<byte>(mem, bytes2.Length));

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
    
    SignalTarget ISignalEmitter.SignalTarget { get; set; }
    
    public void OnSignal(SettingsChanged<string> signal) {
        this.Emit(new RunAtEndOfThisFrame(() => {
            switch (signal.SettingName) {
                case nameof(Settings.Theme):
                    LoadThemeFromFile(signal.Value);
                    break;
                case nameof(Settings.Font):
                    SetFontSize(signal.Settings.FontSize);
                    break;
            }
        }));
    }
    
    public void OnSignal(SettingsChanged<int> signal) {
        this.Emit(new RunAtEndOfThisFrame(() => {
            switch (signal.SettingName) {
                case nameof(Settings.FontSize):
                    SetFontSize(signal.Value);
                    break;
            }
        }));
    }

    public void OnSignal(SettingsChanged<bool> signal) {
        this.Emit(new RunAtEndOfThisFrame(() => {
            switch (signal.SettingName) {
                case nameof(Settings.UseBoldFontByDefault):
                    SetFontSize(signal.Settings.FontSize);
                    break;
            }
        }));
    }
}

public sealed class Theme {
    [JsonPropertyName("ImGui")]
    public ImGuiStyleData ImGuiStyle { get; set; } = new();

    public Dictionary<string, Color> TriggerCategoryColors { get; set; } = new() {
        [TriggerCategories.Default] = Color.LightSkyBlue,
        [TriggerCategories.Camera] = Color.IndianRed,
        [TriggerCategories.Audio] = Color.LimeGreen,
        [TriggerCategories.Visual] = Color.MediumPurple
    };

    public void Apply() {
        ImGuiStyle.Apply();
    }

    public string ToJson() => this.ToJson(Themes.JsonOptions);

    public static Theme CreateFromCurrent(Theme current) {
        var theme = new Theme {
            ImGuiStyle = ImGuiStyleData.CreateFromCurrent(current),
            TriggerCategoryColors = current.TriggerCategoryColors.ToDictionary()
        };

        return theme;
    }

    public sealed class ImGuiStyleData {
        public Dictionary<string, Color> Colors { get; set; } = [];

        public float Alpha { get; set; }

        public float DisabledAlpha { get; set; }

        public NumVector2 WindowPadding { get; set; }

        public float WindowRounding { get; set; }

        public float WindowBorderSize { get; set; }

        public float WindowBorderHoverPadding { get; set; }

        public NumVector2 WindowMinSize { get; set; }

        public NumVector2 WindowTitleAlign { get; set; }

        public ImGuiDir WindowMenuButtonPosition { get; set; }

        public float ChildRounding { get; set; }

        public float ChildBorderSize { get; set; }

        public float PopupRounding { get; set; }

        public float PopupBorderSize { get; set; }

        public NumVector2 FramePadding { get; set; }

        public float FrameRounding { get; set; }

        public float FrameBorderSize { get; set; }

        public NumVector2 ItemSpacing { get; set; }

        public NumVector2 ItemInnerSpacing { get; set; }

        public NumVector2 CellPadding { get; set; }

        public NumVector2 TouchExtraPadding { get; set; }

        public float IndentSpacing { get; set; }

        public float ColumnsMinSpacing { get; set; }

        public float ScrollbarSize { get; set; }

        public float ScrollbarRounding { get; set; }

        public float ScrollbarPadding { get; set; }

        public float GrabMinSize { get; set; }

        public float GrabRounding { get; set; }

        public float LogSliderDeadzone { get; set; }

        public float ImageBorderSize { get; set; }

        public float TabRounding { get; set; }

        public float TabBorderSize { get; set; }

        public float TabMinWidthBase { get; set; }

        public float TabMinWidthShrink { get; set; }

        public float TabCloseButtonMinWidthSelected { get; set; }

        public float TabCloseButtonMinWidthUnselected { get; set; }

        public float TabBarBorderSize { get; set; }

        public float TabBarOverlineSize { get; set; }

        public float TableAngledHeadersAngle { get; set; }

        public NumVector2 TableAngledHeadersTextAlign { get; set; }

        public ImGuiTreeNodeFlags TreeLinesFlags { get; set; }

        public float TreeLinesSize { get; set; }

        public float TreeLinesRounding { get; set; }

        public ImGuiDir ColorButtonPosition { get; set; }

        public NumVector2 ButtonTextAlign { get; set; }

        public NumVector2 SelectableTextAlign { get; set; }

        public float SeparatorTextBorderSize { get; set; }

        public NumVector2 SeparatorTextAlign { get; set; }

        public NumVector2 SeparatorTextPadding { get; set; }

        public NumVector2 DisplayWindowPadding { get; set; }

        public NumVector2 DisplaySafeAreaPadding { get; set; }

        public bool DockingNodeHasCloseButton { get; set; }

        public float DockingSeparatorSize { get; set; }

        public void Apply() {
            var style = ImGui.GetStyle();
            style.Alpha = Alpha;
            style.DisabledAlpha = DisabledAlpha;
            style.WindowPadding = WindowPadding;
            style.WindowRounding = WindowRounding;
            style.WindowBorderSize = WindowBorderSize;
            style.WindowBorderHoverPadding = WindowBorderHoverPadding;
            style.WindowMinSize = WindowMinSize;
            style.WindowTitleAlign = WindowTitleAlign;
            style.WindowMenuButtonPosition = WindowMenuButtonPosition;
            style.ChildRounding = ChildRounding;
            style.ChildBorderSize = ChildBorderSize;
            style.PopupRounding = PopupRounding;
            style.PopupBorderSize = PopupBorderSize;
            style.FramePadding = FramePadding;
            style.FrameRounding = FrameRounding;
            style.FrameBorderSize = FrameBorderSize;
            style.ItemSpacing = ItemSpacing;
            style.ItemInnerSpacing = ItemInnerSpacing;
            style.CellPadding = CellPadding;
            style.TouchExtraPadding = TouchExtraPadding;
            style.IndentSpacing = IndentSpacing;
            style.ColumnsMinSpacing = ColumnsMinSpacing;
            style.ScrollbarSize = ScrollbarSize;
            style.ScrollbarRounding = ScrollbarRounding;
            style.ScrollbarPadding = ScrollbarPadding;
            style.GrabMinSize = GrabMinSize;
            style.GrabRounding = GrabRounding;
            style.LogSliderDeadzone = LogSliderDeadzone;
            style.ImageBorderSize = ImageBorderSize;
            style.TabRounding = TabRounding;
            style.TabBorderSize = TabBorderSize;
            style.TabMinWidthBase = TabMinWidthBase;
            style.TabMinWidthShrink = TabMinWidthShrink;
            style.TabCloseButtonMinWidthSelected = TabCloseButtonMinWidthSelected;
            style.TabCloseButtonMinWidthUnselected = TabCloseButtonMinWidthUnselected;
            style.TabBarBorderSize = TabBarBorderSize;
            style.TabBarOverlineSize = TabBarOverlineSize;
            style.TableAngledHeadersAngle = TableAngledHeadersAngle;
            style.TableAngledHeadersTextAlign = TableAngledHeadersTextAlign;
            style.TreeLinesFlags = TreeLinesFlags;
            style.TreeLinesSize = TreeLinesSize;
            style.TreeLinesRounding = TreeLinesRounding;
            style.ColorButtonPosition = ColorButtonPosition;
            style.ButtonTextAlign = ButtonTextAlign;
            style.SelectableTextAlign = SelectableTextAlign;
            style.SeparatorTextBorderSize = SeparatorTextBorderSize;
            style.SeparatorTextAlign = SeparatorTextAlign;
            style.SeparatorTextPadding = SeparatorTextPadding;
            style.DisplayWindowPadding = DisplayWindowPadding;
            style.DisplaySafeAreaPadding = DisplaySafeAreaPadding;
            style.DockingNodeHasCloseButton = DockingNodeHasCloseButton;
            style.DockingSeparatorSize = DockingSeparatorSize;

            for (ImGuiCol i = 0; i < ImGuiCol.Count; i++) {
                var name = ImGui.GetStyleColorNameS(i);
                if (Colors.TryGetValue(name, out var color))
                    style.Colors[(int) i] = color.ToNumVec4();
            }
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static unsafe ImGuiStyleData CreateFromCurrent(Theme theme) {
            var data = new ImGuiStyleData();

            var style = ImGui.GetStyle();
            data.Alpha = style.Alpha;
            data.DisabledAlpha = style.DisabledAlpha;
            data.WindowPadding = style.WindowPadding;
            data.WindowRounding = style.WindowRounding;
            data.WindowBorderSize = style.WindowBorderSize;
            data.WindowBorderHoverPadding = style.WindowBorderHoverPadding;
            data.WindowMinSize = style.WindowMinSize;
            data.WindowTitleAlign = style.WindowTitleAlign;
            data.WindowMenuButtonPosition = style.WindowMenuButtonPosition;
            data.ChildRounding = style.ChildRounding;
            data.ChildBorderSize = style.ChildBorderSize;
            data.PopupRounding = style.PopupRounding;
            data.PopupBorderSize = style.PopupBorderSize;
            data.FramePadding = style.FramePadding;
            data.FrameRounding = style.FrameRounding;
            data.FrameBorderSize = style.FrameBorderSize;
            data.ItemSpacing = style.ItemSpacing;
            data.ItemInnerSpacing = style.ItemInnerSpacing;
            data.CellPadding = style.CellPadding;
            data.TouchExtraPadding = style.TouchExtraPadding;
            data.IndentSpacing = style.IndentSpacing;
            data.ColumnsMinSpacing = style.ColumnsMinSpacing;
            data.ScrollbarSize = style.ScrollbarSize;
            data.ScrollbarRounding = style.ScrollbarRounding;
            data.ScrollbarPadding = style.ScrollbarPadding;
            data.GrabMinSize = style.GrabMinSize;
            data.GrabRounding = style.GrabRounding;
            data.LogSliderDeadzone = style.LogSliderDeadzone;
            data.ImageBorderSize = style.ImageBorderSize;
            data.TabRounding = style.TabRounding;
            data.TabBorderSize = style.TabBorderSize;
            data.TabMinWidthBase = style.TabMinWidthBase;
            data.TabMinWidthShrink = style.TabMinWidthShrink;
            data.TabCloseButtonMinWidthSelected = style.TabCloseButtonMinWidthSelected;
            data.TabCloseButtonMinWidthUnselected = style.TabCloseButtonMinWidthUnselected;
            data.TabBarBorderSize = style.TabBarBorderSize;
            data.TabBarOverlineSize = style.TabBarOverlineSize;
            data.TableAngledHeadersAngle = style.TableAngledHeadersAngle;
            data.TableAngledHeadersTextAlign = style.TableAngledHeadersTextAlign;
            data.TreeLinesFlags = style.TreeLinesFlags;
            data.TreeLinesSize = style.TreeLinesSize;
            data.TreeLinesRounding = style.TreeLinesRounding;
            data.ColorButtonPosition = style.ColorButtonPosition;
            data.ButtonTextAlign = style.ButtonTextAlign;
            data.SelectableTextAlign = style.SelectableTextAlign;
            data.SeparatorTextBorderSize = style.SeparatorTextBorderSize;
            data.SeparatorTextAlign = style.SeparatorTextAlign;
            data.SeparatorTextPadding = style.SeparatorTextPadding;
            data.DisplayWindowPadding = style.DisplayWindowPadding;
            data.DisplaySafeAreaPadding = style.DisplaySafeAreaPadding;
            data.DockingNodeHasCloseButton = style.DockingNodeHasCloseButton;
            data.DockingSeparatorSize = style.DockingSeparatorSize;

            data.Colors = theme.ImGuiStyle.Colors.ToDictionary();
            for (ImGuiCol i = 0; i < ImGuiCol.Count; i++) {
                var name = ImGui.GetStyleColorNameS(i);
                var color = new Color((*ImGui.GetStyleColorVec4(i)).ToXna());

                data.Colors[name] = color;
            }

            return data;
        }
    }
}

public interface IThemeColor {
    public Color Get(Theme theme);

    public NumVector4 ToNumVec4(Theme theme);
}

public static class ThemeColors {
    public static CustomColor ModNameColor { get; } = new("Search:ModName", Color.LightSkyBlue);

    public static CustomColor TagColor { get; } = new("Search:Tag", Color.Gold);

    public static CustomColor FormEditedColor { get; } = new("Form:Edited", Color.LimeGreen);

    public static CustomColor FormWarningColor { get; } = new("Form:Warning", new Color(230, 179, 0, 255));

    public static CustomColor FormInvalidColor { get; } = new("Form:Invalid", Color.Red);

    public static CustomColor FormNullColor { get; } = new("TextDisabled", Color.Gray);

    public static CustomColor TextColor { get; } = new("Text", Color.White);
}

public sealed class ImGuiColor(ImGuiCol col) : IThemeColor {
    public Color Get(Theme theme) {
        unsafe {
            return new Color((*ImGui.GetStyleColorVec4(col)).ToXna());
        }
    }

    public NumVector4 ToNumVec4(Theme theme) {
        unsafe {
            return *ImGui.GetStyleColorVec4(col);
        }
    }
}

public sealed class CustomColor(string id, Color def) : IThemeColor {
    public Color Get(Theme theme) => theme.ImGuiStyle.Colors.GetOrSetDefault(id, def);

    public NumVector4 ToNumVec4(Theme theme) => Get(theme).ToNumVec4();
}