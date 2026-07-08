using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using Rysy.Components;
using Rysy.Helpers;

namespace Rysy.Gui.Windows;

/// <summary>
/// A command to be ran via the Command Palette.
/// </summary>
public interface ICommandPaletteCommand {
    public const int PreviewSize = 32;

    public bool Enabled => true;
    
    public Searchable Searchable { get; }

    public XnaWidgetDef? CreatePreview();
    
    public bool HasPreview { get; }
    
    public ITooltip? Tooltip { get; }

    public void Run();
}

public sealed class CommandPaletteWindow : Window {
    private string _search = "";

    private Cache<IReadOnlyList<ICommandPaletteCommand>>? _commandsCache;

    private readonly ComboCache<ICommandPaletteCommand> _comboCache = new();

    private bool _firstRender = true;

    private int _selectedIdx;

    private float _downKeyInterval, _upKeyInterval;

    private float? _cachedTotalHeight;

    private bool _shouldClose;

    private int _version;
    
    public CommandPaletteWindow() : base("rysy.windows.commandPalette", 
        new GuiSize(120, 30).CalculateWindowSize(false))
    {
    }

    public CommandPaletteWindow(IEnumerable<ICommandPaletteCommand> commands) : this() {
        ChangeCommands(commands);
    }

    private void ClearCaches() {
        _selectedIdx = 0;
        _cachedTotalHeight = null;
    }
    
    private void ChangeCommands(IEnumerable<ICommandPaletteCommand> commands) {
        _search = "";
        _commandsCache = new Cache<IReadOnlyList<ICommandPaletteCommand>>(new CacheToken(), commands.ToList);
        _comboCache.Clear();
        _firstRender = true;
        _version++;
        ClearCaches();
    }

    public static void ChangeCommands(Scene scene, IEnumerable<ICommandPaletteCommand> commands) {
        if (scene.Get<CommandPaletteWindow>() is { } existing) {
            existing._shouldClose = false;
            existing.ChangeCommands(commands);
        } else {
            scene.AddWindow(new CommandPaletteWindow(commands));
        }
    }

    private NumVector2 GetMaterialListBoxSize(NumVector2 windowSize) 
        => new(windowSize.X, windowSize.Y -  ImGui.GetFrameHeightWithSpacing());
    
