using Hexa.NET.ImGui;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Platforms;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rysy.Gui;

public static class Themes {
    private static Theme _current = new();

    public static Theme Current {
        get => _current;
        private set {
            if (_current == value) return;
            _current = value;
            ThemeChanged?.Invoke(value);
        }
    }

    public static event Action<Theme>? ThemeChanged;

    private sealed class ColorJsonConverter : JsonConverter<Color> {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var str = reader.GetString() ?? "ffffff";

            return ColorHelper.TryGet(str, ColorFormat.RGBA, out Color c) ? c : default;
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
            writer.WriteStringValue(ColorHelper.ToRGBAString(value));
        }
    }

    internal static readonly JsonSerializerOptions JsonOptions = new() {
        IncludeFields = true,
        WriteIndented = true,
        IgnoreReadOnlyProperties = true,
        Converters = { new ColorJsonConverter(), new JsonStringEnumConverter(), }
    };

    public static void LoadThemeFromJson(string themeJson) {
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
    
    public static void LoadThemeFromFile(string filename) {
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

        ReadOnlySpan<ushort> latinExtendedRanges = [
            0x0100, 0x024F,
            0,
        ];

        builder.AddRanges((uint*) Unsafe.AsPointer(ref Unsafe.AsRef(in latinExtendedRanges[0])));
        builder.BuildRanges(&ranges);


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


            if (fontPtr.Handle != null) {
                var newCfgData = *cfg.Handle;
                var newCfg = new ImFontConfigPtr(&newCfgData) { MergeMode = true };
                const int ICON_MIN_FA = 0xe000;
                const int ICON_MAX_FA = 0xf8ff;
                ReadOnlySpan<ushort> icon_ranges = [ICON_MIN_FA, ICON_MAX_FA, 0];
                var mem = ImGui.MemAlloc((uint) icon_ranges.Length * sizeof(ushort));
                icon_ranges.CopyTo(new Span<ushort>((void*) mem, icon_ranges.Length));
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
                if (RysyPlatform.Current.GetRysyFilesystem().TryReadAllBytes(name) is { } bytes) {
                    // Imgui will take ownership of this memory, we need to native-alloc it.
                    var mem = ImGui.MemAlloc((uint) bytes.Length);
                    bytes.CopyTo(new Span<byte>((void*) mem, bytes.Length));
                    fontPtr = io.Fonts.AddFontFromMemoryTTF(mem, bytes.Length, size, cfg);
                } else if (RysyPlatform.Current.GetSystemFontsFilesystem()?.TryReadAllBytes(name) is { } bytes2) {
                    // Imgui will take ownership of this memory, we need to native-alloc it.
                    var mem = ImGui.MemAlloc((uint) bytes2.Length);
                    bytes2.CopyTo(new Span<byte>((void*) mem, bytes2.Length));

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

    public static Theme CreateFromCurrent() {
        var theme = new Theme {
            ImGuiStyle = ImGuiStyleData.CreateFromCurrent(),
            TriggerCategoryColors = Themes.Current.TriggerCategoryColors.ToDictionary()
        };

        return theme;
    }

    public sealed class ImGuiStyleData {
        public Dictionary<string, Color> Colors { get; set; } = [];

        #region Custom Colors

        [JsonIgnore]
        public Color ModNameColor => Colors.GetOrSetDefault("Search:ModName", Color.LightSkyBlue);

        [JsonIgnore]
        public Color TagColor => Colors.GetOrSetDefault("Search:Tag", Color.Gold);

        [JsonIgnore]
        public Color FormEditedColor => Colors.GetOrSetDefault("Form:Edited", Color.LimeGreen);

        [JsonIgnore]
        public Color FormWarningColor => Colors.GetOrSetDefault("Form:Warning", new Color(230, 179, 0, 255));

        [JsonIgnore]
        public Color FormInvalidColor => Colors.GetOrSetDefault("Form:Invalid", Color.Red);

        [JsonIgnore]
        public Color FormNullColor => Colors.GetOrSetDefault("TextDisabled", Color.Gray);

        [JsonIgnore]
        public Color TextColor => Colors.GetOrSetDefault("Text", Color.White);

        #endregion

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
        public static unsafe ImGuiStyleData CreateFromCurrent() {
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

            data.Colors = Themes.Current.ImGuiStyle.Colors.ToDictionary();
            for (ImGuiCol i = 0; i < ImGuiCol.Count; i++) {
                var name = ImGui.GetStyleColorNameS(i);
                var color = new Color((*ImGui.GetStyleColorVec4(i)).ToXna());

                data.Colors[name] = color;
            }

            return data;
        }
    }
}