﻿using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Platforms;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rysy.Gui;

public static class ImGuiManager {
    public static ImGuiRenderer GuiRenderer { get; private set; }
    
    public static IImGuiResourceManager GuiResourceManager { get; set; }

    public static float MenubarHeight { get; set; }

    public static uint CentralDockingSpaceID { get; private set; }

    public static ImGuiWindowFlags WindowFlagsResizable =>
        //ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.NoScrollbar |
        //ImGuiWindowFlags.NoResize |
        //ImGuiWindowFlags.NoTitleBar |
        //ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.None;

    public static ImGuiWindowFlags WindowFlagsUnresizable =>
        //ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoResize |
        //ImGuiWindowFlags.NoTitleBar |
        //ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.None;

    public static ImGuiTableFlags TableFlags =>
        ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH |
        ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg |
        ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.Hideable;

    public static bool WantCaptureMouse => RysyState.ImGuiAvailable && ImGui.GetIO().WantCaptureMouse;
    public static bool WantTextInput => RysyState.ImGuiAvailable && ImGui.GetIO().WantTextInput;
    
    public static void Load() {
        GuiRenderer = new ImGuiRenderer();
        GuiResourceManager = GuiRenderer;

        ImGuiThemer.SetFontSize(16f);

        RysyState.ImGuiAvailable = true;
    }

    public static void PushWindowStyle() {
        //ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        //ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
        //ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
    }

    public static void PopWindowStyle() {
        //ImGui.PopStyleVar(3);
    }

    /// <summary>
    /// Calls <see cref="PushInvalidStyle"/> if <paramref name="condition"/> is true.
    /// Returns <paramref name="condition"/>
    /// </summary>
    public static bool PushInvalidStyleIf(bool condition) {
        if (condition)
            PushInvalidStyle();

        return condition;
    }

