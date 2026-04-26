using Hexa.NET.ImGui;
using Rysy.Components;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.Signals;
using System.Diagnostics;

namespace Rysy.Tools;

public class ToolHandler : ISignalListener<ThemeChanged> {
    public const float DefaultMaterialListWidth = 360f;

    public IHistoryHandler History { get; }

    public readonly Input Input;
    public IComponentRegistry ComponentRegistry { get; }
    private readonly IRysyLoggerFactory _loggerFactory;

    private Action? _onUnload;

    public readonly ToolRegistry Registry;

    private readonly Lock _toolLock = new();
    
    private List<Tool> _tools;

    public EditorState EditorState { get; }
    
    public IReadOnlyList<Tool> Tools => _tools;

    public QuickActionRegistry QuickActionRegistry { get; }

    public Tool CurrentTool {
        get => field ??= _tools.FirstOrDefault() ?? throw new UnreachableException("No tools registered?");
        set {
            if (field != value) {
                CancelInteraction();
                field = value;
            }
        }
    }

    private Tool Create(Type type, IHistoryHandler history, Input input) {
        var t = (Tool) Activator.CreateInstance(type)!;

        t.ToolHandler = this;
        t.Logger = _loggerFactory.CreateLogger(type);
        t.ScopedComponentRegistry = new ComponentRegistryScope(ComponentRegistry);
        t.History = history;
        t.Input = input;
        t.Init();

        t.HotkeyHandler = new(input, HotkeyHandler.ImGuiModes.Never);
        t.InitHotkeys(t.HotkeyHandler);

        return t;
    }

    private static readonly string[] HardcodedOrder = [ "brush", "rectangle", "placement", "selection", "script" ];

    public ToolHandler(EditorState editorState, IHistoryHandler history, Input input, IComponentRegistry componentRegistry, ToolRegistry? registry = null) {
        EditorState = editorState;
        History = history;
        Input = input;
        ComponentRegistry = new ComponentRegistryScope(componentRegistry);
        _loggerFactory = componentRegistry.GetRequired<IRysyLoggerFactory>();
        Registry = registry ?? ToolRegistry.Global;

        CreateTools();
        Registry.Tools.OnChanged += RegistryToolListChanged;
        EditorState.OnCurrentRoomChanged += CancelInteraction;
        History.OnUndo += CancelInteraction;

        QuickActionRegistry = new QuickActionRegistry(this);
    }

    private void OnThemeChanged(Theme theme) {
        foreach (var tool in Tools) {
            tool.OnThemeChanged(theme);
        }
    }

    public void Unload() {
        UnloadAllTools();
        (ComponentRegistry as IDisposable)?.Dispose();
        Registry.Tools.OnChanged -= RegistryToolListChanged;
        EditorState.OnCurrentRoomChanged -= CancelInteraction;
        History.OnUndo -= CancelInteraction;
        _onUnload?.Invoke();
        _onUnload = null;
    }

    void ISignalListener<ThemeChanged>.OnSignal(ThemeChanged signal) {
        OnThemeChanged(signal.NewTheme);
    }

    ~ToolHandler() {
        UnloadAllTools();
    }

    private void RegistryToolListChanged(ListenableListChanged<Type> changed) {
        CreateTools();
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
                    (t.ScopedComponentRegistry as IDisposable)?.Dispose();
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
        
        handler.AddHotkeyFromSettings("tools.nextMode", "shift+ctrl+scrolldown", () => SwapToNextMode(1), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("tools.prevMode", "shift+ctrl+scrollup", () => SwapToNextMode(-1), HotkeyModes.OnHoldSmoothInterval);
        
        handler.AddHotkeyFromSettings("tools.nextLayer", "alt+scrolldown", () => SwapToNextLayer(1), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("tools.prevLayer", "alt+scrollup", () => SwapToNextLayer(-1), HotkeyModes.OnHoldSmoothInterval);
    }

    public void AddWindows(Settings settings) {
        ComponentRegistry.Add(new WindowPersister<RecentListWindow>(
            () => new RecentListWindow(this, QuickActionRegistry, Input), RecentListWindow.LangKey, settings, defaultState: true));
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
        var layer = tool.Layer;
        for (int i = 0; i < layers.Count; i++) {
            if (layers[i] == layer) {
                tool.Layer = layers[(i + idOffset).MathMod(layers.Count)];
                return;
            }
        }
    }

    public void Update(Camera camera, Room? currentRoom) {
        CurrentTool.HotkeyHandler.Update();
        CurrentTool.Update(camera, currentRoom);
    }

    public void Render(Camera camera, Room? currentRoom) {
        if (currentRoom is { }) {
            currentRoom.StartBatch(camera, Colorgrade.None);
            CurrentTool.Render(camera, currentRoom);
            Gfx.EndBatch();
        }
        
        Gfx.BeginBatch();
        CurrentTool.RenderOverlay();
        Gfx.EndBatch();
    }

    private bool _firstGui = true;

    internal string MaterialListWindowName => "Material";
    internal string RecentWindowName => "rysy.recent_list".Translate();
    
    public void RenderGui() {
        /*
        var dockspaceId = ImGui.GetID("##dockspace");
        
        bool shouldSetDockspaces = false;

        if (ImGuiP.DockBuilderGetNode(dockspaceId).IsNull) {
            shouldSetDockspaces = true;
            ImGuiP.DockBuilderRemoveNode(dockspaceId);
            ImGuiP.DockBuilderAddNode(dockspaceId, ImGuiDockNodeFlags.None);

            uint materialId, recentId;
            materialId = ImGuiP.DockBuilderSplitNode(dockspaceId, ImGuiDir.Right, 0.5f, null, &recentId);
            
            //ImGuiP.DockBuilderDockWindow(MaterialListWindowName, materialId);
            ImGuiP.DockBuilderDockWindow(RecentWindowName, recentId);
            ImGuiP.DockBuilderFinish(dockspaceId);
        }
       // ImGui.DockSpaceOverViewport(dockspaceId, ImGui.GetMainViewport(), ImGuiDockNodeFlags.None);
        */
        
        RenderToolList(_firstGui, out float toolHeight);
        RenderLayerList(_firstGui, toolHeight);
        RenderModeList();

        if (CurrentTool.BeginMaterialListWindow(_firstGui) is { } size) {
            CurrentTool.RenderGui(size);
        }
        ImGui.End();
            
        _firstGui = false;
    }

    private NumVector2 GetToolListBoxSize(int elementCount) {
        var windowSize = ImGui.GetContentRegionAvail();

        return windowSize with {
            Y = (ImGui.GetTextLineHeightWithSpacing() * elementCount + ImGui.GetStyle().FramePadding.Y).AtMost(windowSize.Y)
        };
    }

    private void RenderModeList() {
        var tool = CurrentTool;
        var currentMode = tool.Mode;
        
        ImGuiManager.PushWindowStyle();
        ImGui.Begin("Mode", ImGuiManager.WindowFlagsResizable);
        ImGuiManager.PopWindowStyle();

        if (ImGui.BeginListBox("##ToolModeBox", GetToolListBoxSize(tool.ValidModes.Count))) {
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

        if (ImGui.BeginListBox("##ToolLayerBox", GetToolListBoxSize(tool.ValidLayers.Count))) {
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

        lock (_toolLock) {
            if (ImGui.BeginListBox("##ToolToolsBox", GetToolListBoxSize(Tools.Count))) {
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

    public void PushRecentMaterial(object material) => QuickActionRegistry.PushRecentMaterial(material);
}
