using ImGuiNET;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.History;
using Rysy.Scenes;

namespace Rysy.Tools;

public class ToolHandler {
    public const float DefaultMaterialListWidth = 150f;

    public readonly HistoryHandler History;

    public ToolHandler(HistoryHandler history) {
        History = history;
        Tools = new();

        // TODO: autogen
        AddTool(new BrushTool());
        AddTool(new TileRectTool());

        

        HotReloadHandler.OnHotReload += () => {
            _firstGui = true;
        };
    }

    private void AddTool(Tool tool) {
        Tools.Add(tool);
        tool.History = History;
        tool.Init();
    }

    public List<Tool> Tools;

    public Tool CurrentTool {
        get => Tools[ToolIndex];
        set => ToolIndex = Tools.IndexOf(value);
    }
    public int ToolIndex { get; set; } = 1;

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
            var size = new NumVector2(80f, ImGui.GetTextLineHeightWithSpacing() * tool.ValidLayers.Count + ImGui.GetFrameHeightWithSpacing() * 2);
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