    private static bool _invalidStyleEnabled;
    public static void PushInvalidStyle() {
        ImGui.PushStyleColor(ImGuiCol.Text, new NumVector4(255, 0, 0, 255));
        ImGui.PushStyleColor(ImGuiCol.Border, new NumVector4(255, 0, 0, 255));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1);
        _invalidStyleEnabled = true;
    }

    public static void PopInvalidStyle() {
        if (_invalidStyleEnabled) {
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(2);
            _invalidStyleEnabled = false;
        }
    }

    private static bool _editedStylePushed;
    public static void PushEditedStyle() {
        ImGui.PushStyleColor(ImGuiCol.Text, new NumVector4(0, 255, 0, 255));
        ImGui.PushStyleColor(ImGuiCol.Border, new NumVector4(0, 255, 0, 255));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1);
        _editedStylePushed = true;
    }

    public static void PopEditedStyle() {
        if (_editedStylePushed) {
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(2);
            _editedStylePushed = false;
        }
    }

    private static bool _nullStylePushed;
    public static unsafe void PushNullStyle() {
        var color = *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled);//(Color.LightGray * 0.8f).ToNumVec4();

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.PushStyleColor(ImGuiCol.Border, color);
        //ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
        //ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1);
        _nullStylePushed = true;
    }

    public static void PopNullStyle() {
        if (_nullStylePushed) {
            ImGui.PopStyleColor(2);
            //ImGui.PopStyleVar(2);
            _nullStylePushed = false;
        }
    }

    private static readonly Stack<(TextEmphasis, TextEmphasisPushCtx)> Emphases = new();
    public static void PushEmphasis(TextEmphasis emphasis) {
        Emphases.Push((emphasis, emphasis.PushToImgui()));
    }

    public static TextEmphasis? PopEmphasis() {
        if (Emphases.TryPop(out var ret)) {
            ret.Item1.PopFromImgui(ret.Item2);
            return ret.Item1;
        }

        return null;
    }

    public struct StyleHolder {
        public bool Null { get; set; }
        public bool Edited { get; set; }
        public bool Invalid { get; set; }
        public TextEmphasis? Emphasis { get; set; }
    }

    public static void PushAllStyles(StyleHolder holder) {
        if (holder.Null) {
            PushNullStyle();
        } else {
            PopNullStyle();
        }

        if (holder.Edited) {
            PushEditedStyle();
        } else {
            PopEditedStyle();
        }

        if (holder.Invalid) {
            PushInvalidStyle();
        } else {
            PopInvalidStyle();
        }

        PopEmphasis();
        if (holder.Emphasis is { } emphasis)
            PushEmphasis(emphasis);
    }

    public static StyleHolder PopAllStyles() {
        var holder = new StyleHolder() {
            Null = _nullStylePushed,
            Edited = _editedStylePushed,
            Invalid = _invalidStyleEnabled,
        };

        PopEditedStyle();
        PopInvalidStyle();
        PopNullStyle();
        holder.Emphasis = PopEmphasis();

        return holder;
    }

    public static void List<T>(IEnumerable<T> source, Func<T, string> itemNameGetter, ComboCache<T>? cache, Action<T> onClick, HashSet<string>? favorites = null) {
        cache ??= new();
        var search = cache.Search;

        if (ImGui.InputText("Search", ref search, 512)) {
            cache.Search = search;
        }

        var filtered = cache.GetValue(source, itemNameGetter, search, favorites);

        foreach (var item in filtered) {
            var name = itemNameGetter(item);
            if (ImGui.MenuItem(favorites?.Contains(name) ?? false ? $"* {name}" : name)) {
                onClick(item);
            }
        }
    }

    /// <summary>
    /// Creates a menu with <see cref="ImGui.BeginMenu(string)"/> using elements from the <paramref name="source"/>.
    /// </summary>
    public static void DropdownMenu<T>(string name, IEnumerable<T> source, Func<T, string> itemNameGetter, Action<T> onClick) {
        if (ImGui.BeginMenu(name)) {
            foreach (var item in source) {
                if (ImGui.MenuItem(itemNameGetter(item) ?? "[null]")) {
                    onClick(item);
                }
            }
            ImGui.EndMenu();
        }
    }

    public static void DropdownMenu<T>(string name, Action<T> onClick) where T : struct, Enum {
        var values = Enum.GetValues<T>();

        if (ImGui.BeginMenu(name)) {
            foreach (var item in values) {
                if (ImGui.MenuItem(item.ToString())) {
                    onClick(item);
                }
            }
            ImGui.EndMenu();
        }
    }

    public static void EnumCombo<T>(string name, ref T value) where T : struct, Enum {
        var values = Enum.GetValues<T>();

        if (ImGui.BeginCombo(name, value.ToString())) {
            foreach (var item in values) {
                if (ImGui.MenuItem(item.ToString())) {
                    value = item;
                }
            }
            ImGui.EndCombo();
        }
    }
    
    public static bool EnumComboTranslated<T>(ReadOnlySpan<char> prefix, ref T value) where T : struct, Enum {
        var values = Enum.GetValues<T>();
        var ret = false;
        
        if (ImGui.BeginCombo(prefix.TranslateOrNull() ?? prefix, value.ToString().TranslateOrHumanize(prefix)).WithTranslatedTooltip($"{prefix}.tooltip")) {
            foreach (var item in values) {
                var itemStr = item.ToString();
                if (TranslatedMenuItem(itemStr, prefix)) {
                    value = item;
                    ret = true;
                }
            }
            ImGui.EndCombo();
        }

        return ret;
    }
    
    public static bool EnumComboTranslated<T>(ReadOnlySpan<T> values, ReadOnlySpan<char> prefix, ref T value) where T : struct, Enum {
        var ret = false;
        
        if (ImGui.BeginCombo(prefix.TranslateOrNull() ?? prefix, value.ToString().TranslateOrHumanize(prefix)).WithTranslatedTooltip($"{prefix}.tooltip")) {
            foreach (var item in values) {
                var itemStr = item.ToString();
                if (TranslatedMenuItem(itemStr, prefix)) {
                    value = item;
                    ret = true;
                }
            }
            ImGui.EndCombo();
        }

        return ret;
    }

    public static bool Combo<T>(string name, ref T? value, IDictionary<T, string> values, 
        ref string search, string? tooltip = null, ComboCache<T>? cache = null,
        Func<T, string, bool>? menuItemRenderer = null) where T : notnull {

        menuItemRenderer ??= static (_, name) => ImGui.MenuItem(name);
        
        if (value is null || !values.TryGetValue(value, out var valueName)) {
            valueName = value?.ToString() ?? "";
        }

        bool changed = false;

        if (ImGui.BeginCombo(name, valueName).WithTooltip(tooltip)) {
            var oldStyles = PopAllStyles();
            ImGui.InputText("Search", ref search, 512);

            cache ??= new();
            var filtered = cache.GetValue(values, search);

            foreach (var item in filtered) {
                if (menuItemRenderer(item.Key, item.Value)) {
                    value = item.Key;
                    changed = true;
                }
            }

            ImGui.EndCombo();
            PushAllStyles(oldStyles);
        }

        return changed;
    }

    public static bool Combo<T>(string name, ref T value, IList<T> values, Func<T, string> toString, string? tooltip = null) where T : notnull {
        string? search = null;
        return Combo(name, ref value, values, toString, ref search, tooltip, null);
    }

    public static bool Combo<T>(string name, ref T value, IList<T> values, Func<T, string> toString, 
        [NotNullIfNotNull(nameof(search))] ref string? search, string? tooltip = null,
        ComboCache<T>? cache = null, Func<T, string, bool>? renderMenuItem = null) 
        where T : notnull {
        var valueName = toString(value);
        bool changed = false;
        renderMenuItem ??= static (t, valueName) => ImGui.MenuItem(valueName);

        if (ImGui.BeginCombo(name, valueName, ImGuiComboFlags.None).WithTooltip(tooltip)) {
            var oldStyles = PopAllStyles();

            if (search is { }) {
                ImGui.InputText("Search", ref search, 512);

                cache ??= new();
                values = cache.GetValue(values, toString, search);
            }
            
            foreach (var item in values) {
                if (renderMenuItem(item, toString(item))) {
                    if (!changed)
                        value = item;
                    changed = true;
                }
            }
            
            ImGui.EndCombo();
            PushAllStyles(oldStyles);
        }

        return changed;
    }

    public static bool EditableCombo<T>(string name, ref T value, IList<T> values, Func<T, string> toString, Func<string, T> stringToValue,
        [NotNullIfNotNull(nameof(search))] ref string? search, string? tooltip = null,
        ComboCache<T>? cache = null, Func<T, string, bool>? renderMenuItem = null,
        Func<T, string>? textInputStringGetter = null) {
        bool changed = false;
        var xPadding = ImGui.GetStyle().FramePadding.X;
        var buttonWidth = ImGui.GetFrameHeight();
        
        renderMenuItem ??= static (_, name) => ImGui.MenuItem(name);
        
        var valueToString = textInputStringGetter?.Invoke(value) ?? toString(value);
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - buttonWidth - xPadding);
        if (ImGui.InputText($"##text{name}", ref valueToString, 256).WithTooltip(tooltip)) {
            value = stringToValue(valueToString);
            changed = true;
        }

        cache ??= new();

        ImGui.SameLine(0f, xPadding);

        var size = cache.Size ??= CalcListSize(values.Select(toString));
        ImGui.SetNextWindowSize(new(size.X.AtLeast(320f), ImGui.GetTextLineHeightWithSpacing() * 16.AtMost(values.Count + 1) + ImGui.GetFrameHeight()));
        if (ImGui.BeginCombo($"##combo{name}", valueToString, ImGuiComboFlags.NoPreview).WithTooltip(tooltip)) {
            var oldStyles = PopAllStyles();
            
            ImGui.InputText("Search", ref search, 512);

            ImGui.BeginChild($"comboInner{name}");

            var filtered = cache.GetValue(values, toString, search);

            foreach (var item in filtered) {
                if (renderMenuItem(item, toString(item))) {
                    value = item;
                    changed = true;
                }
            }

            ImGui.EndChild();
            ImGui.EndCombo();
            
            PushAllStyles(oldStyles);
        }
        ImGui.SameLine(0f, xPadding);
        ImGui.Text(name);
        true.WithTooltip(tooltip);

        return changed;
    }

    public static bool EditableCombo<T>(string name, ref T? value, IDictionary<T, string> values, Func<string, T> stringToValue, 
        ref string search, string? tooltip = null, ComboCache<T>? cache = null,
        Func<T, string, bool>? menuItemRenderer = null)
        where T : notnull {

        bool changed = false;
        var xPadding = ImGui.GetStyle().FramePadding.X;
        var buttonWidth = ImGui.GetFrameHeight();

        menuItemRenderer ??= static (_, name) => ImGui.MenuItem(name);

        var valueToString = value?.ToString() ?? "";
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - buttonWidth - xPadding);
        if (ImGui.InputText($"##text{name}", ref valueToString, 128).WithTooltip(tooltip)) {
            value = stringToValue(valueToString);
            changed = true;
        }

        cache ??= new();

        ImGui.SameLine(0f, xPadding);

        var size = cache.Size ??= CalcListSize(values.Values);
        ImGui.SetNextWindowSize(new(size.X.AtLeast(320f), ImGui.GetTextLineHeightWithSpacing() * 16.AtMost(values.Count + 1) + ImGui.GetFrameHeight()));
        if (ImGui.BeginCombo($"##combo{name}", valueToString, ImGuiComboFlags.NoPreview).WithTooltip(tooltip)) {
            var oldStyles = PopAllStyles();
            
            ImGui.InputText("Search", ref search, 512);

            ImGui.BeginChild($"comboInner{name}");

            var filtered = cache.GetValue(values, search);

            foreach (var item in filtered) {
                if (menuItemRenderer(item.Key, item.Value)) {
                    value = item.Key;
                    changed = true;
                }
            }

            ImGui.EndChild();
            ImGui.EndCombo();
            
            PushAllStyles(oldStyles);
        }
        ImGui.SameLine(0f, xPadding);
        ImGui.Text(name);
        true.WithTooltip(tooltip);

        return changed;
    }

    public static bool ColorEdit(string label, ref Color color, ColorFormat format, string? tooltip, string? hexCodeOverride = null) {
        var colorHex = hexCodeOverride ?? ColorHelper.ToString(color, format);
        bool edited = false;

        var xPadding = ImGui.GetStyle().FramePadding.X;
        var buttonWidth = ImGui.GetFrameHeight();

        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - buttonWidth - xPadding);
        if (ImGui.InputText(Interpolator.Temp($"##text{label}"), ref colorHex, 24).WithTooltip(tooltip)) {
            if (ColorHelper.TryGet(colorHex, format, out var newColor)) {
                color = newColor;
            }
            edited = true;
        }

        ImGui.SameLine(0f, xPadding);

        switch (format) {
            case ColorFormat.RGB:
                var colorN3 = color.ToNumVec3();
                if (ImGui.ColorEdit3(Interpolator.Temp($"##combo{label}"), ref colorN3, ImGuiColorEditFlags.NoInputs).WithTooltip(tooltip)) {
                    color = new Color(colorN3.ToXna());
                    edited = true;
                }
                break;
            case ColorFormat.RGBA:
            case ColorFormat.ARGB:
                var colorN4 = color.ToNumVec4();
                if (ImGui.ColorEdit4(Interpolator.Temp($"##combo{label}"), ref colorN4, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs).WithTooltip(tooltip)) {
                    color = new Color(colorN4.ToXna());
                    edited = true;
                }
                break;
            default:
                break;
        }


        ImGui.SameLine(0f, xPadding);
        ImGui.Text(label);
        true.WithTooltip(tooltip);

        return edited;
    }
    
    public static bool ColorEditAllowEmpty(string label, ref string colorStr, ColorFormat format, string? tooltip) {
        bool edited = false;

        var xPadding = ImGui.GetStyle().FramePadding.X;
        var buttonWidth = ImGui.GetFrameHeight();

        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - buttonWidth - xPadding);
        if (ImGui.InputText(Interpolator.Temp($"##text{label}"), ref colorStr, 24).WithTooltip(tooltip)) {
            edited = true;
        }

        ImGui.SameLine(0f, xPadding);

        ColorHelper.TryGet(colorStr, format, out var color);

        switch (format) {
            case ColorFormat.RGB:
                var colorN3 = color.ToNumVec3();
                if (ImGui.ColorEdit3(Interpolator.Temp($"##combo{label}"), ref colorN3, ImGuiColorEditFlags.NoInputs).WithTooltip(tooltip)) {
                    colorStr = new Color(colorN3.ToXna()).ToString(format);
                    edited = true;
                }
                break;
            case ColorFormat.RGBA:
            case ColorFormat.ARGB:
                var colorN4 = color.ToNumVec4();
                if (ImGui.ColorEdit4(Interpolator.Temp($"##combo{label}"), ref colorN4, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs).WithTooltip(tooltip)) {
                    colorStr = new Color(colorN4.ToXna()).ToString(format);
                    edited = true;
                }
                break;
            default:
                break;
        }


        ImGui.SameLine(0f, xPadding);
        ImGui.Text(label);
        true.WithTooltip(tooltip);

        return edited;
    }

    public static void WithBottomBar(Action renderMain, Action renderBottomBar, uint? id = null) {
        var height = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().FramePadding.Y * 4f;
        var posy = ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - height;

        id ??= (uint) renderMain.Method.GetHashCode();
        ImGui.BeginChild(id.Value, new(0, posy), ImGuiChildFlags.None, ImGuiWindowFlags.NoResize);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y); //  + ImGui.GetStyle().FramePadding.Y  
        renderMain();
        ImGui.EndChild();
        ImGui.Separator();
        renderBottomBar();
    }

    public static float CalcListHeight(int count) => ImGui.GetTextLineHeightWithSpacing() * count + ImGui.GetFrameHeightWithSpacing() * 2;

    public static NumVector2 CalcListSize(IEnumerable<string> strings) {
        int i = 1;
        string longest = "";

        foreach (var str in strings) {
            if (str.Length > longest.Length)
                longest = str;

            i++;
        }

        var style = ImGui.GetStyle();

        return new(
            ImGui.CalcTextSize(longest).X + style.WindowPadding.X * 2 + style.ItemSpacing.X,
            CalcListHeight(i)
        );
    }

    private static Dictionary<string, (RenderTarget2D Target, nint ID)> Targets = new(StringComparer.Ordinal);

    public static void XnaWidget(XnaWidgetDef def)
        => XnaWidget(def.ID, def.W, def.H, def.RenderFunc, def.Camera, def.Rerender);

    public static void XnaWidget(string id, int w, int h, Action renderFunc, Camera? camera = null, bool rerender = true) {
        if (w <= 0 || h <= 0)
            return;

        bool isNew = false;
        if (!Targets.TryGetValue(id, out var t) || t.Target.Width != w || t.Target.Height != h) {
            if (t.Target != null) {
                GuiResourceManager.UnbindTexture(t.ID);
                t.Target.Dispose();
            }

            t.Target = new(RysyState.GraphicsDevice, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            t.ID = GuiResourceManager.BindTexture(t.Target);
            Targets[id] = t;
            isNew = true;
        }

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
        ImGui.Image(t.ID, new(w, h));
        ImGui.PopStyleVar(1);
        
        if ((rerender || isNew) && ImGui.IsItemVisible()) {
            var g = RysyState.GraphicsDevice;
            g.SetRenderTarget(t.Target);
            g.Clear(Color.Transparent);
            GFX.BeginBatch(camera);
            renderFunc();
            GFX.EndBatch();
            g.SetRenderTarget(null);
        }
    }

    public static void DisposeXnaWidget(string id) {
        if (Targets.TryGetValue(id, out var t)) {
            GuiResourceManager.UnbindTexture(t.ID);
            t.Target?.Dispose();
            Targets.Remove(id);
        }
    }

    public static void XnaWidgetSprite(string id, ISprite sprite, Point? size = null, bool rerender = true) {
        if (!sprite.IsLoaded)
            return;
        
        int w, h;
        Camera? camera = null;
        if (size is null) {
            var c = sprite.GetCollider().Rect;
            w = c.Width;
            h = c.Height;

            if (c.X != 0 || c.Y != 0) {
                camera ??= new Camera();
                camera.Move(new(c.X, c.Y));
            }

            while (w * h < 64 * 64) {
                camera ??= new Camera(new Viewport(0, 0, w, h));
                camera.Scale *= 2f;
                w *= 2;
                h *= 2;
            }
            
            while (w * h > 320 * 180) {
                camera ??= new Camera(new Viewport(0, 0, w, h));
                camera.Scale /= 1.5f;
                w = w * 2 / 3;
                h = h * 2 / 3;
            }
        } else {
            w = size.Value.X;
            h = size.Value.Y;
        }
        
        XnaWidget(id, w, h, () => sprite.Render(SpriteRenderCtx.Default()), camera, rerender);
    }

    public static void SpriteTooltip(string id, ISprite? sprite) {
        if (sprite is null)
            return;
        
        if (ImGui.BeginTooltip()) {
            XnaWidgetSprite(id, sprite);
            ImGui.EndTooltip();
        }
    }

    public static bool TranslatedButton(string id) {
        return ImGui.Button(id.Translate()).WithTranslatedTooltip($"{id}.tooltip");
    }

    public static bool TranslatedCheckbox(string id, ref bool v) {
        return ImGui.Checkbox(id.Translate(), ref v).WithTranslatedTooltip($"{id}.tooltip");
    }

    public static bool TranslatedInputText(string id, ref string v, uint maxLen) {
        return ImGui.InputText(id.Translate(), ref v, maxLen).WithTranslatedTooltip($"{id}.tooltip");
    }
    
    public static bool TranslatedInputInt(string id, ref int v) {
        return ImGui.InputInt(id.Translate(), ref v).WithTranslatedTooltip($"{id}.tooltip");
    }
    
    public static bool TranslatedInputFloat(string id, ref float v) {
        return ImGui.InputFloat(id.Translate(), ref v).WithTranslatedTooltip($"{id}.tooltip");
    }
    
    public static bool TranslatedInputFloat(string id, ref float v, float step) {
        return ImGui.InputFloat(id.Translate(), ref v, step).WithTranslatedTooltip($"{id}.tooltip");
    }
    
    public static bool TranslatedInputFloat(string id, ref float v, float step, ReadOnlySpan<char> format) {
        return ImGui.InputFloat(id.Translate(), ref v, step, step, format).WithTranslatedTooltip($"{id}.tooltip");
    }
    
    public static bool TranslatedInputFloat2(string id, ref NumVector2 v) {
        return ImGui.InputFloat2(id.Translate(), ref v).WithTranslatedTooltip($"{id}.tooltip");
    }

    public static bool TranslatedDragFloat2(string id, ref NumVector2 v, float v_speed, float v_min, float v_max) {
        return ImGui.DragFloat2(id.Translate(), ref v, v_speed, v_min, v_max).WithTranslatedTooltip($"{id}.tooltip");
    }

    public static void TranslatedText(string id) {
        ImGui.Text(id.Translate());
        true.WithTranslatedTooltip($"{id}.tooltip");
    }

    public static void TranslatedTextWrapped(string id) {
        ImGui.TextWrapped(id.Translate());
        true.WithTranslatedTooltip($"{id}.tooltip");
    }

    public static bool TranslatedMenuItem(ReadOnlySpan<char> key, ReadOnlySpan<char> prefix) {
        return ImGui.MenuItem(key.TranslateOrNull(prefix) ?? key).WithTranslatedTooltip($"{prefix}.{key}.tooltip");
    }

    public static unsafe int? IndexDragDrop(string payloadName, ref int index) {
        int? dropped = null;
        fixed(int* indexPtr = &index)
            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoDisableHover | ImGuiDragDropFlags.SourceNoPreviewTooltip)) {
                ImGui.SetDragDropPayload(payloadName, (IntPtr)indexPtr, sizeof(int));
                ImGui.EndDragDropSource();
            }

        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(payloadName, ImGuiDragDropFlags.AcceptBeforeDelivery);
            if (payload.NativePtr != null) {
                dropped = *(int*) payload.Data;
            }
            ImGui.EndDragDropTarget();
        }

        return dropped;
    }

    public static void Link(string text) {
        var em = new TextEmphasis { Link = text, Underline = true };
        var ctx = em.PushToImgui();
        ImGui.Text(text);
        em.PopFromImgui(ctx);
        ImGui.NewLine();
    }

    // Mostly taken from https://github.com/woofdoggo/Starforge/blob/main/Starforge/Core/Interop/ImGuiRenderer.cs
    public unsafe class ImGuiRenderer : IImGuiResourceManager {
        private RasterizerState RasterizerState;
        private GraphicsDevice GraphicsDevice => RysyState.GraphicsDevice;
        private BasicEffect Effect;

        private byte[] VertexData;
        private VertexBuffer VertexBuffer;
        private int VertexBufferSize;

        private byte[] IndexData;
        private IndexBuffer IndexBuffer;
        private int IndexBufferSize;

        private Dictionary<IntPtr, Texture2D> Textures;
        private int TextureID;
        private IntPtr? FontTextureID;

        private int ScrollWheelValue;
        
        sealed record ImGuiXnaKeyBind(ImGuiKey Key, Keys Xna, Keys? AltKey = null);
        
        private static readonly List<ImGuiXnaKeyBind> ImGuiKeys = new()
        {
            new(ImGuiKey.Tab, Keys.Tab),
            new(ImGuiKey.LeftArrow, Keys.Left),
            new(ImGuiKey.RightArrow, Keys.Right),
            new(ImGuiKey.UpArrow, Keys.Up),
            new(ImGuiKey.DownArrow, Keys.Down),
            new(ImGuiKey.PageUp, Keys.PageUp),
            new(ImGuiKey.PageDown, Keys.PageDown),
            new(ImGuiKey.Home, Keys.Home),
            new(ImGuiKey.End, Keys.End),
            new(ImGuiKey.Insert, Keys.Insert),
            new(ImGuiKey.Delete, Keys.Delete),
            new(ImGuiKey.Backspace, Keys.Back),
            new(ImGuiKey.Space, Keys.Space),
            new(ImGuiKey.Enter, Keys.Enter),
            new(ImGuiKey.Escape, Keys.Escape),
            new(ImGuiKey.KeypadEnter, Keys.Enter),
            new(ImGuiKey.A, Keys.A),
            new(ImGuiKey.C, Keys.C),
            new(ImGuiKey.V, Keys.V),
            new(ImGuiKey.X, Keys.X),
            new(ImGuiKey.Y, Keys.Y),
            new(ImGuiKey.Z, Keys.Z),
            
            new(ImGuiKey.ModCtrl, Keys.LeftControl, Keys.RightControl),
            new(ImGuiKey.ModShift, Keys.LeftShift, Keys.RightShift),
            new(ImGuiKey.ModAlt, Keys.LeftAlt, Keys.RightAlt),
        };

        public ImGuiRenderer() {
            // ImGui.NET doesn't expose the dock builder API, but we can just ship the ini file...
            if (!File.Exists("imgui.ini")) {
                RysyPlatform.Current.GetRysyFilesystem().TryOpenFile("default_imgui.ini", s => {
                   using var fs = File.Open("imgui.ini", FileMode.Create);
                   s.CopyTo(fs);
                });
            }

            IntPtr ctx = ImGui.CreateContext();
            ImGui.SetCurrentContext(ctx);

            EnableDocking();

            //ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

            Textures = new Dictionary<IntPtr, Texture2D>();

            RasterizerState = new RasterizerState() {
                CullMode = CullMode.None,
                DepthBias = 0,
                FillMode = FillMode.Solid,
                MultiSampleAntiAlias = false,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0
            };

            SetupInput();
        }

        private static void EnableDocking() {
            var io = ImGui.GetIO();

            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            
            io.ConfigDockingAlwaysTabBar = true;
            io.ConfigDockingTransparentPayload = true;
        }

        public unsafe void BuildFontAtlas() {
            // Get ImGUI font texture
            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bpp);

            Texture2D fontTex = new Texture2D(GraphicsDevice, width, height, false, SurfaceFormat.Color);
            #if FNA
            fontTex.SetDataPointerEXT(0, null, new IntPtr(pixelData), width * height * bpp);
            #else
            // Copy data to managed array
            byte[] pixels = new byte[width * height * bpp];
            unsafe {
                Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);
            }
            fontTex.SetData(pixels);
            #endif

            // Deallocate and unbind any previously built font texture
            if (FontTextureID.HasValue)
                UnbindTexture(FontTextureID.Value);

            // Bind font texture to ImGUI
            FontTextureID = BindTexture(fontTex);

            io.Fonts.SetTexID(FontTextureID.Value);
            io.Fonts.ClearTexData();
        }

        public IntPtr BindTexture(Texture2D tex) {
            IntPtr id = new IntPtr(TextureID++);
            Textures.Add(id, tex);
            return id;
        }

        public void UnbindTexture(IntPtr texPtr) {
            Textures.Remove(texPtr);
        }

        public void BeforeLayout(float elapsedSeconds) {
            ImGui.GetIO().DeltaTime = elapsedSeconds;
            if (RysyState.Game.IsActive)
                UpdateInput();
            ImGui.NewFrame();

            // allow docking windows to the sides of the window
            CentralDockingSpaceID = ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), 
                ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingOverCentralNode);
        }

        public void AfterLayout() {
            ImGui.Render();
            unsafe {
                RenderDrawData(ImGui.GetDrawData());
            }

            #if !Celeste
            switch (ImGui.GetMouseCursor()) {
                case ImGuiMouseCursor.None:
                    FnaMonogameCompat.SetMouseCursor(MouseCursor.Arrow);
                    break;
                case ImGuiMouseCursor.Arrow:
                    FnaMonogameCompat.SetMouseCursor(MouseCursor.Arrow);
                    break;
                case ImGuiMouseCursor.TextInput:
                    FnaMonogameCompat.SetMouseCursor(MouseCursor.IBeam);
                    break;
                case ImGuiMouseCursor.ResizeAll:
                    FnaMonogameCompat.SetMouseCursor(MouseCursor.SizeAll);
                    break;
                case ImGuiMouseCursor.ResizeNS:
                    FnaMonogameCompat.SetMouseCursor(MouseCursor.SizeNS);
                    break;
                case ImGuiMouseCursor.ResizeEW:
                    FnaMonogameCompat.SetMouseCursor(MouseCursor.SizeWE);
                    break;
                case ImGuiMouseCursor.ResizeNESW:
                    FnaMonogameCompat.SetMouseCursor(MouseCursor.SizeNESW);
                    break;
                case ImGuiMouseCursor.ResizeNWSE:
                    FnaMonogameCompat.SetMouseCursor(MouseCursor.SizeNWSE);
                    break;
                case ImGuiMouseCursor.Hand:
                    FnaMonogameCompat.SetMouseCursor(MouseCursor.Hand);
                    break;
                case ImGuiMouseCursor.NotAllowed:
                    FnaMonogameCompat.SetMouseCursor(MouseCursor.No);
                    break;
                case ImGuiMouseCursor.COUNT:
                    break;
            }
            #endif
        }

        protected unsafe void SetupInput() {
            ImGuiIOPtr io = ImGui.GetIO();
#if FNA
            TextInputEXT.TextInput += OnTextInput;
            TextInputEXT.StartTextInput();
            
            // Setup clipboard callbacks
            // Not needed for windows, but is needed for other OSes
            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
            static void SetClipboard(nint userdata, byte* txt) {
                _ = SDL2Ext.SDL_SetClipboardText(txt);
            }
            
            delegate* <byte*> get = &SDL2Ext.SDL_GetClipboardText;
            delegate* unmanaged[Cdecl]<nint, byte*, void> set = &SetClipboard;
            io.GetClipboardTextFn = (nint)get;
            io.SetClipboardTextFn = (nint)set;
#else
            RysyEngine.Instance.Window.TextInput += (object? sender, TextInputEventArgs e) => OnTextInput(e.Character);
#endif
            io.Fonts.AddFontDefault();
        }

        private void OnTextInput(char c) {
            const char volumeUp = (char) 128;
            const char volumeDown = (char) 129;

            if (c is '\t' or volumeUp or volumeDown)
                return;

            ImGui.GetIO().AddInputCharacter(c);
        }

        protected Effect UpdateEffect(Texture2D texture) {
            Effect ??= new BasicEffect(GraphicsDevice);
            ImGuiIOPtr io = ImGui.GetIO();

            Effect.World = Matrix.Identity;
            Effect.View = Matrix.Identity;
            Effect.Projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
            Effect.TextureEnabled = true;
            Effect.Texture = texture;
            Effect.VertexColorEnabled = true;

            return Effect;
        }

        protected void UpdateInput() {
            // Make sure the window is focused before responding to input.
            if (!RysyState.Game?.IsActive ?? true)
                return;

            ImGuiIOPtr io = ImGui.GetIO();

            MouseState m = Mouse.GetState();
            KeyboardState kbd = Keyboard.GetState();

            foreach (var (imGuiKey, xnaKey, altKeyMaybe) in ImGuiKeys)
            {
                if (kbd.IsKeyDown(xnaKey) || (altKeyMaybe is {} altKey && kbd.IsKeyDown(altKey))) {
                    if (!ImGui.IsKeyDown(imGuiKey)) {
                        io.AddKeyEvent(imGuiKey, true);
                    }
                } else {
                    if (ImGui.IsKeyDown(imGuiKey)) {
                        io.AddKeyEvent(imGuiKey, false);
                    }
                }
            }

            io.DisplaySize = new System.Numerics.Vector2(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);
            io.MousePos = new System.Numerics.Vector2(m.X, m.Y);

            io.MouseDown[0] = m.LeftButton == ButtonState.Pressed;
            io.MouseDown[1] = m.RightButton == ButtonState.Pressed;
            io.MouseDown[2] = m.MiddleButton == ButtonState.Pressed;

            int scrollDelta = m.ScrollWheelValue - ScrollWheelValue;
            io.MouseWheel = scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0;
            ScrollWheelValue = m.ScrollWheelValue;
        }

        private void RenderDrawData(ImDrawDataPtr ptr) {
            Viewport lastViewport = GraphicsDevice.Viewport;
            Rectangle lastScissor = GraphicsDevice.ScissorRectangle;
            
            ptr.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);
            GraphicsDevice.Viewport = new Viewport(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            UpdateBuffers(ptr);
            RenderCommandLists(ptr);

            // Restore graphics state
            GraphicsDevice.Viewport = lastViewport;
            GraphicsDevice.ScissorRectangle = lastScissor;
        }

        private unsafe void UpdateBuffers(ImDrawDataPtr ptr) {
            if (ptr.TotalVtxCount == 0)
                return;

            // Make vertex/index buffers larger if needed
            if (ptr.TotalVtxCount > VertexBufferSize) {
                if (VertexBuffer != null)
                    VertexBuffer.Dispose();

                VertexBufferSize = (int) (ptr.TotalVtxCount * 1.5f);
                VertexBuffer = new VertexBuffer(GraphicsDevice, DrawVertDeclaration.Declaration, VertexBufferSize, BufferUsage.None);
                VertexData = new byte[VertexBufferSize * DrawVertDeclaration.Size];
            }

            if (ptr.TotalIdxCount > IndexBufferSize) {
                if (IndexBuffer != null)
                    IndexBuffer.Dispose();

                IndexBufferSize = (int) (ptr.TotalIdxCount * 1.5f);
                IndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, IndexBufferSize, BufferUsage.None);
                IndexData = new byte[IndexBufferSize * sizeof(ushort)];
            }

            // Copy draw data to managed byte arrays
            int vtxOffset = 0;
            int idxOffset = 0;

            for (int i = 0; i < ptr.CmdListsCount; i++) {
                ImDrawListPtr cmdList = ptr.CmdLists[i];
                fixed (void* vtxDstPtr = &VertexData[vtxOffset * DrawVertDeclaration.Size]) {
                    fixed (void* idxDstPtr = &IndexData[idxOffset * sizeof(ushort)]) {
                        Buffer.MemoryCopy((void*) cmdList.VtxBuffer.Data, vtxDstPtr, VertexData.Length, cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);
                        Buffer.MemoryCopy((void*) cmdList.IdxBuffer.Data, idxDstPtr, IndexData.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
                    }
                }

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }

            // Copy byte arrays to GPU
            VertexBuffer.SetData(VertexData, 0, ptr.TotalVtxCount * DrawVertDeclaration.Size);
            IndexBuffer.SetData(IndexData, 0, ptr.TotalIdxCount * sizeof(ushort));
        }

        private unsafe void RenderCommandLists(ImDrawDataPtr ptr) {
            int vtxOffset = 0;
            int idxOffset = 0;
            
            GraphicsDevice.SetVertexBuffer(VertexBuffer);
            GraphicsDevice.Indices = IndexBuffer;
            GraphicsDevice.BlendFactor = Color.White;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.RasterizerState = RasterizerState;
            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            
            for (int i = 0; i < ptr.CmdListsCount; i++) {
                ImDrawListPtr cmdList = ptr.CmdLists[i];

                for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++) {
                    ImDrawCmdPtr cmd = cmdList.CmdBuffer[cmdi];
                    if (!Textures.TryGetValue(cmd.TextureId, out Texture2D? texture)) {
                        throw new InvalidOperationException($"Could not find ImGUI texture with ID {cmd.TextureId}");
                    }
                    
                    GraphicsDevice.ScissorRectangle = new Rectangle(
                        (int) cmd.ClipRect.X,
                        (int) cmd.ClipRect.Y,
                        (int) (cmd.ClipRect.Z - cmd.ClipRect.X),
                        (int) (cmd.ClipRect.W - cmd.ClipRect.Y)
                    );

                    Effect e = UpdateEffect(texture);
                    for (int passIndex = 0; passIndex < e.CurrentTechnique.Passes.Count; passIndex++) {
                        EffectPass pass = e.CurrentTechnique.Passes[passIndex];
                        pass.Apply();
                        GraphicsDevice.DrawIndexedPrimitives(
                            primitiveType: PrimitiveType.TriangleList,
                            baseVertex: vtxOffset,
                            startIndex: idxOffset,
                            primitiveCount: (int) cmd.ElemCount / 3
#if FNA
                            ,minVertexIndex: 0,
                            numVertices: (int) cmd.ElemCount
#endif
                        );
                    }

                    idxOffset += (int) cmd.ElemCount;
                }

                vtxOffset += cmdList.VtxBuffer.Size;
            }
        }
    }

    public static class DrawVertDeclaration {
        public static unsafe readonly int Size = sizeof(ImDrawVert);
        public static readonly VertexDeclaration Declaration = new VertexDeclaration(
            Size,
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0), // Position
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0), // UV
            new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
        );
    }
}
