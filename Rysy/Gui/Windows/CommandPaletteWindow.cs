using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using Rysy.Components;
using Rysy.Helpers;

namespace Rysy.Gui.Windows;

/// <summary>
/// A command to be ran via the Command Palette.
/// To register,
/// </summary>
public interface ICommandPaletteCommand {
    public const int PreviewSize = 32;
    
    public Searchable Searchable { get; }

    public XnaWidgetDef? CreatePreview();
    
    public ITooltip? Tooltip { get; }

    public void Run();
}

public sealed class CommandPaletteWindow : Window {
    private string _search = "";

    private Cache<IReadOnlyList<ICommandPaletteCommand>>? _commandsCache;

    private readonly ComboCache<ICommandPaletteCommand> _comboCache = new();

    private ICommandPaletteCommand? _currentCommand;

    private bool _firstRender = true;

    private int _selectedIdx = 0;

    private float _downKeyInterval, _upKeyInterval;
    
    public CommandPaletteWindow() : base("rysy.windows.commandPalette", 
        new GuiSize(50, 30).CalculateWindowSize(false))
    {
    }

    protected NumVector2 GetMaterialListBoxSize(NumVector2 windowSize) 
        => new(windowSize.X, windowSize.Y -  ImGui.GetFrameHeightWithSpacing());
    
    protected override void Render() {
        base.Render();
        var showPlacementIcons = Settings.Instance.ShowPlacementIcons;

        if (ImGuiManager.SearchInput(ref _search, focusKeyboard: _firstRender)) {
            _selectedIdx = 0;
        }
        _firstRender = false;
        ImGui.Separator();

        _commandsCache ??= Registry?.GetAllIncludingProvidersCache<ICommandPaletteCommand>();

        var commands = _commandsCache is not null
            ? _comboCache.GetValue(_commandsCache.Value, cmd => cmd.Searchable, _search)
            : [];

        var size = ImGui.GetContentRegionAvail();
        var elementHeight = showPlacementIcons ? ICommandPaletteCommand.PreviewSize + ImGui.GetStyle().FramePadding.Y : ImGui.GetTextLineHeightWithSpacing();
        var elementsVisible = size.Y / elementHeight;
        var skip = (ImGui.GetScrollY() / elementHeight) - 1;

        var totalCount = commands.Count + 1;
        ImGui.BeginChild(Interpolator.TempU8("##palette_list"), 
            new(0, Math.Max(GetMaterialListBoxSize(size).Y - ImGui.GetFrameHeightWithSpacing(), totalCount * elementHeight)), 
            ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollWithMouse);
        // make sure columns stay consistent
        skip = Math.Min(skip, commands.Count - elementsVisible);
        if (skip > 0) {
            ImGui.BeginChild("mat-list", new NumVector2(0, skip * elementHeight));
            ImGui.EndChild();
        }

        if (Input.Global.Keyboard.HeldOrClickedSmoothInterval(Keys.Down, ref _downKeyInterval)) {
            _selectedIdx++;
        }
        if (Input.Global.Keyboard.HeldOrClickedSmoothInterval(Keys.Up, ref _upKeyInterval)) {
            _selectedIdx--;
        }
        if (commands.Count > 0)
            _selectedIdx = _selectedIdx.MathMod(commands.Count);
        else
            _selectedIdx = 0;

        var id = 0;
        var rendered = 0;
        var anyExecuted = false;
        foreach (var (command, searchable) in commands) {
            if (rendered < elementsVisible && skip <= 0) {
                rendered++;

                using (_ = ScopedImGui.Id(id))
                    anyExecuted |= RenderCommand(command, searchable, id);
                id++;
            }
            skip--;
        }
        ImGui.EndChild();
        
        if (anyExecuted)
            RemoveSelf();
    }
    
    protected bool RenderCommand(ICommandPaletteCommand command, Searchable searchable, int id) {
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
        if (isSelected) {
            _currentCommand = command;
        }

        var displayName = searchable.TextWithMods;
        if (ImGui.Selectable(Interpolator.TempU8($"##{displayName}"), isSelected, 
                ImGuiSelectableFlags.AllowOverlap, size) || (isSelected && Input.Global.Keyboard.IsKeyClicked(Keys.Enter))) {
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