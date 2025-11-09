using Hexa.NET.ImGui;
using Microsoft.Win32;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using System.Diagnostics;

namespace Rysy.Tools;

public class ToolHandler {
    public const float DefaultMaterialListWidth = 360f;

    public readonly HistoryHandler History;

    public readonly Input Input;

    public readonly ToolRegistry Registry;

    private readonly object _toolLock = new();
    
    private List<Tool> _tools;
    
    public IReadOnlyList<Tool> Tools => _tools;

    private Tool? _currentTool;
    public Tool CurrentTool {
        get => _currentTool ??= _tools.FirstOrDefault() ?? throw new UnreachableException("No tools registered?");
        set {
            if (_currentTool != value) {
                CancelInteraction();
                _currentTool = value;
            }
        }
    }

    private readonly List<QuickActionInfo> _quickActions = [];
    private int _maxQuickActions = 3;

    public IReadOnlyList<QuickActionInfo> QuickActions => _quickActions;

    private Tool Create(Type type, HistoryHandler history, Input input) {
        var t = (Tool) Activator.CreateInstance(type)!;

        t.History = history;
        t.Input = input;
        t.Init();

        t.HotkeyHandler = new(input, HotkeyHandler.ImGuiModes.Never);
        t.InitHotkeys(t.HotkeyHandler);

        t.ToolHandler = this;

        return t;
    }

    private static readonly string[] HardcodedOrder = [ "brush", "rectangle", "placement", "selection", "script" ];