    protected override void Render() {
        base.Render();
        var showPlacementIcons = Settings.Instance.ShowPlacementIcons;

        if (ImGuiManager.SearchInput(ref _search, persistenceKey: _version.ToString(), focusKeyboard: _firstRender)) {
            ClearCaches();
        }
        _firstRender = false;
        ImGui.Separator();

        _commandsCache ??= Registry?.GetAllIncludingProvidersCache<ICommandPaletteCommand>()
            .Chain(IReadOnlyList<ICommandPaletteCommand> (x) => x.Where(cmd => cmd.Enabled).ToList());

        if (!_commandsCache?.HasCachedValue ?? false) {
            ClearCaches();
        }

        var commands = _commandsCache is not null
            ? _comboCache.GetValue(_commandsCache.Value, cmd => cmd.Searchable, _search)
            : [];
        
        _cachedTotalHeight ??= commands.Sum(x => GetCommandHeight(showPlacementIcons, x.Item1));

        var size = ImGui.GetContentRegionAvail();

        ImGui.BeginChild("##palette_list_main");
        var skip = ImGui.GetScrollY();

        var totalCount = commands.Count + 1;
        float? scrollToSet = null;
        
        ImGui.BeginChild(Interpolator.TempU8("##palette_list"), 
            new(0, Math.Max(GetMaterialListBoxSize(size).Y - ImGui.GetFrameHeightWithSpacing(), _cachedTotalHeight.Value)), 
            ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);

        if (skip > 0) {
            ImGui.BeginChild("mat-list", new NumVector2(0, skip));
            ImGui.EndChild();
        }

        var wasSelectionChanged = false;
        if (Input.Global.Keyboard.HeldOrClickedSmoothInterval(Keys.Down, ref _downKeyInterval)) {
            _selectedIdx++;
            wasSelectionChanged = true;
        }
        if (Input.Global.Keyboard.HeldOrClickedSmoothInterval(Keys.Up, ref _upKeyInterval)) {
            _selectedIdx--;
            wasSelectionChanged = true;
        }
        if (commands.Count > 0)
            _selectedIdx = _selectedIdx.MathMod(commands.Count);
        else
            _selectedIdx = 0;

        var id = 0;
        var rendered = 0f;
        var anyExecuted = false;
        foreach (var (command, searchable) in commands) {
            var elementHeight = GetCommandHeight(showPlacementIcons, command);
            
            if (rendered < size.Y && skip <= 0) {
                rendered += elementHeight;

                using (_ = ScopedImGui.Id(id)) {
                    anyExecuted |= RenderCommand(command, searchable, id);
                    if (wasSelectionChanged && id == _selectedIdx) {
                        scrollToSet = ImGui.GetCursorPosY() - ImGui.GetCursorStartPos().Y;
                    }
                }
                if (!ImGui.IsItemVisible())
                    break;
            } else {
                skip -= elementHeight;
            }
            
            id++;
        }
        ImGui.EndChild();

        if (scrollToSet is not null) {
            ImGui.SetScrollFromPosY(ImGui.GetCursorStartPos().Y + scrollToSet.Value);
        }
        ImGui.EndChild();
        
        if (anyExecuted && _shouldClose)
            RemoveSelf();
    }

    private static float GetCommandHeight(bool showPlacementIcons, ICommandPaletteCommand command)
    {
        return showPlacementIcons && command.HasPreview 
            ? ICommandPaletteCommand.PreviewSize + ImGui.GetStyle().FramePadding.Y
            : ImGui.GetTextLineHeightWithSpacing();
    }

    private bool RenderCommand(ICommandPaletteCommand command, Searchable searchable, int id) {
        bool ret = false;
        var showPlacementIcons = Settings.Instance.ShowPlacementIcons;
        var currentMod = EditorState.Current?.Map?.Mod;

        var size = new NumVector2(0, 0);
        var cursorStart = ImGui.GetCursorPos();

        var previewOrNull = showPlacementIcons ? command.CreatePreview() : null;

        if (previewOrNull is { } preview) {
            size.Y = preview.H;
        }

        var isSelected = _selectedIdx == id;

        var displayName = searchable.TextWithMods;
        if (ImGui.Selectable(Interpolator.TempU8($"##{displayName}"), isSelected, 
                ImGuiSelectableFlags.AllowOverlap, size) || (isSelected && Input.Global.Keyboard.IsKeyClicked(Keys.Enter))) {
            _shouldClose = true;
            PopupNotificationWindow.ShowOnException(new LangKey("rysy.windows.commandPalette.failedToRun", command.Searchable.Text), command.Run);
            ret = true;
        }

        if (ImGui.IsItemHovered()) {
            if (ImGui.BeginTooltip()) {
                command.Tooltip?.RenderImGui();
                searchable.RenderImGuiInfo(EditorState.Current, currentMod);
                ImGui.EndTooltip();
            }
        }

        ImGui.SetCursorPos(cursorStart);
        if (previewOrNull is { } p && showPlacementIcons) {
            ImGuiManager.XnaWidget(p);
            ImGui.SameLine();
        }

        
        // center the text
        cursorStart.Y = ImGui.GetCursorPos().Y;
        if (showPlacementIcons)
            cursorStart.Y += (previewOrNull?.H / 2 - ImGui.GetFontSize() / 2f) ?? 0;
        ImGui.SetCursorPosY(cursorStart.Y);
        searchable.RenderImGuiText(currentMod);

        return ret;
    }
}