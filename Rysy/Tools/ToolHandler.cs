using ImGuiNET;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.History;
using Rysy.Scenes;

namespace Rysy.Tools;

public class ToolHandler {
    public const float DefaultMaterialListWidth = 200f;

    public readonly HistoryHandler History;

    public ToolHandler(HistoryHandler history) {
        History = history;
        Tools = new();

        // TODO: autogen
        AddTool(new BrushTool());
        AddTool(new TileRectTool());
        AddTool(new PlacementTool());
        AddTool(new SelectionTool());

        HotReloadHandler.OnHotReload += () => {
            _firstGui = true;
        };
        RysyEngine.OnViewportChanged += (v) => {
            _firstGui = true;
        };

        history.OnUndo += CancelInteraction;
    }

    public void CancelInteraction() {
        CurrentTool?.CancelInteraction();
    }

    private void AddTool(Tool tool) {
        Tools.Add(tool);
        tool.History = History;
        tool.Init();
    }

    public List<Tool> Tools;

    private Tool _currentTool;
    public Tool CurrentTool {
        get => _currentTool ??= Tools.FirstOrDefault() ?? throw new Exception("No tools registered?");
        set {
            CancelInteraction();
            _currentTool = value;
        }
    }

    public void InitHotkeys(HotkeyHandler handler) {
        foreach (var tool in Tools) {
            tool.InitHotkeys(handler);
        }

        handler.AddHotkeyFromSettings("tools.nextTool", "tab", SwapToNextTool, HotkeyModes.OnClick);
    }

    private void SwapToNextTool() {
        var i = Tools.IndexOf(CurrentTool);
        CurrentTool = Tools[(i + 1) % Tools.Count];
    }

    public void Update(Camera camera, Room currentRoom) {
        CurrentTool.Update(camera, currentRoom);
    }

    public void Render(Camera camera, Room currentRoom) {
        currentRoom.StartBatch(camera);

        CurrentTool.Render(camera, currentRoom);

        GFX.EndBatch();

        GFX.BeginBatch();
        CurrentTool.RenderOverlay();
        GFX.EndBatch();
    }

    private bool _firstGui = true;

    public void RenderGui(EditorScene editor) {
        if (editor is not { Map: { } map }) {
            return;
        }

        RenderToolList(editor, _firstGui, out float toolHeight);
        RenderLayerList(editor, _firstGui, toolHeight);
        CurrentTool.RenderGui(editor, _firstGui);
        _firstGui = false;
    }

    private void RenderLayerList(EditorScene editor, bool firstGui, float toolListHeight) {
        var tool = CurrentTool;
        var currentLayer = tool.Layer;

        if (firstGui) {
            var menubarHeight = ImGuiManager.MenubarHeight;
            var viewport = RysyEngine.Instance.GraphicsDevice.Viewport;
            var size = new NumVector2(80f, ImGui.GetTextLineHeightWithSpacing() * Tools.Max(t => t.ValidLayers.Count) + ImGui.GetFrameHeightWithSpacing() * 2);
            ImGui.SetNextWindowPos(new NumVector2(viewport.Width - size.X - DefaultMaterialListWidth, menubarHeight + toolListHeight));
            ImGui.SetNextWindowSize(size);
        }

        ImGuiManager.PushWindowStyle();
        if (!ImGui.Begin("Layer", ImGuiManager.WindowFlagsResizable)) {
            return;
        }
        ImGuiManager.PopWindowStyle();

        var windowSize = ImGui.GetWindowSize();

        if (ImGui.BeginListBox("##ToolLayerBox", new(windowSize.X - 10, ImGui.GetTextLineHeightWithSpacing() * tool.ValidLayers.Count + 5))) {
            foreach (var item in tool.ValidLayers) {
                if (ImGui.Selectable(item, currentLayer == item)) {
                    tool.Layer = item;
                }
            }
            ImGui.EndListBox();
        }

        ImGui.End();
    }

    private void RenderToolList(EditorScene editor, bool firstGui, out float height) {
        height = 0f;
        if (firstGui) {
            var menubarHeight = ImGuiManager.MenubarHeight;
            var viewport = RysyEngine.Instance.GraphicsDevice.Viewport;
            var size = new NumVector2(120f, ImGui.GetTextLineHeightWithSpacing() * Tools.Count + ImGui.GetFrameHeightWithSpacing() * 2);
            ImGui.SetNextWindowPos(new NumVector2(viewport.Width - size.X - DefaultMaterialListWidth, menubarHeight));
            ImGui.SetNextWindowSize(size);
            height = size.Y;
        }

        ImGuiManager.PushWindowStyle();
        if (!ImGui.Begin("Tool", ImGuiManager.WindowFlagsResizable)) {
            return;
        }
        ImGuiManager.PopWindowStyle();

        var windowSize = ImGui.GetWindowSize();

        if (ImGui.BeginListBox("##ToolToolsBox", new(windowSize.X - 10, ImGui.GetTextLineHeightWithSpacing() * Tools.Count + 5))) {
            foreach (var item in Tools) {
                if (ImGui.Selectable(item.Name, CurrentTool == item)) {
                    CurrentTool = item;
                }
            }
            ImGui.EndListBox();
        }

        ImGui.End();
    }
}