    public ToolHandler(HistoryHandler history, Input input, ToolRegistry? registry = null) {
        History = history;
        Input = input;
        Registry = registry ?? ToolRegistry.Global;

        CreateTools();
        Registry.Tools.OnChanged += CreateTools;
        EditorState.OnCurrentRoomChanged += CancelInteraction;
        History.OnUndo += CancelInteraction;
        Themes.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(Theme theme) {
        foreach (var tool in Tools) {
            tool.OnThemeChanged(theme);
        }
    }

    public void Unload() {
        UnloadAllTools();
        Registry.Tools.OnChanged -= CreateTools;
        EditorState.OnCurrentRoomChanged -= CancelInteraction;
        History.OnUndo -= CancelInteraction;
        Themes.ThemeChanged -= OnThemeChanged;
    }

    ~ToolHandler() {
        UnloadAllTools();
    }

    private void CreateTools() {
        UnloadAllTools();

        var newTools = new List<Tool>();
        foreach (var toolType in Registry.Tools) {
            newTools.Add(Create(toolType, History, Input));
        }
        
        newTools = newTools
            .OrderBy(t => HardcodedOrder.AsSpan().IndexOf(t.Name))
            .ThenBy(t => t.Name)
            .ToList();

        lock (_toolLock) {
            _tools = newTools;
        }

        if (CurrentTool is { Name: {} prevName }) {
            SetToolByName(prevName);
        }
    }

    private void UnloadAllTools() {
        lock (_toolLock) {
            if (_tools is { }) {
                foreach (var t in _tools) {
                    t.Unload();
                }
                _tools.Clear();
            }
        }
    }

    public ToolHandler UsePersistence(bool value) {
        foreach (var tool in Tools) {
            tool.UsePersistence = value;
        }

        return this;
    }

    public void CancelInteraction() {
        CurrentTool?.CancelInteraction();
    }

    public T? GetTool<T>() where T : Tool {
        return (T?)_tools.FirstOrDefault(t => t is T);
    }

    public Tool? GetToolByName(string name) {
        return _tools.FirstOrDefault(t => t.Name == name);
    }

    public T? SetTool<T>() where T : Tool {
        var tool = GetTool<T>();

        if (tool is { })
            CurrentTool = tool;

        return tool;
    }

    public Tool? SetToolByName(string name) {
        var tool = GetToolByName(name);

        if (tool is { })
            CurrentTool = tool;

        return tool;
    }

    public void InitHotkeys(HotkeyHandler handler) {
        foreach (var tool in Tools) {
            tool.HotkeyHandler = new(Input, handler.ImGuiMode);
            tool.InitHotkeys(tool.HotkeyHandler);
        }

        handler.AddHotkeyFromSettings("tools.nextTool", "", () => SwapToNextTool(1), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("tools.prevTool", "", () => SwapToNextTool(-1), HotkeyModes.OnHoldSmoothInterval);
        
        handler.AddHotkeyFromSettings("tools.nextMode", "shift+scrolldown", () => SwapToNextMode(1), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("tools.prevMode", "shift+scrollup", () => SwapToNextMode(-1), HotkeyModes.OnHoldSmoothInterval);
        
        handler.AddHotkeyFromSettings("tools.nextLayer", "alt+scrolldown", () => SwapToNextLayer(1), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("tools.prevLayer", "alt+scrollup", () => SwapToNextLayer(-1), HotkeyModes.OnHoldSmoothInterval);
    }

    private void SwapToNextTool(int idOffset) {
        var i = _tools.IndexOf(CurrentTool);
        CurrentTool = _tools[(i + idOffset).MathMod(_tools.Count)];
    }

    private void SwapToNextMode(int idOffset) {
        if (CurrentTool is not { } tool)
            return;

        var modes = tool.ValidModes;
        var i = modes.IndexOf(tool.Mode);
        if (i == -1)
            return;

        tool.Mode = modes[(i + idOffset).MathMod(modes.Count)];
    }
    
    private void SwapToNextLayer(int idOffset) {
        if (CurrentTool is not { } tool)
            return;

        var layers = tool.ValidLayers;
        var i = layers.IndexOf(tool.Layer);
        if (i == -1)
            return;

        tool.Layer = layers[(i + idOffset).MathMod(layers.Count)];
    }

    private void CleanupQuickActions() {
        _quickActions.Sort((a, b) => b.IsFavourite.CompareTo(a.IsFavourite));
        while (_quickActions.Count >= _maxQuickActions) {
            _quickActions.RemoveAt(_quickActions.Count - 1);
        }
    }
    
    internal void PushRecentMaterial(object material) {
        QuickActionInfo quickAction;

        // TODO: ignore x,y,width,height fields for placements.
        var duplicateIdx = _quickActions.FindIndex(x => material.Equals(x.GetMaterial(this)));
        if (duplicateIdx >= 0) {
            quickAction = _quickActions[duplicateIdx];
            // Don't move favourites around.
            if (quickAction.IsFavourite)
                return;
            _quickActions.RemoveAt(duplicateIdx);
        } else {
            quickAction = QuickActionInfo.CreateFrom(CurrentTool);
        }

        _quickActions.Insert(0, quickAction);
        
        CleanupQuickActions();
    }

    public void Update(Camera camera, Room? currentRoom) {
        CurrentTool.HotkeyHandler.Update();
        CurrentTool.Update(camera, currentRoom);
    }

    public void Render(Camera camera, Room? currentRoom) {
        if (currentRoom is { }) {
            currentRoom.StartBatch(camera, Colorgrade.None);
            CurrentTool.Render(camera, currentRoom);
            GFX.EndBatch();
        }
        
        GFX.BeginBatch();
        CurrentTool.RenderOverlay();
        GFX.EndBatch();
    }

    private bool _firstGui = true;

    internal string MaterialListWindowName => "Material";
    internal string RecentWindowName => "rysy.recent_list".Translate();
    
    public void RenderGui() {
        RenderToolList(_firstGui, out float toolHeight);
        RenderLayerList(_firstGui, toolHeight);
        RenderModeList();
        RenderRecentList();

        if (CurrentTool.BeginMaterialListWindow(_firstGui) is { } size) {
            CurrentTool.RenderGui(size);
        }
        ImGui.End();
            
        _firstGui = false;
    }

    private void RenderModeList() {
        var tool = CurrentTool;
        var currentMode = tool.Mode;
        
        ImGuiManager.PushWindowStyle();
        ImGui.Begin("Mode", ImGuiManager.WindowFlagsResizable);
        ImGuiManager.PopWindowStyle();
        
        var windowSize = ImGui.GetWindowSize();
        if (ImGui.BeginListBox("##ToolModeBox", new(windowSize.X - 10, ImGui.GetTextLineHeightWithSpacing() * tool.ValidModes.Count + 2))) {
            foreach (var item in tool.ValidModes) {
                if (ImGui.Selectable(item.LocalizedName, currentMode == item)) {
                    tool.Mode = item;
                }
            }
            ImGui.EndListBox();
        }
        
        ImGui.End();
    }
    
    private void RenderLayerList(bool firstGui, float toolListHeight) {
        var tool = CurrentTool;
        var currentLayer = tool.Layer;

        if (firstGui) {
            /*
            var menubarHeight = ImGuiManager.MenubarHeight;
            var viewport = RysyEngine.Instance.GraphicsDevice.Viewport;
            var size = new NumVector2(ImGuiManager.CalcListSize(Tools.SelectMany(t => t.ValidLayers).Select(t => t.Name)).X, ImGui.GetTextLineHeightWithSpacing() * Tools.Max(t => t.ValidLayers.Count) + ImGui.GetFrameHeightWithSpacing() * 2);
            ImGui.SetNextWindowPos(new NumVector2(viewport.Width - size.X - DefaultMaterialListWidth, menubarHeight + toolListHeight));
            ImGui.SetNextWindowSize(size);*/
        }

        ImGuiManager.PushWindowStyle();
        ImGui.Begin("Layer", ImGuiManager.WindowFlagsResizable);
        ImGuiManager.PopWindowStyle();

        var windowSize = ImGui.GetWindowSize();

        if (ImGui.BeginListBox("##ToolLayerBox", new(windowSize.X - 10, ImGui.GetTextLineHeightWithSpacing() * tool.ValidLayers.Count + 5))) {
            foreach (var item in tool.ValidLayers) {
                if (ImGui.Selectable(item.LocalizedName, currentLayer.Name == item.Name)) {
                    tool.Layer = item;
                }
            }
            ImGui.EndListBox();
        }

        ImGui.End();
    }

    private void RenderToolList(bool firstGui, out float height) {
        height = 0f;
        if (firstGui) {
            var menubarHeight = ImGuiManager.MenubarHeight;
            var viewport = RysyState.GraphicsDevice.Viewport;
            var size = new NumVector2(120f, ImGui.GetTextLineHeightWithSpacing() * Tools.Count + ImGui.GetFrameHeightWithSpacing() * 2);
            ImGui.SetNextWindowPos(new NumVector2(viewport.Width - size.X - DefaultMaterialListWidth, menubarHeight));
            ImGui.SetNextWindowSize(size);
            height = size.Y;
        }

        ImGuiManager.PushWindowStyle();
        ImGui.Begin("Tool", ImGuiManager.WindowFlagsResizable);
        ImGuiManager.PopWindowStyle();

        var windowSize = ImGui.GetWindowSize();

        lock (_toolLock) {
            if (ImGui.BeginListBox("##ToolToolsBox", new(windowSize.X - 10, ImGui.GetTextLineHeightWithSpacing() * Tools.Count + 5))) {
                foreach (var item in Tools) {
                    if (ImGui.Selectable(item.Name.TranslateOrHumanize("rysy.tools"), CurrentTool == item)) {
                        CurrentTool = item;
                    }
                }
                ImGui.EndListBox();
            }
        }

        ImGui.End();
    }

    private void RenderRecentList() {
        ImGuiManager.PushWindowStyle();
        ImGui.Begin(RecentWindowName, ImGuiManager.WindowFlagsResizable);
        ImGuiManager.PopWindowStyle();

        var windowSize = ImGui.GetWindowSize();
        var actionWidth = Tool.PreviewSize + ImGui.GetStyle().ItemSpacing.X;
        var visibleActions = (int)(((windowSize.X) - 0) / actionWidth);
        _maxQuickActions = visibleActions;
        
        var actions = _quickActions;
        var i = 0;
        foreach (var action in actions.Take(_maxQuickActions)) {
            var tool = GetToolByName(action.ToolName);
            if (tool is null)
                continue;
            var layer = EditorLayers.EditorLayerFromName(action.Layer);

            ImGui.PushID($"quick-action-{i++}-{action.MaterialString}");
            
            var size = new NumVector2(0, 0);
            
            if (action.GetMaterial(this) is { } material && tool.GetMaterialPreview(layer, material) is {} preview) {
                var cursorStart = ImGui.GetCursorPos();
                size.X = preview.W;
                size.Y = preview.H;
                
                if (ImGui.Selectable("##selectable"u8, CurrentTool == tool && material.Equals(tool.Material), ImGuiSelectableFlags.AllowOverlap, size)) {
                    action.Apply(this);
                }

                if (ImGui.IsItemHovered()) {
                    if (ImGui.IsItemActive() && Input.Mouse.LeftDoubleClicked()) {
                        Input.Mouse.ConsumeLeft();
                        action.IsFavourite = !action.IsFavourite;
                        CleanupQuickActions();
                    }
                    
                    tool.RenderMaterialTooltip(layer, material, tool.GetMaterialSearchable(layer, material));
                }

                ImGui.SetCursorPos(cursorStart);
                ImGuiManager.XnaWidget(preview);
                ImGui.SameLine();
                var endPos = ImGui.GetCursorPos();
                
                if (action.IsFavourite) {
                    ImGui.SetCursorPos(cursorStart);
                    ImGuiManager.FavoriteIcon();
                    ImGui.SameLine();
                    // Avoid imgui assertions for using SetCursorPos to move forward
                    ImGui.SetCursorPos(cursorStart);
                    ImGui.Dummy(size);
                    ImGui.SameLine();
                }
            }
            
            ImGui.PopID();
        }

        ImGui.End();
    }
}
